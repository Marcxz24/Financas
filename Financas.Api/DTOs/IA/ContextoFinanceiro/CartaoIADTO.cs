namespace Financas.Api.DTOs.IA.ContextoFinanceiro
{
    /// <summary>
    /// Representa um cartão de crédito no contexto enviado à IA.
    /// Inclui informações de limite e uso para que a IA possa
    /// avaliar a saúde do crédito do usuário.
    /// </summary>
    public class CartaoIADTO
    {
        /// <summary>Nome descritivo do cartão (ex: "Nubank", "Visa Platinum").</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Limite total de crédito aprovado.</summary>
        public decimal Limite { get; set; }

        /// <summary>Valor total em aberto (soma das faturas abertas).</summary>
        public decimal TotalEmAberto { get; set; }

        /// <summary>Limite ainda disponível para uso (Limite - TotalEmAberto).</summary>
        public decimal LimiteDisponivel { get; set; }

        /// <summary>Status atual do cartão (ex: "Ativo", "Bloqueado").</summary>
        public string Status { get; set; } = string.Empty;
    }
}
