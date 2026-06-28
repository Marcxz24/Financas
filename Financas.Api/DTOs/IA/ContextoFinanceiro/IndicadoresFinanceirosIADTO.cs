namespace Financas.Api.DTOs.IA.ContextoFinanceiro
{
    /// <summary>
    /// Representa os indicadores financeiros calculados do usuário.
    /// Dados agregados que oferecem à IA uma visão analítica rápida
    /// sem necessidade de processar todos os lançamentos individualmente.
    /// </summary>
    public class IndicadoresFinanceirosIADTO
    {
        /// <summary>Média mensal de receitas nos últimos 3 meses.</summary>
        public decimal MediaReceitasMensal { get; set; }

        /// <summary>Média mensal de despesas nos últimos 3 meses.</summary>
        public decimal MediaDespesasMensal { get; set; }

        /// <summary>Percentual de despesas em relação às receitas (0 a 100+).</summary>
        public decimal PercentualGastoSobreReceita { get; set; }

        /// <summary>Maior categoria de gasto no mês atual por valor total.</summary>
        public string? MaiorCategoriaGasto { get; set; }

        /// <summary>Valor total gasto na maior categoria no mês atual.</summary>
        public decimal ValorMaiorCategoriaGasto { get; set; }

        /// <summary>Quantidade de faturas com status "Atrasada".</summary>
        public int FaturasAtrasadas { get; set; }

        /// <summary>Valor total das faturas em atraso.</summary>
        public decimal ValorTotalFaturasAtrasadas { get; set; }
    }
}
