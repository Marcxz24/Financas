namespace Financas.Api.DTOs.CartaoCredito
{
    /// <summary>
    /// DTO de resposta que representa os dados de um Cartão de Crédito para o Front-end.
    /// Retorna as informações formatadas e prontas para exibição.
    /// </summary>
    public class CartaoCreditoResponseDTO
    {
        /// <summary>
        /// Identificador único do cartão.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do usuário dono do cartão.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Nome do cartão (ex: Nubank, Inter).
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Limite total de crédito disponível.
        /// </summary>
        public decimal Limite { get; set; }

        /// <summary>
        /// Valor atualmente comprometido em compras/faturas.
        /// </summary>
        public decimal LimiteUtilizado { get; set; }

        /// <summary>
        /// Valor ainda disponível para novas compras.
        /// </summary>
        public decimal LimiteDisponivel { get; set; }

        /// <summary>
        /// Percentual do limite já utilizado.
        /// </summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>
        /// Dia do mês em que a fatura vence.
        /// </summary>
        public int DiaVencimento { get; set; }

        /// <summary>
        /// Representação textual do status do cartão (ex: "Ativo", "Bloqueado").
        /// Facilitando a leitura pelo Front-end sem necessidade de conversão.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
