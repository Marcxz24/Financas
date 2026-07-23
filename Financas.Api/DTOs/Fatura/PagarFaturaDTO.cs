using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Fatura
{
    /// <summary>
    /// DTO utilizado para registrar o pagamento (total ou parcial) de uma fatura de cartão de crédito.
    /// </summary>
    public class PagarFaturaDTO
    {
        /// <summary>
        /// Identificador da fatura que receberá o pagamento.
        /// </summary>
        [Required(ErrorMessage = "O ID da fatura é obrigatório.")]
        public int FaturaId { get; set; }

        /// <summary>
        /// Valor que está sendo pago pelo usuário. 
        /// Deve ser maior que zero e pode ser menor, igual ou (em casos de crédito extra) maior que o ValorTotal da fatura.
        /// </summary>
        [Required(ErrorMessage = "O valor do pagamento é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser um número positivo maior que zero")]
        public decimal ValorPago { get; set; }

        /// <summary>
        /// Data e hora em que o pagamento foi realizado.
        /// Por padrão, utiliza o horário local atual.
        /// </summary>
        public DateTime DataPagamento { get; set; } = DateTime.Now;

        /// <summary>
        /// Identificador da conta bancária de onde sairá o saldo para pagar a fatura.
        /// Se preenchido, o sistema deve subtrair o valor pago do saldo desta conta.
        /// </summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>
        /// Campo de texto livre para anotações extras sobre o pagamento 
        /// (ex: "Pago com bônus", "Comprovante enviado por e-mail").
        /// </summary>
        public string? Observacao { get; set; }
    }
}

