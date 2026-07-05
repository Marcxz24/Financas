namespace Financas.Api.DTOs.Transferencia
{
    /// <summary>
    /// DTO utilizado para exibição resumida das transferências.
    /// Ideal para listagens e histórico.
    /// </summary>
    public class TransferenciaResumoDTO
    {
        /// <summary>
        /// Identificador da transferência.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Data em que a transferência foi realizada.
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
        /// Valor transferido.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Observação cadastrada pelo usuário.
        /// </summary>
        public string? Observacao { get; set; }
    }
}