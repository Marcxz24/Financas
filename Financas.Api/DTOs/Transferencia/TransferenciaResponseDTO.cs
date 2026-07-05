namespace Financas.Api.DTOs.Transferencia
{
    /// <summary>
    /// DTO retornado após a realização de uma transferência entre contas bancárias.
    /// </summary>
    public class TransferenciaResponseDTO
    {
        /// <summary>
        /// Identificador da transferência.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Valor transferido.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Data da transferência.
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Nome da conta bancária de origem.
        /// </summary>
        public string ContaOrigem { get; set; } = string.Empty;

        /// <summary>
        /// Nome da conta bancária de destino.
        /// </summary>
        public string ContaDestino { get; set; } = string.Empty;

        /// <summary>
        /// Observação da transferência.
        /// </summary>
        public string? Observacao { get; set; }
    }
}