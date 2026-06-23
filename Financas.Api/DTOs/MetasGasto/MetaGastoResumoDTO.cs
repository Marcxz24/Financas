using Financas.Api.Entities.Enums;

namespace Financas.Api.DTOs.MetasGasto
{
    /// <summary>
    /// DTO utilizado para exibição resumida de metas de gasto.
    /// Ideal para cards e componentes visuais compactos em dashboards.
    /// </summary>
    public class MetaGastoResumoDTO
    {
        /// <summary>Identificador único da meta.</summary>
        public int Id { get; set; }

        /// <summary>Nome da meta definido pelo usuário.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Valor total definido como objetivo da meta.</summary>
        public decimal ValorMeta { get; set; }

        /// <summary>Tipo da meta: Despesa ou Patrimônio.</summary>
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor de progresso calculado conforme o tipo da meta.
        /// </summary>
        public decimal ValorAtual { get; set; }

        /// <summary>Percentual de utilização da meta em relação ao valor definido.</summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>
        /// Status calculado da meta com base no tipo e percentual de progresso.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}