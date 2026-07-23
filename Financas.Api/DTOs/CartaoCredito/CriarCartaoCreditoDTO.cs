using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.CartaoCredito
{
    /// <summary>
    /// DTO para transporte de dados na criação de um novo Cartão de Crédito.
    /// Contém as validações necessárias para garantir a integridade antes de chegar à Entity.
    /// </summary>
    public class CriarCartaoCreditoDTO
    {
        /// <summary>
        /// Nome descritivo do cartão (ex: Nubank, Inter Black).
        /// Obrigatório e limitado a 100 caracteres para evitar poluição no banco de dados.
        /// </summary>
        [Required(ErrorMessage = "O nome do cartão é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O Nome do Cartão não pode ultrapassar 100 Caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Limite total concedido pela operadora do cartão.
        /// Deve ser um valor maior que zero para permitir lançamentos futuros.
        /// </summary>
        [Required(ErrorMessage = "O limite é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O Limite deve ser um valor positivo.")]
        public decimal Limite { get; set; }

        /// <summary>
        /// Dia do mês (1 a 31) em que a fatura vence.
        /// </summary>
        [Required(ErrorMessage = "O dia de vencimento é obrigatório.")]
        [Range(1, 31, ErrorMessage = "O Dia do Vencimento deve ser entre 1 e 31.")]
        public int DiaVencimento { get; set; }
    }
}
