namespace Financas.Api.DTOs.IA.ContextoFinanceiro
{
    /// <summary>
    /// Representa o contexto financeiro completo do usuário que será
    /// serializado em prompt para o modelo de linguagem (LLM).
    /// Montado exclusivamente pelo IAService consultando o banco de dados
    /// via Entity Framework Core — jamais preenchido pelo Front-end.
    /// </summary>
    public class ContextoFinanceiroDTO
    {
        // ── Identificação e período ─────────────────────────────────────────

        /// <summary>Nome de exibição do usuário autenticado.</summary>
        public string NomeUsuario { get; set; } = string.Empty;

        /// <summary>Mês de referência para o contexto (1-12).</summary>
        public int MesReferencia { get; set; }

        /// <summary>Ano de referência para o contexto.</summary>
        public int AnoReferencia { get; set; }

        // ── Resumo mensal ───────────────────────────────────────────────────

        /// <summary>Total de receitas registradas no mês de referência.</summary>
        public decimal TotalReceitasMes { get; set; }

        /// <summary>Total de despesas registradas no mês de referência.</summary>
        public decimal TotalDespesasMes { get; set; }

        /// <summary>Saldo mensal calculado (Receitas - Despesas).</summary>
        public decimal SaldoMensal { get; set; }

        // ── Patrimônio e contas ─────────────────────────────────────────────

        /// <summary>Soma do saldo de todas as contas bancárias do usuário.</summary>
        public decimal PatrimonioTotal { get; set; }

        /// <summary>Quantidade de contas bancárias cadastradas.</summary>
        public int QuantidadeContas { get; set; }

        /// <summary>Lista estruturada de contas bancárias com saldos individuais.</summary>
        public List<ContaIADTO> ContasBancarias { get; set; } = new();

        // ── Cartões de crédito ──────────────────────────────────────────────

        /// <summary>Soma de todas as faturas abertas dos cartões do usuário.</summary>
        public decimal TotalFaturasAbertas { get; set; }

        /// <summary>Soma do limite total disponível em todos os cartões ativos.</summary>
        public decimal LimiteTotalDisponivel { get; set; }

        /// <summary>Lista estruturada de cartões com uso e limite disponível.</summary>
        public List<CartaoIADTO> CartaoCreditos { get; set; } = new();

        // ── Metas financeiras ───────────────────────────────────────────────

        /// <summary>Quantidade total de metas financeiras ativas no período.</summary>
        public int QuantidadeMetas { get; set; }

        /// <summary>Quantidade de metas com status "Estourado".</summary>
        public int MetasEstouradas { get; set; }

        /// <summary>Quantidade de metas com status "Atenção" (acima de 80%).</summary>
        public int MetasEmAtencao { get; set; }

        /// <summary>Lista estruturada de metas com progresso individual.</summary>
        public List<MetaIADTO> Metas { get; set; } = new();

        // ── Lançamentos recentes ────────────────────────────────────────────

        /// <summary>
        /// Últimos 15 lançamentos do usuário ordenados por data decrescente.
        /// Limitado para controlar o consumo de tokens no prompt.
        /// </summary>
        public List<LancamentoIADTO> UltimosLancamentos { get; set; } = new();

        // ── Indicadores calculados ──────────────────────────────────────────

        /// <summary>Indicadores financeiros calculados para análise da IA.</summary>
        public IndicadoresFinanceirosIADTO Indicadores { get; set; } = new();
    }
}
