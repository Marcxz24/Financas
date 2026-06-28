namespace Financas.Api.DTOs.IA.ContextoFinanceiro
{
    /// <summary>
    /// Representa uma conta bancária no contexto enviado à IA.
    /// Contém apenas os dados necessários para análise financeira,
    /// sem expor propriedades internas ou de navegação do EF Core.
    /// </summary>
    public class ContaIADTO
    {
        /// <summary>Nome descritivo da conta (ex: "Nubank", "Carteira").</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Tipo da conta (ex: "Digital", "Corrente", "Poupanca").</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Saldo atual disponível na conta.</summary>
        public decimal Saldo { get; set; }
    }
}
