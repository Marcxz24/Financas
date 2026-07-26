using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Lancamento
{
    /// <summary>
    /// Objeto de Transferência de Dados para criação de novos lançamentos.
    /// Isolamos a entrada da API da entidade 'Lancamento' para maior segurança e flexibilidade.
    /// </summary>
    public class CriarLancamentoDTO
    {
        /// <summary>
        /// Descrição breve do lançamento. 
        /// Validamos o tamanho para evitar truncamento no banco de dados (Limite: 255).
        /// </summary>
        [Required(ErrorMessage = "A descrição do lançamento é obrigatória")]
        [MaxLength(255, ErrorMessage = "A descrição não pode ultrapassar 255 caracteres")]
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Valor monetário. 
        /// Usamos 'decimal' para precisão financeira absoluta, evitando erros de arredondamento de 'double/float'.
        /// </summary>
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser um número positivo maior que zero")]
        public decimal Valor { get; set; }

        /// <summary>
        /// Data da competência ou do pagamento/recebimento.
        /// </summary>
        [Required(ErrorMessage = "A data do lançamento é obrigatória")]
        public DateTime Data { get; set; }

        /// <summary>
        /// Identifica se é 'Receita (1)' ou 'Despesa (2)'.
        /// O ASP.NET fará o bind automático da string no JSON para o valor numérico do Enum.
        /// </summary>
        [Required(ErrorMessage = "O tipo de lançamento (Receita/Despesa) é obrigatório")]
        [EnumDataType(typeof(TipoLancamento), ErrorMessage = "Tipo de lançamento inválido")]
        public TipoLancamento Tipo { get; set; }

        /// <summary>
        /// Identificador da categoria associada ao lançamento.
        /// Campo opcional — o lançamento pode ser criado sem categoria.
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Identificador da conta bancária onde a movimentação financeira ocorreu.
        /// Sendo opcional (int?), permite flexibilidade caso o lançamento ainda não esteja vinculado a uma conta.
        /// </summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>
        /// Identificador do cofrinho associado ao lançamento.
        /// </summary>
        public int? CofrinhoId { get; set; }

        /// <summary>
        /// Identificador do cartão de crédito utilizado na transação.
        /// Sendo opcional (int?), permite distinguir lançamentos de conta corrente (nulos) de despesas de cartão.
        /// </summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>
        /// Indica o número de parcelas para lançamentos parcelados.
        /// Não sendo obrigatório, o valor padrão é 1 (não parcelado).
        /// </summary>
        [Range(1, 48, ErrorMessage = "O parcelamento deve ser entre 1 e 48 vezes")]
        public int QuantidadeParcelas { get; set; } = 1;
    }
}