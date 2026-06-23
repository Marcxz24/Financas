using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Metas
{
    /// <summary>
    /// DTO utilizado na atualização de uma meta financeira existente.
    /// Aplica as mesmas regras de exclusividade de campos por TipoMeta do DTO de criação.
    /// </summary>
    public class AtualizarMetaGastoDTO : IValidatableObject
    {
        /// <summary>
        /// Nome descritivo da meta de gasto.
        /// </summary>
        [Required(ErrorMessage = "O nome da meta é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O nome da meta não pode ultrapassar 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Tipo da meta: Despesa ou Patrimônio.
        /// </summary>
        [Required(ErrorMessage = "O tipo da meta é obrigatório.")]
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor total definido como objetivo da meta. Deve ser maior que zero.
        /// </summary>
        [Required(ErrorMessage = "O valor da meta é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da meta deve ser maior que zero.")]
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Data de início do período de vigência da meta.
        /// </summary>
        [Required(ErrorMessage = "A data de início é obrigatória.")]
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do período de vigência da meta.
        /// </summary>
        [Required(ErrorMessage = "A data de término é obrigatória.")]
        public DateTime DataFinal { get; set; }

        /// <summary>
        /// Identificador opcional da categoria associada à meta.
        /// Válido apenas para metas do tipo Despesa.
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Identificador opcional do cartão de crédito associado à meta.
        /// Válido apenas para metas do tipo Despesa.
        /// </summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>
        /// Identificador opcional da conta bancária vinculada à meta.
        /// Válido apenas para metas do tipo Patrimônio.
        /// Quando nulo, considera a soma de todas as contas do usuário.
        /// </summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>
        /// Valida as regras de exclusividade de campos com base no TipoMeta.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TipoMeta == TipoMeta.Despesa)
            {
                if (ContaBancariaId.HasValue)
                    yield return new ValidationResult(
                        "ContaBancariaId não é permitido em metas do tipo Despesa.",
                        new[] { nameof(ContaBancariaId) });
            }

            if (TipoMeta == TipoMeta.Receita)
            {
                if (CategoriaId.HasValue)
                    yield return new ValidationResult(
                        "CategoriaId não é permitido em metas do tipo Patrimônio.",
                        new[] { nameof(CategoriaId) });

                if (CartaoCreditoId.HasValue)
                    yield return new ValidationResult(
                        "CartaoCreditoId não é permitido em metas do tipo Patrimônio.",
                        new[] { nameof(CartaoCreditoId) });
            }
        }
    }
}