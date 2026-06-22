using Financas.Api.Entities.Enums;

namespace Financas.Api.DTOs.MetasGasto
{
    /// <summary>
    /// DTO completo utilizado para detalhar uma meta de gasto.
    /// Retorna todas as informações relevantes, incluindo valores, período e vínculos.
    /// </summary>
    public class MetaGastoResponseDTO
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
        /// Valor total estabelecido como objetivo da meta.
        /// </summary>
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Tipo da meta, define se é baseada em despesas ou receitas.
        /// </summary>
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor acumulado até o momento dentro do período da meta.
        /// </summary>
        public decimal ValorGastoAtual { get; set; }

        /// <summary>
        /// Data de início do período em que a meta está ativa.
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do período de vigência da meta.
        /// </summary>
        public DateTime DataFinal { get; set; }

        /// <summary>
        /// Identificador opcional da categoria vinculada à meta.
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Nome da categoria associada à meta, quando aplicável.
        /// </summary>
        public string? CategoriaNome { get; set; }

        /// <summary>
        /// Identificador opcional do cartão de crédito vinculado à meta.
        /// </summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>
        /// Nome do cartão de crédito associado à meta, quando aplicável.
        /// </summary>
        public string? CartaoNome { get; set; }

        /// <summary>
        /// Percentual de utilização da meta com base no valor acumulado.
        /// </summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>
        /// Status calculado da meta (ex: dentro do limite, atenção, estourado).
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}