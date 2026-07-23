namespace Financas.Api.DTOs.Fatura
{
    /// <summary>
    /// DTO de resposta para faturas encerradas (fechadas ou pagas).
    /// </summary>
    public class FaturaEncerradaDTO
    {
        public int Id { get; set; }

        public string CartaoNome { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }

        public DateTime? DataFechamento { get; set; }

        public DateTime DataVencimento { get; set; }

        public decimal ValorTotal { get; set; }

        public decimal ValorPago { get; set; }

        public decimal SaldoPendente { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
