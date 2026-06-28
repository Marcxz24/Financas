namespace Financas.Api.DTOs.IA.ContextoFinanceiro
{
    /// <summary>
    /// Representa uma meta financeira no contexto enviado à IA.
    /// Inclui progresso atual para que a IA possa orientar o usuário
    /// sobre metas próximas de estourar ou em risco.
    /// </summary>
    public class MetaIADTO
    {
        /// <summary>Nome identificador da meta (ex: "Limite de Supermercado").</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Tipo da meta ("Despesa" ou "Receita/Patrimônio").</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Valor alvo definido pelo usuário para a meta.</summary>
        public decimal ValorMeta { get; set; }

        /// <summary>Valor atual atingido no período da meta.</summary>
        public decimal ValorAtual { get; set; }

        /// <summary>Percentual de utilização em relação à meta (0 a 100+).</summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>Status textual da meta (ex: "Estourado", "Dentro do limite").</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Data de início do período de vigência da meta.</summary>
        public DateTime DataInicio { get; set; }

        /// <summary>Data de encerramento do período de vigência da meta.</summary>
        public DateTime DataFinal { get; set; }

        /// <summary>Nome da categoria vinculada, se houver.</summary>
        public string? CategoriaNome { get; set; }
    }
}
