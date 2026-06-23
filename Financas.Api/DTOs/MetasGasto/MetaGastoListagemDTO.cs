using Financas.Api.Entities.Enums;

namespace Financas.Api.DTOs.MetasGasto
{
    /// <summary>
    /// DTO otimizado para listagem de metas de gasto em grids e telas de resumo.
    /// Inclui progresso calculado e status atual de cada meta.
    /// </summary>
    public class MetaGastoListagemDTO
    {
        /// <summary>Identificador único da meta.</summary>
        public int Id { get; set; }

        /// <summary>Nome da meta exibido para o usuário.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Valor total definido como objetivo da meta.</summary>
        public decimal ValorMeta { get; set; }

        /// <summary> Identificador da conta bancaria associada à meta, se houver.</summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>Tipo da meta: Despesa ou Patrimônio.</summary>
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor de progresso calculado conforme o tipo da meta.
        /// </summary>
        public decimal ValorAtual { get; set; }

        /// <summary>Percentual de utilização da meta com base no valor atual.</summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>Data de início do período da meta.</summary>
        public DateTime DataInicio { get; set; }

        /// <summary>Data de término do período da meta.</summary>
        public DateTime DataFinal { get; set; }

        /// <summary>Status calculado da meta (ex: Dentro do limite, Atenção, Estourado).</summary>
        public string Status { get; set; } = string.Empty;
    }
}