using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Metas
{
    public class CriarMetaGastoDTO
    {
        /// <summary>
        /// Nome descritivo da meta de gasto.
        /// Usado para identificação da meta na interface do usuário.
        /// </summary>
        [Required(ErrorMessage = "O nome da meta é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O nome da meta não pode ultrapassar 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Identificador opcional da categoria monitorada pela meta.
        /// Permite segmentar os gastos por tipo específico de despesa.
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Identificador opcional do cartão de crédito associado à meta.
        /// Pode ser usado para restringir o controle de gastos por cartão.
        /// </summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>
        /// Valor máximo definido para a meta de gastos.
        /// Deve obrigatoriamente ser maior que zero.
        /// </summary>
        [Required(ErrorMessage = "O valor da meta é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da meta deve ser maior que zero.")]
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Define o tipo da meta, indicando se o controle será por despesas ou receitas.
        /// Esse campo influencia diretamente na regra de cálculo da meta.
        /// </summary>
        [Required(ErrorMessage = "O Tipo da meta é obrigatório")]
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Data de início do período de monitoramento da meta.
        /// A partir dessa data os lançamentos passam a ser considerados.
        /// </summary>
        [Required(ErrorMessage = "A data de incio é obrigatória.")]
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do período de monitoramento da meta.
        /// Após essa data, os lançamentos deixam de ser contabilizados.
        /// </summary>
        [Required(ErrorMessage = "A data de termino é obrigatória.")]
        public DateTime DataFinal { get; set; }
    }
}