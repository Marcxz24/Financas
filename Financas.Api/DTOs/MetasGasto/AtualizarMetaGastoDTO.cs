using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Metas
{
    public class AtualizarMetaGastoDTO
    {
        /// <summary>
        /// Nome descritivo da meta de gasto.
        /// Serve para identificação da meta pelo usuário na interface.
        /// </summary>
        [Required(ErrorMessage = "O nome da meta é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O nome da meta não pode ultrapassar 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Identificador opcional da categoria vinculada à meta.
        /// Usado para filtrar e agrupar lançamentos relacionados.
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Identificador opcional do cartão de crédito associado à meta.
        /// Permite limitar a meta a gastos de um cartão específico.
        /// </summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>
        /// Valor total definido como objetivo da meta.
        /// Deve ser maior que zero.
        /// </summary>
        [Required(ErrorMessage = "O valor da meta é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da meta deve ser maior que zero.")]
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Data de início do período em que a meta passa a ser considerada nos cálculos.
        /// </summary>
        [Required(ErrorMessage = "A data de incio é obrigatória.")]
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data final do período de validade da meta.
        /// Após essa data, a meta deixa de ser contabilizada.
        /// </summary>
        [Required(ErrorMessage = "A data de termino é obrigatória.")]
        public DateTime DataFinal { get; set; }

        /// <summary>
        /// Tipo da meta, define se será baseada em despesas ou receitas.
        /// Impacta diretamente no cálculo de progresso.
        /// </summary>
        [Required(ErrorMessage = "O Tipo da meta é obrigatório")]
        public TipoMeta TipoMeta { get; set; }
    }
}