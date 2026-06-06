namespace Financas.Api.DTOs.Lancamento
{
    /// <summary>
    /// Representa um lançamento individual (pagamento ou crédito) que compõe o extrato da fatura.
    /// </summary>
    public class LancamentoExtratoDTO
    {
        /// <summary>
        /// Identificador único do lançamento.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Descrição detalhada do lançamento ou transação.
        /// </summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Valor monetário da transação.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Data e hora em que o lançamento foi realizado.
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Classificação do tipo de lançamento (ex: 'Pagamento', 'Estorno', 'Crédito').
        /// </summary>
        public string Tipo { get; set; } = string.Empty;
    }
}
