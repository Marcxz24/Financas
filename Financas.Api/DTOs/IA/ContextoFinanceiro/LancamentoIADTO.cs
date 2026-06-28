namespace Financas.Api.DTOs.IA.ContextoFinanceiro
{
    /// <summary>
    /// Representa um lançamento financeiro recente no contexto enviado à IA.
    /// Contém apenas os campos relevantes para análise de padrão de gastos,
    /// sem expor dados internos como IDs de relacionamento.
    /// </summary>
    public class LancamentoIADTO
    {
        /// <summary>Descrição textual do lançamento (ex: "Aluguel", "Salário").</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Valor monetário da transação.</summary>
        public decimal Valor { get; set; }

        /// <summary>Data em que o lançamento ocorreu.</summary>
        public DateTime Data { get; set; }

        /// <summary>Tipo da transação ("Receita" ou "Despesa").</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Nome da categoria associada ao lançamento, se houver.</summary>
        public string? CategoriaNome { get; set; }

        /// <summary>Nome da conta bancária onde o lançamento foi registrado, se houver.</summary>
        public string? ContaBancariaNome { get; set; }

        /// <summary>Nome do cartão de crédito utilizado, se houver.</summary>
        public string? CartaoCreditoNome { get; set; }
    }
}
