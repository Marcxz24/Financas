namespace Financas.Api.DTOs.Fatura
{
    /// <summary>
    /// DTO de resposta com os dados resumidos de uma fatura de cartão de crédito.
    /// </summary>
    public class FaturaResponseDTO
    {
        public int Id { get; set; }

        public int CartaoCreditoId { get; set; }

        /// <summary>
        /// Nome do cartão de crédito vinculado à fatura.
        /// </summary>
        public string CartaoNome { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de fechamento manual. Nulo enquanto a fatura estiver aberta.
        /// </summary>
        public DateTime? DataFechamento { get; set; }

        public DateTime DataVencimento { get; set; }

        public decimal ValorTotal { get; set; }

        public decimal ValorPago { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
