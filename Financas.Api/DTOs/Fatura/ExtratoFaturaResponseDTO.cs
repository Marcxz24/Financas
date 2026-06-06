using Financas.Api.DTOs.Lancamento;

namespace Financas.Api.DTOs.Fatura
{
    /// <summary>
    /// DTO detalhado que representa o extrato de uma fatura específica.
    /// Consolida os totais financeiros e a lista cronológica de todos os pagamentos realizados.
    /// </summary>
    public class ExtratoFaturaResponseDTO
    {
        /// <summary>
        /// Identificador da fatura consultada.
        /// </summary>
        public int FaturaId { get; set; }

        /// <summary>
        /// Valor bruto total da fatura (soma de todos os lançamentos de compras).
        /// </summary>
        public decimal ValorTotal { get; set; }

        /// <summary>
        /// Soma acumulada de todos os pagamentos já confirmados para este ciclo.
        /// </summary>
        public decimal TotalPago { get; set; }

        /// <summary>
        /// Valor que ainda precisa ser quitado (ValorTotal - TotalPago).
        /// Útil para o Front-end exibir o "Quanto falta pagar".
        /// </summary>
        public decimal SaldoRestante { get; set; }

        /// <summary>
        /// Lista detalhada contendo cada transação de pagamento individual, 
        /// incluindo datas, valores e observações de cada baixa.
        /// </summary>
        public List<PagamentoResponseDTO> Pagamentos { get; set; } = new();

        public List<LancamentoExtratoDTO> Lancamentos { get; set; } = [];
    }
}
