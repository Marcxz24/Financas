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
    /// Cada cartão possui no máximo uma fatura aberta por vez; o fechamento do ciclo
    /// é sempre manual. As datas de faturas encerradas permanecem imutáveis, preservando
    /// a integridade histórica dos dados financeiros.
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
            var faturas = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .Where(f => f.CartaoCredito.UsuarioId == usuarioId)
                .OrderByDescending(f => f.DataInicio)
                .Select(f => new FaturaResponseDTO
                {
                    Id = f.Id,
                    CartaoCreditoId = f.CartaoCreditoId,
                    CartaoNome = f.CartaoCredito.Nome,
                    DataInicio = f.DataInicio,
                    DataFechamento = f.DataFechamento,
                    DataVencimento = f.DataVencimento,
                    ValorTotal = f.ValorTotal,
                    ValorPago = f.ValorPago,
                    Status = f.Status.ToString()
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
            var fatura = await _financasDbContext.Fatura
                .Include(f => f.CartaoCredito)
                .FirstOrDefaultAsync(f => f.Id == faturaId && f.CartaoCredito.UsuarioId == usuarioId);

            if (fatura == null)
                throw new KeyNotFoundException("Fatura não encontrada.");

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

            decimal totalPago = 0;
            if (pagamentos.Any())
                totalPago = pagamentos.Sum(p => p.ValorPago);

            var saldoRestante = fatura.ValorTotal - totalPago;

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
        /// Obtém os dados da fatura aberta do cartão para retorno à interface de usuário (DTO).
        /// Cria automaticamente uma nova fatura aberta caso ainda não exista.
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário proprietário.</param>
        /// <param name="dataCompra">Parâmetro mantido por compatibilidade; não influencia a seleção da fatura.</param>
        /// <returns>Objeto de resposta formatado com os dados da fatura aberta.</returns>
        public async Task<FaturaResponseDTO> ObterOuCriarFaturaAtual(int cartaoId, int usuarioId, DateTime dataCompra)
        {
            var fatura = await ObterOuCriarFaturaAbertaEntidade(cartaoId, usuarioId);

            return await MapearParaResponseDTO(fatura);
        }

        /// <summary>
        /// Localiza a fatura aberta do cartão ou cria uma nova automaticamente.
        /// Todo lançamento de cartão deve ser associado à única fatura aberta existente.
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário para validação de segurança.</param>
        /// <param name="dataCompra">Parâmetro mantido por compatibilidade; não influencia a seleção da fatura.</param>
        /// <returns>A entidade de Fatura aberta (existente ou recém-criada).</returns>
        /// <exception cref="KeyNotFoundException">Lançada se o cartão não for localizado.</exception>
        public async Task<Fatura> ObterOuCriarFaturaAtualEntidade(int cartaoId, int usuarioId, DateTime dataCompra)
        {
            return await ObterOuCriarFaturaAbertaEntidade(cartaoId, usuarioId);
        }

        /// <summary>
        /// Localiza a fatura aberta do cartão ou cria uma nova automaticamente.
        /// </summary>
        /// <param name="cartaoId">ID do cartão de crédito.</param>
        /// <param name="usuarioId">ID do usuário para validação de segurança.</param>
        /// <returns>A entidade de Fatura aberta (existente ou recém-criada).</returns>
        /// <exception cref="KeyNotFoundException">Lançada se o cartão não for localizado.</exception>
        public async Task<Fatura> ObterOuCriarFaturaAbertaEntidade(int cartaoId, int usuarioId)
        {
            var cartao = await _financasDbContext.CartaoCredito
                .FirstOrDefaultAsync(c => c.Id == cartaoId && c.UsuarioId == usuarioId);

            if (cartao == null)
                throw new KeyNotFoundException("Cartão não encontrado.");

            var faturaAberta = await _financasDbContext.Fatura
                .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartaoId && f.Status == FaturaStatus.Aberta);

            if (faturaAberta != null)
                return faturaAberta;

            return await CriarNovaFaturaAberta(cartao);
        }

        /// <summary>
        /// Calcula o próximo vencimento válido com base no dia de vencimento configurado no cartão.
        /// </summary>
        /// <param name="cartao">Cartão de crédito com a configuração de vencimento.</param>
        /// <param name="referencia">Data de referência para o cálculo (padrão: momento atual).</param>
        /// <returns>A data de vencimento calculada.</returns>
        private static DateTime CalcularProximoVencimento(CartaoCredito cartao, DateTime referencia)
        {
            var ano = referencia.Year;
            var mes = referencia.Month;
            var ultimoDiaMes = DateTime.DaysInMonth(ano, mes);
            var diaAjustado = Math.Min(cartao.DiaVencimento, ultimoDiaMes);
            var vencimentoEsteMes = new DateTime(ano, mes, diaAjustado, 23, 59, 59, 999);

            if (referencia <= vencimentoEsteMes)
                return vencimentoEsteMes;

            var proximoMes = referencia.AddMonths(1);
            var ultimoDiaProximoMes = DateTime.DaysInMonth(proximoMes.Year, proximoMes.Month);
            var diaProximoMes = Math.Min(cartao.DiaVencimento, ultimoDiaProximoMes);

            return new DateTime(proximoMes.Year, proximoMes.Month, diaProximoMes, 23, 59, 59, 999);
        }

        /// <summary>
        /// Cria uma nova fatura aberta para o cartão informado e persiste no banco de dados.
        /// </summary>
        /// <param name="cartao">Cartão de crédito ao qual a fatura será vinculada.</param>
        /// <returns>A entidade de Fatura recém-criada.</returns>
        private async Task<Fatura> CriarNovaFaturaAberta(CartaoCredito cartao)
        {
            var fatura = CriarNovaFaturaAbertaEntidade(cartao);
            await _financasDbContext.SaveChangesAsync();
            return fatura;
        }

        /// <summary>
        /// Monta a entidade de uma nova fatura aberta sem persistir (útil dentro de transações).
        /// </summary>
        /// <param name="cartao">Cartão de crédito ao qual a fatura será vinculada.</param>
        /// <returns>A entidade de Fatura pronta para ser adicionada ao contexto.</returns>
        private Fatura CriarNovaFaturaAbertaEntidade(CartaoCredito cartao)
        {
            var agora = DateTime.Now;

            var fatura = new Fatura
            {
                CartaoCreditoId = cartao.Id,
                Competencia = new DateTime(agora.Year, agora.Month, 1),
                DataInicio = agora,
                DataFechamento = null,
                DataVencimento = CalcularProximoVencimento(cartao, agora),
                ValorTotal = 0,
                ValorPago = 0,
                Status = FaturaStatus.Aberta
            };

            _financasDbContext.Fatura.Add(fatura);
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
            using var transaction = await _financasDbContext.Database.BeginTransactionAsync();

            try
            {
                var fatura = await _financasDbContext.Fatura
                    .Include(f => f.CartaoCredito)
                    .FirstOrDefaultAsync(f => f.Id == dto.FaturaId);

                if (fatura == null)
                    throw new KeyNotFoundException("Fatura não encontrada.");

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

                if (valorPagamentoArredondado > valorRestante)
                    throw new ArgumentException("Valor pago excede o valor restante da fatura.");

                if (dto.ContaBancariaId.HasValue)
                {
                    var conta = await _financasDbContext.ContasBancarias
                        .FirstOrDefaultAsync(c => c.Id == dto.ContaBancariaId.Value && c.UsuarioId == usuarioId);

                    if (conta == null)
                        throw new KeyNotFoundException("Conta bancária não encontrada.");

                    if (conta.Saldo < dto.ValorPago)
                        throw new InvalidOperationException("Saldo insuficiente.");

                    conta.Saldo -= dto.ValorPago;
                }

                var pagamento = new PagamentoFatura
                {
                    FaturaId = fatura.Id,
                    ValorPago = dto.ValorPago,
                    DataPagamento = dto.DataPagamento == default
                            ? DateTime.Now
                            : dto.DataPagamento,
                    ContaBancariaId = dto.ContaBancariaId,
                    Observacao = dto.Observacao
                };

                _financasDbContext.PagamentoFatura.Add(pagamento);

                fatura.ValorPago += dto.ValorPago;

                fatura.Status = fatura.ValorPago >= fatura.ValorTotal
                    ? FaturaStatus.Paga
                    : FaturaStatus.Fechada;

                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Encerra manualmente a fatura aberta do cartão e abre automaticamente uma nova fatura.
        /// As datas da fatura encerrada são congeladas definitivamente, preservando o histórico.
        /// Utiliza transação para garantir que o fechamento e a criação da nova ocorram simultaneamente.
        /// </summary>
        /// <param name="faturaId">Identificador da fatura a ser encerrada.</param>
        /// <param name="usuarioId">Identificador do usuário para validação de segurança.</param>
        /// <exception cref="KeyNotFoundException">Lançada se a fatura não existir.</exception>
        /// <exception cref="UnauthorizedAccessException">Lançada se a fatura pertencer a outro usuário.</exception>
        /// <exception cref="InvalidOperationException">Lançada se a fatura não puder ser fechada por regras de status ou data.</exception>
        public async Task FecharFatura(int faturaId, int usuarioId)
        {
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

                if (fatura.Status == FaturaStatus.Fechada || fatura.Status == FaturaStatus.Paga)
                    throw new InvalidOperationException("Somente faturas abertas podem ser fechadas.");

                if (fatura.Status != FaturaStatus.Aberta)
                    throw new InvalidOperationException("Somente faturas abertas podem ser fechadas.");

                var agora = DateTime.Now;

                if (fatura.DataInicio.Year == agora.Year && fatura.DataInicio.Month == agora.Month)
                    throw new InvalidOperationException("Não é possível fechar a fatura no mesmo mês em que foi iniciada.");

                if (agora >= fatura.DataVencimento)
                    throw new InvalidOperationException("Não é possível fechar a fatura após ou no dia do vencimento.");

                fatura.DataFechamento = agora;
                fatura.Status = FaturaStatus.Fechada;

                var existeOutraAberta = await _financasDbContext.Fatura
                    .AnyAsync(f => f.CartaoCreditoId == fatura.CartaoCreditoId
                        && f.Status == FaturaStatus.Aberta
                        && f.Id != fatura.Id);

                if (!existeOutraAberta)
                    CriarNovaFaturaAbertaEntidade(fatura.CartaoCredito);

                await _financasDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
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
                .OrderByDescending(f => f.DataInicio)
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

        /// <summary>
        /// Mapeia uma entidade <see cref="Fatura"/> para o DTO de resposta.
        /// </summary>
        private async Task<FaturaResponseDTO> MapearParaResponseDTO(Fatura fatura)
        {
            if (fatura.CartaoCredito == null)
            {
                await _financasDbContext.Entry(fatura)
                    .Reference(f => f.CartaoCredito)
                    .LoadAsync();
            }

            return new FaturaResponseDTO
            {
                Id = fatura.Id,
                CartaoCreditoId = fatura.CartaoCreditoId,
                CartaoNome = fatura.CartaoCredito.Nome,
                DataInicio = fatura.DataInicio,
                DataFechamento = fatura.DataFechamento,
                DataVencimento = fatura.DataVencimento,
                ValorTotal = fatura.ValorTotal,
                ValorPago = fatura.ValorPago,
                Status = fatura.Status.ToString()
            };
        }
    }
}
