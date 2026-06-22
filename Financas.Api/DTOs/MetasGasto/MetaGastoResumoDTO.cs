using Financas.Api.Entities.Enums;

namespace Financas.Api.DTOs.MetasGasto
{
    /// <summary>
    /// DTO utilizado para exibição resumida de metas de gasto.
    /// Ideal para cards e componentes visuais compactos.
    /// </summary>
    public class MetaGastoResumoDTO
    {
        /// <summary>
        /// Identificador único da meta.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da meta definido pelo usuário.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Valor total definido como objetivo da meta.
        /// </summary>
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Tipo da meta, indicando se é baseada em despesas ou receitas.
        /// </summary>
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor acumulado até o momento dentro do período da meta.
        /// </summary>
        public decimal ValorGastoAtual { get; set; }

        /// <summary>
        /// Percentual de utilização da meta em relação ao valor definido (0 a 100).
        /// </summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>
        /// Status calculado da meta com base no progresso atual.
        /// Pode indicar dentro do limite, atenção ou estourada.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}