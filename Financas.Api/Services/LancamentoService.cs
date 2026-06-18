using Financas.Api.Data;
using Financas.Api.DTOs.Lancamento;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    public class LancamentoService
    {
        // Variável privada para armazenar o contexto do banco de dados (MySQL)
        private readonly FinancasDbContext _financasDbContext;

        private readonly CartaoCreditoService _cartaoCreditoService;

        private readonly FaturaService _faturaService;

        // Construtor que recebe o contexto via Injeção de Dependência
        public LancamentoService(FinancasDbContext financasDbContext, CartaoCreditoService cartaoCreditoService, FaturaService faturaService)
        {
            _financasDbContext = financasDbContext;
            _cartaoCreditoService = cartaoCreditoService;
            _faturaService = faturaService;
        }

        // Este método aplica uma alteração financeira ao saldo atual da conta
        private void AplicarValor(ContaBancaria conta, decimal valor, TipoLancamento tipo)
        {
            if (conta == null)
                throw new ArgumentNullException(nameof(conta), "A conta bancária não pode ser nula.");

            if (valor <= 0)
                throw new ArgumentException("O valor deve ser maior que zero.");

            // Verifica a direção do fluxo financeiro através do Enum 'TipoLancamento'
            if (tipo == TipoLancamento.Receita)
            {
                // Algoritmo: Se for entrada (Receita), utiliza o operador de atribuição aditiva
                // O valor é somado ao saldo existente na memória da entidade
                conta.Saldo += valor;
            }
            else
            {
                // Antes de subtrair, verifica se o saldo é suficiente para cobrir a despesa.
                if (conta.Saldo < valor)
                    throw new InvalidOperationException("Saldo insuficiente.");

                // Algoritmo: Se for saída (Despesa), utiliza o operador de atribuição subtrativa
                // O valor é deduzido do saldo atual da conta
                conta.Saldo -= valor;
            }
        }

        // Este método reverte uma operação financeira anterior para restaurar o saldo original
        private void EstornarValor(ContaBancaria conta, decimal valor, TipoLancamento tipo)
        {
            var tipoInvertido = tipo == TipoLancamento.Receita
                ? TipoLancamento.Despesa 
                : TipoLancamento.Receita;

            AplicarValor(conta, valor, tipoInvertido);
        }

        // Método para criar um novo lançamento (receita ou despesa)
        public async Task<LancamentoResponseDTO> CriarLancamento(CriarLancamentoDTO dto, int usuarioId)
        {
            // 1. Validação: Verifica se o usuário que está tentando criar o lançamento realmente existe
            var usuario = await _financasDbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            if (dto.CategoriaId == 0)
                dto.CategoriaId = null;

            if (dto.ContaBancariaId == 0)
                dto.ContaBancariaId = null;

            if (dto.CartaoCreditoId == 0)
                dto.CartaoCreditoId = null;

            if (dto.ContaBancariaId != null && dto.CartaoCreditoId != null)
                throw new InvalidOperationException("Não é permitido informar conta bancária e cartão ao mesmo tempo.");

            if (dto.CategoriaId != null)
            {
                var categoria = await _financasDbContext.Categorias
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.UsuarioId == usuarioId);

                if (categoria == null)
                    throw new KeyNotFoundException("Categoria não encontrada ou não pertence ao usuário.");
            }

            ContaBancaria? contaBancaria = null;
            CartaoCredito? cartaoCredito = null;
            Fatura? fatura = null;

            if (dto.ContaBancariaId != null)
            {
                contaBancaria = await _financasDbContext.ContasBancarias
                    .FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId && c.UsuarioId == usuarioId);

                if (contaBancaria == null)
                    throw new KeyNotFoundException("Conta bancária não encontrada ou não pertence ao usuário.");
            }

            if (dto.CartaoCreditoId != null)
            {
                cartaoCredito = await _financasDbContext.CartaoCredito
                    .FirstOrDefaultAsync(c => c.Id == dto.CartaoCreditoId && c.UsuarioId == usuarioId);

                if (cartaoCredito == null)
                    throw new KeyNotFoundException("Cartão de crédito não encontrado ou não pertence ao usuário.");

                if (dto.Tipo == TipoLancamento.Receita)
                    throw new InvalidOperationException("Não é permitido criar uma receita vinculada a um cartão de crédito.");

                if (dto.Valor <= 0)
                    throw new ArgumentException("O valor deve ser maior que zero.");

                var totalAberto = await _cartaoCreditoService.ObterTotalEmAberto(cartaoCredito.Id, usuarioId);
                var limiteDisponivel = cartaoCredito.Limite - totalAberto;

                // Valida o limite contra o valor TOTAL da compra, independente do número de parcelas
                if (dto.Valor > limiteDisponivel)
                    throw new InvalidOperationException("Limite do cartão de crédito excedido.");

                fatura = await _faturaService.ObterOuCriarFaturaAtualEntidade(cartaoCredito.Id, usuarioId, dto.Data);

                if (fatura.Status != FaturaStatus.Aberta)
                    throw new InvalidOperationException("Não é permitido adicionar lançamentos a uma fatura que não esteja aberta.");
            }

            // Se o lançamento for parcelado (mais de uma vez), seguimos por este caminho
            if (dto.QuantidadeParcelas > 1)
            {
                // Validação 1: Parcelamento só é permitido com cartão de crédito
                if (cartaoCredito == null)
                    throw new InvalidOperationException("Parcelamento só é permitido para compras no cartão de crédito.");

                // Validação 2: Receita não pode ser parcelada
                if (dto.Tipo == TipoLancamento.Receita)
                    throw new InvalidOperationException("Não é permitido parcelar uma receita.");

                // Calcula o valor de cada parcela
                var valorParcela = Math.Round(dto.Valor / dto.QuantidadeParcelas, 2);

                Lancamento? primeiraParcela = null;

                using var transaction = await _financasDbContext.Database.BeginTransactionAsync();
                try
                {
                    for (int i = 0; i < dto.QuantidadeParcelas; i++)
                    {
                        // Calcula a fatura correta para cada mês da parcela
                        var dataParcela = dto.Data.AddMonths(i);
                        var faturaParParcela = await _faturaService
                            .ObterOuCriarFaturaAtualEntidade(cartaoCredito.Id, usuarioId, dataParcela);

                        var parcela = new Lancamento
                        {
                            Descricao = $"{dto.Descricao} ({i + 1}/{dto.QuantidadeParcelas})",
                            Valor = valorParcela,
                            Data = dataParcela,
                            Tipo = dto.Tipo,
                            UsuarioId = usuarioId,
                            CategoriaId = dto.CategoriaId,
                            CartaoCreditoId = dto.CartaoCreditoId,
                            NumeroParcela = i + 1,
                            TotalParcelas = dto.QuantidadeParcelas,
                            FaturaId = faturaParParcela.Id,
                            // Na primeira parcela (i == 0) não há pai — ela É o pai
                            // Nas demais, aponta para o Id da primeira parcela salva
                            LancamentoPaiId = primeiraParcela?.Id
                        };

                        faturaParParcela.ValorTotal += valorParcela;

                        _financasDbContext.Lancamentos.Add(parcela);
                        await _financasDbContext.SaveChangesAsync(); // Salva para gerar o Id

                        // Guarda a referência da primeira parcela para as demais apontarem para ela
                        if (i == 0)
                            primeiraParcela = parcela;
                    }

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // Retorna os dados da primeira parcela como confirmação
                return new LancamentoResponseDTO
                {
                    Id = primeiraParcela!.Id,
                    UsuarioId = usuarioId,
                    Descricao = primeiraParcela.Descricao,
                    Valor = primeiraParcela.Valor,
                    Data = primeiraParcela.Data,
                    Tipo = primeiraParcela.Tipo.ToString(),
                    CategoriaId = primeiraParcela.CategoriaId,
                    CartaoCreditoId = primeiraParcela.CartaoCreditoId,
                    FaturaId = primeiraParcela.FaturaId,
                    NumeroParcela = primeiraParcela.NumeroParcela,
                    TotalParcelas = primeiraParcela.TotalParcelas
                };
            }

            // Processamento de lançamentos simples (sem parcelamento)
            var lancamento = new Lancamento
            {
                Descricao = dto.Descricao,
                Valor = dto.Valor,
                Data = dto.Data,
                Tipo = dto.Tipo,
                UsuarioId = usuarioId,
                CategoriaId = dto.CategoriaId,
                ContaBancariaId = dto.ContaBancariaId,
                CartaoCreditoId = dto.CartaoCreditoId,
                FaturaId = fatura?.Id,
                NumeroParcela = 1,
                TotalParcelas = 1
            };

            if (contaBancaria != null)
                AplicarValor(contaBancaria, dto.Valor, dto.Tipo);

            using var transactionSimples = await _financasDbContext.Database.BeginTransactionAsync();
            try
            {
                if (fatura != null)
                {
                    lancamento.FaturaId = fatura.Id;
                    fatura.ValorTotal += dto.Valor;
                }

                _financasDbContext.Lancamentos.Add(lancamento);
                await _financasDbContext.SaveChangesAsync();
                await transactionSimples.CommitAsync();
            }
            catch
            {
                await transactionSimples.RollbackAsync();
                throw;
            }

            return new LancamentoResponseDTO
            {
                Id = lancamento.Id,
                UsuarioId = usuarioId,
                Descricao = lancamento.Descricao,
                Valor = lancamento.Valor,
                Data = lancamento.Data,
                Tipo = lancamento.Tipo.ToString(),
                CategoriaId = lancamento.CategoriaId,
                CategoriaNome = lancamento.Categoria?.Nome,
                ContaBancariaId = lancamento.ContaBancariaId,
                ContaBancariaNome = lancamento.ContaBancaria?.Nome,
                CartaoCreditoId = lancamento.CartaoCreditoId,
                CartaoCreditoNome = lancamento.CartaoCredito?.Nome,
                FaturaId = lancamento.FaturaId,
                NumeroParcela = lancamento.NumeroParcela,
                TotalParcelas = lancamento.TotalParcelas
            };
        }

        // Método para buscar todos os lançamentos de um usuário específico
        public async Task<List<LancamentoResponseDTO>> GetLancamentos(int usuarioId)
        {
            // Busca no banco apenas os lançamentos que pertencem ao usuário (Filtro UsuarioId)
            var lancamentos = await _financasDbContext.Lancamentos
                .Include(l => l.Categoria) // Inclui os dados da categoria relacionada, se houver
                .Include(l => l.ContaBancaria) // Inclui os dados da conta bancária relacionada, se houver
                .Include(l => l.CartaoCredito) // Inclui os dados do cartão de crédito relacionado, se houver
                .Include(l => l.Fatura) // Inclui os dados da fatura relacionada, se houver
                .Where(l => l.UsuarioId == usuarioId)
                .ToListAsync();

            // Converte a lista de entidades para uma lista de DTOs antes de enviar para o Controller
            return lancamentos.Select(l => new LancamentoResponseDTO
            {
                Id = l.Id,
                UsuarioId = l.UsuarioId,
                Descricao = l.Descricao,
                Valor = l.Valor,
                Data = l.Data,
                Tipo = l.Tipo.ToString(),
                CategoriaId = l.CategoriaId,
                CategoriaNome = l.Categoria?.Nome,
                ContaBancariaId = l.ContaBancariaId,
                ContaBancariaNome = l.ContaBancaria?.Nome,
                CartaoCreditoId = l.CartaoCreditoId,
                CartaoCreditoNome = l.CartaoCredito?.Nome,
                FaturaId = l.FaturaId,
                NumeroParcela = l.NumeroParcela,
                TotalParcelas = l.TotalParcelas,
            }).ToList();
        }

        /// <summary>
        /// Recupera um lançamento específico pelo seu ID, incluindo todas as suas dependências.
        /// </summary>
        /// <param name="id">ID numérico do lançamento.</param>
        /// <returns>Retorna um DTO com os dados do lançamento ou null caso não seja encontrado.</returns>
        public async Task<LancamentoResponseDTO?> GetLancamentoId(int id)
        {
            // Busca o lançamento no banco aplicando o carregamento antecipado (Eager Loading)
            // das entidades relacionadas para evitar múltiplas consultas (N+1) ou retornos nulos nas propriedades de navegação.
            var lancamento = await _financasDbContext.Lancamentos
                .Include(l => l.Categoria)
                .Include(l => l.ContaBancaria)
                .Include(l => l.CartaoCredito)
                .Include(l => l.Fatura)
                .FirstOrDefaultAsync(l => l.Id == id);

            // Validação de existência: evita exceções de referência nula no mapeamento abaixo
            if (lancamento == null)
                return null;

            // Mapeamento manual da Entidade para o DTO de Resposta (Response)
            // Isso garante que apenas os dados necessários sejam trafegados para o Front-end
            return new LancamentoResponseDTO
            {
                Id = lancamento.Id,
                UsuarioId = lancamento.UsuarioId,
                Descricao = lancamento.Descricao,
                Valor = lancamento.Valor,
                Data = lancamento.Data,
                Tipo = lancamento.Tipo.ToString(), // Conversão do Enum para string para facilitar o consumo no React
                CategoriaId = lancamento.CategoriaId,
                CategoriaNome = lancamento.Categoria?.Nome, // Operador condicional nulo para evitar erros caso a categoria seja opcional
                ContaBancariaId = lancamento.ContaBancariaId,
                ContaBancariaNome = lancamento.ContaBancaria?.Nome,
                CartaoCreditoId = lancamento.CartaoCreditoId,
                CartaoCreditoNome = lancamento.CartaoCredito?.Nome,
                FaturaId = lancamento.FaturaId,
                NumeroParcela = lancamento.NumeroParcela,
                TotalParcelas = lancamento.TotalParcelas,
            };
        }

        public async Task<LancamentoResponseDTO> AtualizarLancamento(AtualizarLancamentoDTO dto, int lancamentoId, int usuarioId)
        {
            // 1. Busca o lançamento no banco de dados pelo ID fornecido
            var lancamento = await _financasDbContext.Lancamentos
                .Include(l => l.Categoria) // Inclui os dados da categoria para possível retorno no DTO
                .Include(l => l.ContaBancaria) // Inclui os dados da conta bancária para possível retorno no DTO
                .Include(l => l.CartaoCredito) // Inclui os dados do cartão de crédito para possível retorno no DTO
                .Include(l => l.Fatura) // Inclui os dados da fatura para possível retorno no DTO
                .FirstOrDefaultAsync(l => l.Id == lancamentoId);

            // 2. Validação de existência: Verifica se o registro realmente existe no MySQL
            if (lancamento == null)
                throw new KeyNotFoundException("Lançamento não encontrado.");

            // 3. Regra de Segurança Crítica: Verifica se o lançamento pertence ao usuário logado.
            // Isso impede que um usuário tente editar o ID de um lançamento de terceiros via API.
            if (lancamento.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("Sem permissão para alterar este lançamento.");

            if (lancamento.TotalParcelas > 1)
                throw new InvalidOperationException("Não é permitido editar uma parcela individualmente. Delete e recrie o parcelamento.");

            if (dto.ContaBancariaId != lancamento.ContaBancariaId)
                throw new InvalidOperationException("Não é permitido alterar a conta bancária de um lançamento. Delete e recrie.");

            if (dto.CartaoCreditoId.HasValue && dto.CartaoCreditoId != lancamento.CartaoCreditoId)
                throw new InvalidOperationException("Não é permitido alterar o cartão de crédito de um lançamento. Delete e recrie.");

            if (dto.Valor.HasValue && dto.Valor <= 0)
                throw new ArgumentException("O valor deve ser maior que zero.");

            // Antes de atualizar os campos, armazenamos os valores antigos para comparação e possível estorno
            var valorAntigo = lancamento.Valor; // Armazena o valor atual para comparação e possível estorno
            var tipoAntigo = lancamento.Tipo; // Armazena o tipo atual para comparação e possível estorno
            var contaAntiga = lancamento.ContaBancaria; // Armazena a conta bancária atual para comparação e possível estorno

            var novoValor = dto.Valor ?? lancamento.Valor; 
            var novoTipo = dto.Tipo.HasValue
                ? (TipoLancamento)dto.Tipo
                : lancamento.Tipo;

            // Verifica se houve mudança no valor ou tipo para decidir se é necessário estornar e reaplicar o valor na conta bancária
            var houveMudancaFinanceira =
                    novoValor != valorAntigo ||
                    novoTipo != tipoAntigo;

            // Barreira de Segurança Adicional: Se o lançamento estiver vinculado a uma fatura, não permite alterações que possam comprometer a integridade financeira da fatura.
            if (lancamento.Fatura != null &&
                        (lancamento.Fatura.Status == FaturaStatus.Fechada ||
                        lancamento.Fatura.Status == FaturaStatus.Paga))
            {
                throw new InvalidOperationException("Não é permitido alterar um lançamento vinculado a uma fatura fechada ou paga.");
            }


            // 4. Atualização Parcial: Os IFs abaixo permitem que o usuário envie apenas o que deseja mudar.
            // Se o campo no DTO estiver nulo, o valor atual no banco é preservado.

            if (dto.CategoriaId == 0)
            {
                lancamento.CategoriaId = null; // Permite que o usuário envie "0" para remover a categoria do lançamento
                lancamento.Categoria = null; // Remove a referência da categoria para garantir que os dados relacionados sejam atualizados corretamente no retorno do DTO
            }
            else if (dto.CategoriaId != null)
            {
                var categoria = await _financasDbContext.Categorias
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.UsuarioId == usuarioId);

                if (categoria == null)
                    throw new KeyNotFoundException("Categoria não encontrada ou não pertence ao usuário.");

                lancamento.CategoriaId = dto.CategoriaId; // Permite atualizar a categoria, inclusive para null (sem categoria)

                lancamento.Categoria = categoria; // Atualiza a referência da categoria para garantir que os dados relacionados sejam carregados corretamente no retorno do DTO
            }

            if (dto.Descricao != null)
                lancamento.Descricao = dto.Descricao;

            if (dto.Valor != null)
                lancamento.Valor = (decimal)dto.Valor; // Realiza o cast para decimal

            if (dto.Tipo != null)
                lancamento.Tipo = (Entities.Enums.TipoLancamento)dto.Tipo; // Converte o valor recebido para o Enum correspondente

            if (dto.Data != null)
                lancamento.Data = dto.Data.Value;

            // 5. Persistência: O EF Core detecta que o objeto 'lancamento' foi modificado e gera o comando UPDATE
            using var transaction = await _financasDbContext.Database.BeginTransactionAsync(); // Inicia uma transação para garantir a atomicidade das operações

            try
            {
                if (houveMudancaFinanceira)
                {
                    if (contaAntiga != null)
                        EstornarValor(contaAntiga, valorAntigo, tipoAntigo); // Reverte o valor antigo para restaurar o saldo da conta bancária antes de aplicar a nova alteração

                    if (lancamento.ContaBancaria != null)
                        AplicarValor(lancamento.ContaBancaria, lancamento.Valor, lancamento.Tipo); // Aplica o novo valor para atualizar o saldo da conta bancária com a alteração feita
                     
                    if (lancamento.CartaoCreditoId != null && lancamento.Fatura != null)
                    {
                        _faturaService.EstornarValorFatura(lancamento.Fatura, valorAntigo);
                        _faturaService.AplicarValorFatura(lancamento.Fatura, lancamento.Valor);
                    }
                }

                await _financasDbContext.SaveChangesAsync(); // Salva as alterações no banco de dados, incluindo o lançamento e a conta bancária (se houver)
                await transaction.CommitAsync(); // Confirma a transação, garantindo que todas as alterações sejam aplicadas no banco de dados
            }
            catch
            {
                await transaction.RollbackAsync(); // Em caso de erro, desfaz todas as alterações
                throw; // Relança a exceção para que o controlador possa lidar com ela adequadamente
            }

            // 6. Retorno: Devolve o objeto atualizado formatado para a resposta da API (DTO)
            return new LancamentoResponseDTO
            {
                Id = lancamento.Id,
                UsuarioId = usuarioId,
                Descricao = lancamento.Descricao,
                Valor = lancamento.Valor,
                Tipo = lancamento.Tipo.ToString(), // Converte o Enum para string (ex: "Receita")
                Data = lancamento.Data,
                CategoriaId = lancamento.CategoriaId,
                CategoriaNome = lancamento.Categoria?.Nome,
                ContaBancariaId = lancamento.ContaBancariaId,
                ContaBancariaNome = lancamento.ContaBancaria?.Nome,
                CartaoCreditoId = lancamento.CartaoCreditoId,
                CartaoCreditoNome = lancamento.CartaoCredito?.Nome,
                FaturaId = lancamento.FaturaId,
                NumeroParcela = lancamento.NumeroParcela,
                TotalParcelas = lancamento.TotalParcelas,
            };
        }

        public async Task DeletarLancamento(int lancamentoId, int usuarioId)
        {
            // 1. Busca o lançamento no banco de dados
            // É necessário buscar o objeto completo para que o EF saiba o que remover
            var lancamento = await _financasDbContext.Lancamentos
                .Include(l => l.ContaBancaria)
                .Include(l => l.Fatura)
                .FirstOrDefaultAsync(l => l.Id == lancamentoId);

            // 2. Verificação de existência
            // Evita erro de referência nula caso o ID não exista no MySQL
            if (lancamento == null)
                throw new KeyNotFoundException("Lançamento não encontrado.");

            // 3. Barreira de Segurança:
            // Mesmo que alguém saiba o ID de um lançamento, ele só consegue deletar
            // se o UsuarioId do registro for igual ao ID extraído do Token JWT.
            if (lancamento.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("Não é possível excluir um lançamento de outro usuário.");

            // 4. Regra de Negócio Crítica: Não permite excluir um lançamento que esteja vinculado a uma fatura fechada ou paga, pois isso comprometeria a integridade financeira da fatura.
            // A verificação é feita antes de qualquer alteração para garantir que o lançamento permaneça intacto caso a operação seja proibida.
            // O carregamento antecipado (Include) da fatura permite acessar o status da fatura diretamente na entidade do lançamento, evitando consultas adicionais ao banco de dados.
            if (lancamento.Fatura != null && (lancamento.Fatura.Status == FaturaStatus.Fechada || lancamento.Fatura.Status == FaturaStatus.Paga))
                throw new InvalidOperationException("Não é permitido excluir um lançamento vinculado a uma fatura fechada ou paga.");

            var temParcelasFilhas = await _financasDbContext.Lancamentos
                .AnyAsync(l => l.LancamentoPaiId == lancamento.Id);

            if (temParcelasFilhas)
                throw new InvalidOperationException("Não é permitido excluir um lançamento que possui parcelas filhas. Exclua as parcelas primeiro.");

            using var transaction = await _financasDbContext.Database.BeginTransactionAsync(); // Inicia uma transação para garantir a atomicidade das operações

            try
            {
                if (lancamento.ContaBancaria != null)
                    EstornarValor(lancamento.ContaBancaria, lancamento.Valor, lancamento.Tipo); // Reverte o valor para restaurar o saldo da conta bancária antes de excluir o lançamento

                if (lancamento.Fatura != null)
                    _faturaService.EstornarValorFatura(lancamento.Fatura, lancamento.Valor); // Reverte o valor para restaurar o total da fatura antes de excluir o lançamento

                // 4. Marcação para remoção:
                // O método .Remove() avisa ao Entity Framework que este objeto deve ser deletado
                _financasDbContext.Lancamentos.Remove(lancamento);

                // 5. Execução no Banco:
                // Aqui o EF Core gera o comando SQL "DELETE FROM Lancamentos WHERE Id = ..."
                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync(); // Confirma a transação, garantindo que todas as alterações sejam aplicadas no banco de dados
            }
            catch
            {
                await transaction.RollbackAsync(); // Em caso de erro, desfaz todas as alterações
                throw; // Relança a exceção para que o controlador possa lidar com ela adequadamente
            }
        }
    }
}