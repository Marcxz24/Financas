using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Cofrinho
{
    /// <summary>
    /// DTO utilizado para representar uma transferência de valores entre
    /// uma conta bancária e um cofrinho.
    /// A mesma estrutura é utilizada tanto para depósitos no cofrinho
    /// quanto para resgates para a conta bancária.
    /// </summary>
    public class TransferenciaCofrinhoDTO
    {
        /// <summary>
        /// Identificador da conta bancária envolvida na operação.
        /// Representa a origem ou o destino da transferência,
        /// dependendo do tipo de operação realizada.
        /// </summary>
        [Required(ErrorMessage = "A conta bancária é obrigatória.")]
        public int ContaBancariaId { get; set; }

        /// <summary>
        /// Identificador do cofrinho que participará da transferência.
        /// Representa o destino em depósitos e a origem em resgates.
        /// </summary>
        [Required(ErrorMessage = "O cofrinho é obrigatório.")]
        public int CofrinhoId { get; set; }

        /// <summary>
        /// Valor monetário que será movimentado entre a conta bancária
        /// e o cofrinho. Deve ser obrigatoriamente maior que zero.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Valor { get; set; }
    }
}
