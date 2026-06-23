using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Metas
{
    /// <summary>
    /// DTO utilizado na criação de uma meta financeira.
    /// Suporta dois tipos de meta com regras de campos exclusivas por tipo:
    /// - Despesa: permite CategoriaId e CartaoCreditoId; não permite ContaBancariaId.
    /// - Patrimônio: permite ContaBancariaId (opcional); não permite CategoriaId nem CartaoCreditoId.
    /// </summary>
    public class CriarMetaGastoDTO : IValidatableObject
    {
        /// <summary>
        /// Nome descritivo da meta de gasto.
        /// Usado para identificação da meta na interface do usuário.
        /// </summary>
        [Required(ErrorMessage = "O nome da meta é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O nome da meta não pode ultrapassar 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Define o tipo da meta: Despesa ou Patrimônio.
        /// Esse campo influencia diretamente nas regras de campos permitidos e no cálculo de progresso.
        /// </summary>
        [Required(ErrorMessage = "O tipo da meta é obrigatório.")]
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor máximo definido para a meta.
        /// Deve obrigatoriamente ser maior que zero.
        /// </summary>
        [Required(ErrorMessage = "O valor da meta é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da meta deve ser maior que zero.")]
        public decimal ValorMeta { get; set; }

        /// <summary>
        /// Data de início do período de monitoramento da meta.
        /// </summary>
        [Required(ErrorMessage = "A data de início é obrigatória.")]
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do período de monitoramento da meta.
        /// </summary>
        [Required(ErrorMessage = "A data de término é obrigatória.")]
        public DateTime DataFinal { get; set; }


        /// <summary>
        /// Identificador opcional da categoria monitorada pela meta.
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
        /// Quando informado, o progresso é calculado com base no saldo dessa conta.
        /// Quando nulo, o progresso considera a soma de saldo de todas as contas do usuário.
        /// Válido apenas para metas do tipo Patrimônio.
        /// </summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>
        /// Valida a consistência dos campos em relação ao TipoMeta informado.
        /// Impede combinações inválidas como ContaBancariaId em metas de Despesa
        /// ou CategoriaId/CartaoCreditoId em metas de Patrimônio.
        /// </summary>
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