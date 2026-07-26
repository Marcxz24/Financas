using Financas.Api.Data;
using Financas.Api.DTOs.Cofrinho;
using Financas.Api.DTOs.Lancamento;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável pelo gerenciamento dos cofrinhos do usuário.
    /// Centraliza as regras de negócio relacionadas ao cadastro, manutenção,
    /// consulta e movimentação financeira entre contas bancárias e cofrinhos.
    /// </summary>
    public class CofrinhoService
    {
        // Contexto responsável pelo acesso às entidades persistidas.
        private readonly FinancasDbContext _financasDbContext;

        // Serviço de lançamentos utilizado para manter a rastreabilidade financeira.
        private readonly LancamentoService _lancamentoService;

        /// <summary>
        /// Inicializa as dependências necessárias para execução das regras de negócio.
        /// </summary>
        public CofrinhoService(FinancasDbContext financasDbContext, LancamentoService lancamentoService)
        {
            _financasDbContext = financasDbContext;
            _lancamentoService = lancamentoService;
        }

        /// <summary>
        /// Cria um novo cofrinho para o usuário autenticado.
        /// O saldo inicial é obrigatoriamente iniciado em zero.
        /// </summary>
        /// <summary>
        /// Cria um novo cofrinho para o usuário.
        /// Passos:
        /// 1. Valida existência do usuário.
        /// 2. Garante que o saldo inicial seja zero.
        /// 3. Verifica duplicidade de nome para o mesmo usuário.
        /// 4. Persiste a entidade e retorna o DTO de resposta.
        /// </summary>
        /// <param name="dto">Dados de criação (nome e saldo inicial).</param>
        /// <param name="usuarioId">Id do usuário que recebe o cofrinho.</param>
        /// <returns>Cofrinho criado mapeado para CofrinhoResponseDTO.</returns>
        public async Task<CofrinhoResponseDTO> CriarCofrinho(CriarCofrinhoDTO dto, int usuarioId)
        {
            // Busca o usuário dono do cofrinho; se não existir lança exceção.
            var usuario = await _financasDbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            // Garante que o saldo inicial seja zero (regra de negócio definida).
            if (dto.Saldo != 0)
                throw new InvalidOperationException("O saldo inicial do cofrinho deve ser zero.");

            // Verifica se já existe um cofrinho com o mesmo nome para o usuário (ignora caixa e espaços externos).
            var nomeDuplicado = await _financasDbContext.Cofrinhos.AnyAsync(c => c.UsuarioId == usuarioId && c.Nome.ToLower() == dto.Nome.Trim().ToLower());
            if (nomeDuplicado)
                throw new InvalidOperationException("Já existe um cofrinho com este nome para o usuário.");

            // Cria a entidade com valores iniciais e estado ativo.
            var cofrinho = new Cofrinho
            {
                UsuarioId = usuarioId,
                Nome = dto.Nome.Trim(),
                Saldo = 0m,
                DataCriacao = DateTime.Now,
                Status = StatusCofrinho.Ativo
            };

            // Persiste o novo cofrinho e retorna a representação DTO.
            _financasDbContext.Cofrinhos.Add(cofrinho);
            await _financasDbContext.SaveChangesAsync();

            return MapearParaResponse(cofrinho);
        }

        /// <summary>
        /// Atualiza os dados de um cofrinho existente.
        /// Valida propriedade de usuário e duplicidade de nome quando necessário.
        /// Não altera o saldo nesta operação.
        /// </summary>
        /// <param name="dto">DTO com campos opcionais para atualização.</param>
        /// <param name="cofrinhoId">Id do cofrinho a ser atualizado.</param>
        /// <param name="usuarioId">Id do usuário que solicita a atualização.</param>
        /// <returns>Cofrinho atualizado mapeado para CofrinhoResponseDTO.</returns>
        public async Task<CofrinhoResponseDTO> AtualizarCofrinho(AtualizarCofrinhoDTO dto, int cofrinhoId, int usuarioId)
        {
            // Recupera o cofrinho solicitado; erro se não existir.
            var cofrinho = await _financasDbContext.Cofrinhos.FirstOrDefaultAsync(c => c.Id == cofrinhoId);
            if (cofrinho == null)
                throw new KeyNotFoundException("Cofrinho não encontrado");

            // Verifica se o cofrinho pertence ao usuário que fez a requisição.
            if (cofrinho.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("O cofrinho não pertence ao usuário");

            // Se for solicitado troca de nome, valida duplicidade e aplica a alteração.
            if (dto.Nome != null)
            {
                var nomeDuplicado = await _financasDbContext.Cofrinhos.AnyAsync(c => c.UsuarioId == usuarioId && c.Id != cofrinhoId && c.Nome.ToLower() == dto.Nome.Trim().ToLower());
                if (nomeDuplicado)
                    throw new InvalidOperationException("Já existe um cofrinho com este nome para o usuário.");

                cofrinho.Nome = dto.Nome.Trim();
            }

            // Atualiza status quando informado.
            if (dto.Status != null)
                cofrinho.Status = dto.Status.Value;

            // Persiste somente as alterações realizadas.
            await _financasDbContext.SaveChangesAsync();
            return MapearParaResponse(cofrinho);
        }

        /// <summary>
        /// Remove um cofrinho do banco de dados.
        /// Regras:
        /// - O cofrinho deve existir.
        /// - Deve pertencer ao usuário solicitante.
        /// - O saldo deve ser zero (não permite excluir com saldo positivo).
        /// </summary>
        public async Task ExcluirCofrinho(int cofrinhoId, int usuarioId)
        {
            // Recupera entidade.
            var cofrinho = await _financasDbContext.Cofrinhos.FirstOrDefaultAsync(c => c.Id == cofrinhoId);
            if (cofrinho == null)
                throw new KeyNotFoundException("Cofrinho não encontrado");

            // Verifica propriedade do cofrinho.
            if (cofrinho.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("O cofrinho não pertence ao usuário");

            // Impede exclusão quando existir saldo a ser resgatado.
            if (cofrinho.Saldo > 0)
                throw new InvalidOperationException("Não é possível excluir um cofrinho com saldo positivo. Resgate o saldo antes.");

            _financasDbContext.Cofrinhos.Remove(cofrinho);
            await _financasDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Recupera todos os cofrinhos pertencentes a um usuário, ordenados por nome.
        /// </summary>
        /// <returns>Lista de CofrinhoResponseDTO.</returns>
        public async Task<List<CofrinhoResponseDTO>> ObterCofrinhosUsuario(int usuarioId)
        {
            // Consulta simples com filtro por usuário e ordenação por nome.
            var cofrinhos = await _financasDbContext.Cofrinhos
                .Where(c => c.UsuarioId == usuarioId)
                .OrderBy(c => c.Nome)
                .ToListAsync();

            return cofrinhos.Select(MapearParaResponse).ToList();
        }

        /// <summary>
        /// Recupera um cofrinho por id garantindo que pertença ao usuário.
        /// </summary>
        public async Task<CofrinhoResponseDTO> ObterPorId(int cofrinhoId, int usuarioId)
        {
            // Busca por id; valida existência e propriedade.
            var cofrinho = await _financasDbContext.Cofrinhos.FirstOrDefaultAsync(c => c.Id == cofrinhoId);
            if (cofrinho == null)
                throw new KeyNotFoundException("Cofrinho não encontrado");

            if (cofrinho.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("O cofrinho não pertence ao usuário");

            return MapearParaResponse(cofrinho);
        }

        /// <summary>
        /// Retorna o saldo atual do cofrinho (somente leitura).
        /// Faz validações de existência e propriedade.
        /// </summary>
        public async Task<SaldoCofrinhoDTO> ObterSaldo(int cofrinhoId, int usuarioId)
        {
            var cofrinho = await _financasDbContext.Cofrinhos.FirstOrDefaultAsync(c => c.Id == cofrinhoId);
            if (cofrinho == null)
                throw new KeyNotFoundException("Cofrinho não encontrado");

            if (cofrinho.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("O cofrinho não pertence ao usuário");

            return new SaldoCofrinhoDTO
            {
                CofrinhoId = cofrinho.Id,
                Saldo = cofrinho.Saldo
            };
        }

        /// <summary>
        /// Realiza transferência de valor da conta bancária para o cofrinho.
        /// Opera dentro de uma transação para garantir consistência entre saldos e lançamentos.
        /// </summary>
        /// <remarks>
        /// Cria dois lançamentos: despesa na conta e receita no cofrinho para manter o histórico.
        /// </remarks>
        public async Task<CofrinhoResponseDTO> TransferirContaParaCofrinho(TransferenciaCofrinhoDTO dto, int usuarioId)
        {
            // Valida existência do usuário e da conta informada.
            var usuario = await _financasDbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            var conta = await _financasDbContext.ContasBancarias.FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId);
            if (conta == null)
                throw new KeyNotFoundException("Conta bancária não encontrada");

            if (conta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("A conta bancária não pertence ao usuário");

            // Valida cofrinho alvo e propriedade.
            var cofrinho = await _financasDbContext.Cofrinhos.FirstOrDefaultAsync(c => c.Id == dto.CofrinhoId);
            if (cofrinho == null)
                throw new KeyNotFoundException("Cofrinho não encontrado");

            if (cofrinho.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("O cofrinho não pertence ao usuário");

            // Verifica saldo suficiente e status ativo do cofrinho.
            if (conta.Saldo < dto.Valor)
                throw new InvalidOperationException("Saldo insuficiente na conta bancária.");

            if (cofrinho.Status != StatusCofrinho.Ativo)
                throw new InvalidOperationException("O cofrinho está inativo.");

            // Executa transferência dentro de transação para evitar inconsistências parciais.
            using var transaction = await _financasDbContext.Database.BeginTransactionAsync();
            try
            {
                conta.Saldo -= dto.Valor;
                cofrinho.Saldo += dto.Valor;

                // Registra lançamento de saída na conta (despesa).
                _financasDbContext.Lancamentos.Add(new Lancamento
                {
                    Descricao = "Transferência para Cofrinho",
                    Valor = dto.Valor,
                    Data = DateTime.Now,
                    Tipo = TipoLancamento.Despesa,
                    UsuarioId = usuarioId,
                    ContaBancariaId = conta.Id,
                    CategoriaId = null
                });

                // Registra lançamento de entrada no cofrinho (receita).
                _financasDbContext.Lancamentos.Add(new Lancamento
                {
                    Descricao = "Transferência para Cofrinho",
                    Valor = dto.Valor,
                    Data = DateTime.Now,
                    Tipo = TipoLancamento.Receita,
                    UsuarioId = usuarioId,
                    CofrinhoId = cofrinho.Id,
                    CategoriaId = null
                });

                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await _financasDbContext.Entry(cofrinho).ReloadAsync();
                return MapearParaResponse(cofrinho);
            }
            catch
            {
                // Em caso de erro, desfaz alterações.
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Realiza o resgate do cofrinho para a conta bancária informada.
        /// Garante saldo suficiente no cofrinho e grava lançamentos correspondentes.
        /// </summary>
        public async Task<CofrinhoResponseDTO> TransferirCofrinhoParaConta(TransferenciaCofrinhoDTO dto, int usuarioId)
        {
            // Valida usuário e cofrinho.
            var usuario = await _financasDbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado");

            var cofrinho = await _financasDbContext.Cofrinhos.FirstOrDefaultAsync(c => c.Id == dto.CofrinhoId);
            if (cofrinho == null)
                throw new KeyNotFoundException("Cofrinho não encontrado");

            if (cofrinho.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("O cofrinho não pertence ao usuário");

            // Valida conta destino.
            var conta = await _financasDbContext.ContasBancarias.FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId);
            if (conta == null)
                throw new KeyNotFoundException("Conta bancária não encontrada");

            if (conta.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("A conta bancária não pertence ao usuário");

            // Verifica saldo suficiente no cofrinho para o resgate.
            if (cofrinho.Saldo < dto.Valor)
                throw new InvalidOperationException("Saldo insuficiente no cofrinho.");

            using var transaction = await _financasDbContext.Database.BeginTransactionAsync();
            try
            {
                // Ajusta saldos.
                cofrinho.Saldo -= dto.Valor;
                conta.Saldo += dto.Valor;

                // Lançamento de entrada na conta (receita).
                _financasDbContext.Lancamentos.Add(new Lancamento
                {
                    Descricao = "Resgate do Cofrinho",
                    Valor = dto.Valor,
                    Data = DateTime.Now,
                    Tipo = TipoLancamento.Receita,
                    UsuarioId = usuarioId,
                    ContaBancariaId = conta.Id,
                    CategoriaId = null
                });

                // Lançamento de saída do cofrinho (despesa do cofrinho).
                _financasDbContext.Lancamentos.Add(new Lancamento
                {
                    Descricao = "Resgate do Cofrinho",
                    Valor = dto.Valor,
                    Data = DateTime.Now,
                    Tipo = TipoLancamento.Despesa,
                    UsuarioId = usuarioId,
                    CofrinhoId = cofrinho.Id,
                    CategoriaId = null
                });

                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await _financasDbContext.Entry(cofrinho).ReloadAsync();
                return MapearParaResponse(cofrinho);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Converte a entidade <see cref="Cofrinho"/> para o DTO utilizado nas respostas da API.
        /// Centraliza o mapeamento para evitar duplicação de código e manter consistência
        /// na estrutura dos dados retornados ao cliente.
        /// </summary>
        /// <param name="cofrinho">Entidade do cofrinho obtida do banco de dados.</param>
        /// <returns>
        /// Objeto <see cref="CofrinhoResponseDTO"/> contendo os dados públicos do cofrinho.
        /// </returns>
        private static CofrinhoResponseDTO MapearParaResponse(Cofrinho cofrinho)
        {
            return new CofrinhoResponseDTO
            {
                // Identificador único do cofrinho.
                Id = cofrinho.Id,

                // Identificador do usuário proprietário do cofrinho.
                UsuarioId = cofrinho.UsuarioId,

                // Nome utilizado para identificação do cofrinho.
                Nome = cofrinho.Nome,

                // Saldo financeiro atualmente armazenado no cofrinho.
                Saldo = cofrinho.Saldo,

                // Data em que o cofrinho foi criado.
                DataCriacao = cofrinho.DataCriacao,

                // Converte o enum de status para sua representação textual,
                // facilitando a exibição no Front-End.
                Status = cofrinho.Status.ToString()
            };
        }
    }
}
