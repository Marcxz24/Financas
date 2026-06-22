using Financas.Api.Entities.Enums;

namespace Financas.Api.DTOs.MetasGasto
{
    /// <summary>
    /// DTO otimizado para listagem de metas de gasto.
    /// Usado principalmente em grids, tabelas e telas de resumo.
    /// </summary>
    public class MetaGastoListagemDTO
    {
        /// <summary>
        /// Identificador único da meta.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da meta exibido para o usuário.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Valor total definido como objetivo da meta.
        /// </summary>
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Tipo da meta, define se é baseada em despesas ou receitas.
        /// </summary>
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor já acumulado até o momento dentro do período da meta.
        /// </summary>
        public decimal ValorGastoAtual { get; set; }

        /// <summary>
        /// Percentual de utilização da meta com base no valor atual.
        /// </summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>
        /// Data de início do período da meta.
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do período da meta.
        /// </summary>
        public DateTime DataFinal { get; set; }

        /// <summary>
        /// Status calculado da meta (ex: dentro do limite, atenção, estourado).
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}