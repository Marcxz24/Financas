using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Transferencia
{
    /// <summary>
    /// DTO utilizado para solicitar uma transferência entre contas bancárias.
    /// Contém apenas as informações necessárias para a realização da operação.
    /// </summary>
    public class TransferenciaRequestDTO
    {
        /// <summary>
        /// Identificador da conta bancária de origem.
        /// O saldo será debitado desta conta.
        /// </summary>
        [Required(ErrorMessage = "A conta de origem é obrigatória.")]
        public int ContaOrigemId { get; set; }

        /// <summary>
        /// Identificador da conta bancária de destino.
        /// O saldo será creditado nesta conta.
        /// </summary>
        [Required(ErrorMessage = "A conta de destino é obrigatória.")]
        public int ContaDestinoId { get; set; }

        /// <summary>
        /// Valor da transferência.
        /// Deve ser maior que zero.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da transferência deve ser maior que zero.")]
        public decimal Valor { get; set; }

        /// <summary>
        /// Observação opcional informada pelo usuário.
        /// </summary>
        [StringLength(300, ErrorMessage = "A observação pode conter no máximo 300 caracteres.")]
        public string? Observacao { get; set; }
    }
}