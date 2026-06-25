using Financas.Api.Data;
using Financas.Api.DTOs.Fatura;
using Financas.Api.DTOs.Lancamento;
using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    /// <summary>
    /// Serviço responsável pelo gerenciamento de faturas de cartões de crédito.
    /// Contém a lógica de cálculo de ciclos, datas de fechamento e vencimento.
    /// </summary>
    public class FaturaService
    {
        private readonly FinancasDbContext _financasDbContext;

        /// <summary>
        /// Construtor do serviço de faturas, injetando o contexto do banco de dados.
        /// </summary>
        public FaturaService(FinancasDbContext financasDbContext)
        {
            _financasDbContext = financasDbContext;
        }

        /// <summary>
        /// Incrementa o valor total da fatura após um novo lançamento de gasto.
        /// </summary>
        /// <param name="fatura">A entidade da fatura que será atualizada.</param>
        /// <param name="valor">O valor a ser somado ao total.</param>
        /// <exception cref="ArgumentNullException">Lançada se a entidade fatura for nula.</exception>
        /// <exception cref="ArgumentException">Lançada se o valor for negativo ou zero.</exception>
        public void AplicarValorFatura(Fatura fatura, decimal valor)
        {
            if (fatura == null)
                throw new ArgumentNullException(nameof(fatura), "A fatura não pode ser nula.");

            if (valor <= 0)
                throw new ArgumentException("O valor deve ser maior que zero.");

            fatura.ValorTotal += valor;
        }

        /// <summary>
        /// Subtrai um valor do total da fatura (útil para exclusão de lançamentos ou estornos).
        /// </summary>
        /// <param name="fatura">A entidade da fatura que será atualizada.</param>
        /// <param name="valor">O valor a ser subtraído do total.</param>
        /// <exception cref="ArgumentNullException">Lançada se a entidade fatura for nula.</exception>
        /// <exception cref="ArgumentException">Lançada se o valor for negativo ou zero.</exception>
        public void EstornarValorFatura(Fatura fatura, decimal valor)
        {
            if (fatura == null)
                throw new ArgumentNullException(nameof(fatura), "A fatura não pode ser nula.");

            if (valor <= 0)
                throw new ArgumentException("O valor deve ser maior que zero.");

            fatura.ValorTotal -= valor;
        }

        /// <summary>
        /// Recupera o histórico completo de faturas de todos os cartões vinculados ao usuário.
        /// As faturas são retornadas ordenadas da mais recente para a mais antiga.
        /// </summary>
        /// <param name="usuarioId">Identificador do usuário para filtragem dos dados.</param>
        /// <returns>Uma lista de DTOs contendo o resumo de cada fatura encontrada.</returns>
        public async Task<IEnumerable<FaturaResponseDTO>> ListarFaturas(int usuarioId)
        {
            // Realiza a consulta ao banco de dados aplicando filtros de segurança e ordenação
            var faturas = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito) // Carrega os dados do cartão para validar o UsuarioId
                .Where(f => f.CartaoCredito.UsuarioId == usuarioId) // Garante que o usuário veja apenas seus próprios dados
                .OrderByDescending(f => f.DataFechamento) // Organiza para que a fatura atual/recente apareça primeiro
                .Select(f => new FaturaResponseDTO // Projeção direta para o DTO, otimizando a query SQL
                {
                    Id = f.Id,
                    CartaoCreditoId = f.CartaoCreditoId,
                    DataInicio = f.DataInicio,
                    DataFechamento = f.DataFechamento,
                    DataVencimento = f.DataVencimento,
                    ValorTotal = f.ValorTotal,
                    ValorPago = f.ValorPago,
                    Status = f.Status.ToString() // Converte o Enum para string facilitando o consumo no Front-end
                })
                .ToListAsync();

            return faturas;
        }

        /// <summary>
        /// Consulta o extrato detalhado de uma fatura, incluindo o histórico de pagamentos e o cálculo do saldo restante.
        /// </summary>
        /// <param name="faturaId">ID da fatura a ser consultada.</param>
        /// <param name="usuarioId">ID do usuário proprietário, utilizado para validação de segurança.</param>
        /// <returns>Retorna um objeto com os totais da fatura e a lista de pagamentos efetuados.</returns>
        /// <exception cref="KeyNotFoundException">Lançada caso a fatura não exista ou não pertença ao usuário logado.</exception>
        public async Task<ExtratoFaturaResponseDTO> ObterExtratoFatura(int faturaId, int usuarioId)
        {
            // Busca a fatura no banco, incluindo o cartão para garantir a verificação de posse do usuário (Multitenancy)
            var fatura = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .FirstOrDefaultAsync(f => f.Id == faturaId && f.CartaoCredito.UsuarioId == usuarioId);

            // Valida se a fatura existe
            if (fatura == null)
                throw new KeyNotFoundException("Fatura não encontrada.");

            // Busca todos os pagamentos vinculados a esta fatura, ordenando pelos mais recentes
            var pagamentos = await _financasDbContext.PagamentoFatura
                .Where(p => p.FaturaId == faturaId)
                .OrderByDescending(p => p.DataPagamento)
                .Select(p => new PagamentoResponseDTO
                {
                    Id = p.Id,
                    ValorPago = p.ValorPago,
                    DataPagamento = p.DataPagamento,
                    ContaBancariaId = p.ContaBancariaId,
                    Observacao = p.Observacao
                })
                .ToListAsync();

            var lancamentos = await _financasDbContext.Lancamentos
                .Where(l => l.FaturaId == faturaId)
                .OrderByDescending(l => l.Data)
                .Select(l => new LancamentoExtratoDTO
                {
                    Id = l.Id,
                    Descricao = l.Descricao,
                    Valor = l.Valor,
                    Data = l.Data,
                    Tipo = l.Tipo.ToString()
                })
                .ToListAsync();

            // Calcula o somatório dos pagamentos realizados
            decimal totalPago = 0;
            if (pagamentos.Any())
                totalPago = pagamentos.Sum(p => p.ValorPago);

            // Define quanto ainda resta para quitar a fatura
            var saldoRestante = fatura.ValorTotal - totalPago;

            // Monta o objeto de resposta para o DTO
            return new ExtratoFaturaResponseDTO
            {
                FaturaId = fatura.Id,
                ValorTotal = fatura.ValorTotal,
                TotalPago = totalPago,
                SaldoRestante = saldoRestante,
                Pagamentos = pagamentos,
                Lancamentos = lancamentos
            };
        }

        /// <summary>
        /// Obtém os dados de uma fatura para retorno à interface de usuário (DTO).
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário proprietário.</param>
        /// <param name="dataCompra">Data da transação para determinar o ciclo correto.</param>
        /// <returns>Objeto de resposta formatado com os dados da fatura.</returns>
        public async Task<FaturaResponseDTO> ObterOuCriarFaturaAtual(int cartaoId, int usuarioId, DateTime dataCompra)
        {
            var fatura = await ObterOuCriarFaturaAtualEntidade(cartaoId, usuarioId, dataCompra);

            return new FaturaResponseDTO
            {
                Id = fatura.Id,
                CartaoCreditoId = fatura.CartaoCreditoId,
                DataInicio = fatura.DataInicio,
                DataFechamento = fatura.DataFechamento,
                DataVencimento = fatura.DataVencimento,
                ValorTotal = fatura.ValorTotal,
                ValorPago = fatura.ValorPago,
                Status = fatura.Status.ToString()
            };
        }

        /// <summary>
        /// Lógica interna que localiza uma fatura existente para o período da compra 
        /// ou cria uma nova caso o ciclo ainda não possua registro no banco.
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário para validação de segurança.</param>
        /// <param name="dataCompra">Data da compra usada como referência para o fechamento.</param>
        /// <returns>A entidade de Fatura (processada ou recém-criada).</returns>
        /// <exception cref="KeyNotFoundException">Lançada se o cartão não for localizado.</exception>
        public async Task<Fatura> ObterOuCriarFaturaAtualEntidade(int cartaoId, int usuarioId, DateTime dataCompra)
        {
            // Validação de segurança: o cartão deve pertencer ao usuário
            var cartao = await _financasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId && c.UsuarioId == usuarioId);

            if (cartao == null)
                throw new KeyNotFoundException("Cartão não encontrado.");

            var diaFechamento = cartao.DiaFechamento;
            var diaVencimento = cartao.DiaVencimento;

            // Lógica de "Melhor dia de compra": 
            // Se a compra foi após o fechamento, ela cai no mês seguinte.
            var referencia = dataCompra.Day > diaFechamento
                ? dataCompra.AddMonths(1)
                : dataCompra;

            var ano = referencia.Year;
            var mes = referencia.Month;

            // Tratamento para meses com 28, 29 ou 30 dias (ex: Fevereiro)
            var ultimoDiaMes = DateTime.DaysInMonth(ano, mes);

            var dataFechamento = DateTime.SpecifyKind(
                new DateTime(ano, mes, Math.Min(diaFechamento, ultimoDiaMes)),
                DateTimeKind.Utc);

            var dataInicio = dataFechamento.AddMonths(-1).AddDays(1);

            var dataVencimento = DateTime.SpecifyKind(
                new DateTime(ano, mes, Math.Min(diaVencimento, ultimoDiaMes)),
                DateTimeKind.Utc);

            // Busca por uma fatura já existente que cubra este exato período
            var fatura = await _financasDbContext.Fatura
                .FirstOrDefaultAsync(f =>
                    f.CartaoCreditoId == cartaoId &&
                    f.DataInicio == dataInicio &&
                    f.DataFechamento == dataFechamento);

            // Se não existir, o sistema abre um novo ciclo (Fatura) automaticamente
            if (fatura == null)
            {
                fatura = new Fatura
                {
                    CartaoCreditoId = cartaoId,
                    DataInicio = dataInicio,
                    DataFechamento = dataFechamento,
                    DataVencimento = dataVencimento,
                    ValorTotal = 0,
                    ValorPago = 0,
                    Status = FaturaStatus.Aberta
                };

                _financasDbContext.Fatura.Add(fatura);
                await _financasDbContext.SaveChangesAsync();
            }

            return fatura;
        }

        /// <summary>
        /// Realiza o pagamento de uma fatura, atualizando o saldo da conta bancária de origem 
        /// e o status de quitação da fatura. Todo o processo é protegido por uma transação de banco de dados.
        /// </summary>
        /// <param name="dto">Dados do pagamento (ID da fatura, valor e conta de origem).</param>
        /// <param name="usuarioId">ID do usuário que está realizando a operação.</param>
        /// <exception cref="KeyNotFoundException">Lançada se a fatura ou conta bancária não existirem.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada se o recurso não pertencer ao usuário logado.</exception>
        /// <exception cref="InvalidOperationException">Lançada em caso de saldo insuficiente ou fatura já paga.</exception>
        public async Task PagarFatura(PagarFaturaDTO dto, int usuarioId)
        {
            // Inicia uma transação para garantir que o dinheiro não "suma" se houver erro no meio do processo
            using var transaction = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                // Busca a fatura incluindo os dados do cartão para validar a posse do usuário
                var fatura = await _financasDbContext.Fatura
                    .Include(f => f.CartaoCredito)
                    .FirstOrDefaultAsync(f => f.Id == dto.FaturaId);

                if (fatura == null)
                    throw new KeyNotFoundException("Fatura não encontrada.");

                // Validação de Segurança (Multitenancy)
                if (fatura.CartaoCredito.UsuarioId != usuarioId)
                    throw new UnauthorizedAccessException("Fatura não pertence ao usuário.");

                if (fatura.Status == FaturaStatus.Paga)
                    throw new InvalidOperationException("Fatura já está totalmente paga.");

                if (fatura.Status != FaturaStatus.Fechada && fatura.Status != FaturaStatus.Atrasada)
                    throw new InvalidOperationException("Somente faturas fechadas ou atrasadas podem ser pagas.");

                if (dto.ValorPago <= 0)
                    throw new ArgumentException("Valor pago deve ser maior que zero.");
                
                var totalPagoAtual = await _financasDbContext.PagamentoFatura
                    .Where(p => p.FaturaId == fatura.Id)
                    .SumAsync(p => p.ValorPago);

                var valorRestante = Math.Round(fatura.ValorTotal - totalPagoAtual, 2);
                var valorPagamentoArredondado = Math.Round(dto.ValorPago, 2);

                // LOG DE ERRO DETALHADO PARA DEBUGAR
                if (dto.ValorPago > valorRestante)
                {
                    throw new ArgumentException($"ERRO DEBUG: Pago={dto.ValorPago} | Restante={valorRestante} | TotalFatura={fatura.ValorTotal} | TotalPagoNoBanco={totalPagoAtual}");
                }

                // Impede pagamentos maiores que a dívida atual da fatura
                if (valorPagamentoArredondado > valorRestante)
                    throw new ArgumentException("Valor pago excede o valor restante da fatura.");

                // Se houver uma conta bancária vinculada, realiza a baixa do saldo
                if (dto.ContaBancariaId.HasValue)
                {
                    var conta = await _financasDbContext.ContasBancarias
                        .FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId.Value && c.UsuarioId == usuarioId);

                    if (conta == null)
                        throw new KeyNotFoundException("Conta bancária não encontrada.");

                    if (conta.Saldo < dto.ValorPago)
                        throw new InvalidOperationException("Saldo insuficiente.");

                    // Subtrai o valor do saldo disponível na conta selecionada
                    conta.Saldo -= dto.ValorPago;
                }

                var pagamento = new PagamentoFatura
                {
                    FaturaId = fatura.Id,
                    ValorPago = dto.ValorPago,
                    DataPagamento = dto.DataPagamento == default
                            ? DateTime.UtcNow
                            : dto.DataPagamento,
                    ContaBancariaId = dto.ContaBancariaId,
                    Observacao = dto.Observacao
                };

                _financasDbContext.PagamentoFatura.Add(pagamento);

                fatura.ValorPago += dto.ValorPago;

                fatura.Status = fatura.ValorPago >= fatura.ValorTotal
                    ? FaturaStatus.Paga
                    : FaturaStatus.Fechada;

                // Persiste as alterações e confirma a transação
                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                // Em caso de qualquer erro, reverte as alterações (Saldo da conta e ValorPago da fatura)
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Encerra o ciclo atual de uma fatura e, caso necessário, abre automaticamente a fatura do mês seguinte.
        /// Utiliza transação para garantir que o fechamento da antiga e a criação da nova ocorram simultaneamente.
        /// </summary>
        /// <param name="faturaId">Identificador da fatura a ser encerrada.</param>
        /// <param name="usuarioId">Identificador do usuário para validação de segurança.</param>
        /// <exception cref="KeyNotFoundException">Lançada se a fatura não existir.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada se a fatura pertencer a outro usuário.</exception>
        /// <exception cref="InvalidOperationException">Lançada se a fatura não puder ser fechada por regras de status ou data.</exception>
        public async Task FecharFatura(int faturaId, int usuarioId)
        {
            // Inicia transação para garantir a integridade ao criar a nova fatura
            await using var transaction = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                var fatura = await _financasDbContext.Fatura
                    .Include(f => f.CartaoCredito)
                    .FirstOrDefaultAsync(f => f.Id == faturaId);

                if (fatura == null)
                    throw new KeyNotFoundException("Fatura não encontrada.");

                if (fatura.CartaoCredito.UsuarioId != usuarioId)
                    throw new UnauthorizedAccessException("Fatura não pertence ao usuário.");

                // Validação: Garante que apenas faturas em uso (Aberta/Atrasada) sejam processadas
                if (fatura.Status != FaturaStatus.Aberta && fatura.Status != FaturaStatus.Atrasada)
                    throw new InvalidOperationException("Somente faturas em abertas ou atrasadas podem ser fechadas.");

                // Regra de Negócio: Impede o fechamento antes do dia estipulado no cartão
                if (DateTime.UtcNow < fatura.DataFechamento)
                    throw new InvalidOperationException("Não é possível fechar a fatura antes da data de fechamento.");

                // Verifica se já existe uma fatura futura aberta para evitar duplicidade
                var existeFaturaAberta = await _financasDbContext.Fatura
                    .AnyAsync(f => f.CartaoCreditoId == fatura.CartaoCreditoId &&
                    f.Status == FaturaStatus.Aberta &&
                    f.Id != fatura.Id);

                // Calcula saldo pendente
                var saldoPendente = fatura.ValorTotal - fatura.ValorPago;

                // Define o status correto
                fatura.Status = saldoPendente <= 0
                    ? FaturaStatus.Paga
                    : FaturaStatus.Fechada;

                // Lógica de projeção para o próximo mês
                var novoInicio = fatura.DataFechamento.AddDays(1);
                var novoFechamento = novoInicio.AddMonths(1).AddDays(-1);
                var novoVencimento = fatura.DataVencimento.AddMonths(1);

                // Se não houver uma fatura para o próximo ciclo, cria uma nova automaticamente
                if (!existeFaturaAberta)
                {
                    var novaFatura = new Fatura
                    {
                        CartaoCreditoId = fatura.CartaoCreditoId,
                        DataInicio = novoInicio,
                        DataFechamento = novoFechamento,
                        DataVencimento = novoVencimento,
                        ValorTotal = 0,
                        ValorPago = 0,
                        Status = FaturaStatus.Aberta
                    };

                    _financasDbContext.Fatura.Add(novaFatura);
                }

                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                // Caso ocorra erro ao criar a nova fatura, o fechamento da anterior é cancelado
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Retorna todas as faturas encerradas do usuário.
        /// Considera como encerradas as faturas Pagas e Fechadas.
        /// </summary>
        /// <param name="usuarioId">Identificador do usuário autenticado.</param>
        /// <returns>Lista de faturas encerradas.</returns>
        public async Task<List<FaturaEncerradaDTO>> ListarFaturasEncerradas(int usuarioId)
        {
            return await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .Where(f =>
                    f.CartaoCredito.UsuarioId == usuarioId &&
                    (f.Status == FaturaStatus.Paga ||
                     f.Status == FaturaStatus.Fechada))
                .OrderByDescending(f => f.DataFechamento)
                .Select(f => new FaturaEncerradaDTO
                {
                    Id = f.Id,
                    CartaoNome = f.CartaoCredito.Nome,
                    DataInicio = f.DataInicio,
                    DataFechamento = f.DataFechamento,
                    DataVencimento = f.DataVencimento,
                    ValorTotal = f.ValorTotal,
                    ValorPago = f.ValorPago,
                    SaldoPendente = f.ValorTotal - f.ValorPago,
                    Status = f.Status.ToString()
                })
                .ToListAsync();
        }
    }
}
