using Financas.Api.Entities.Enums;

namespace Financas.Api.DTOs.MetasGasto
{
    /// <summary>
    /// DTO completo retornado nas operações de criação, atualização e consulta de uma meta.
    /// Inclui todos os campos de vínculo, progresso calculado e status atual da meta.
    /// </summary>
    public class MetaGastoResponseDTO
    {
        /// <summary>Identificador único da meta.</summary>
        public int Id { get; set; }

        /// <summary>Nome da meta definido pelo usuário.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Valor total estabelecido como objetivo da meta.</summary>
        public decimal ValorMeta { get; set; }

        /// <summary>Tipo da meta: Despesa ou Patrimônio.</summary>
        public TipoMeta TipoMeta { get; set; }

        /// <summary>
        /// Valor de progresso calculado conforme o tipo da meta:
        /// - Despesa: soma das transações no período filtradas por categoria/cartão.
        /// - Patrimônio: saldo da conta vinculada ou soma de todas as contas do usuário.
        /// </summary>
        public decimal ValorAtual { get; set; }

        /// <summary>Percentual de progresso em relação ao ValorMeta (0 a N).</summary>
        public decimal PercentualUtilizado { get; set; }

        /// <summary>Status calculado com base no tipo e percentual de progresso.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Data de início do período de vigência da meta.</summary>
        public DateTime DataInicio { get; set; }

        /// <summary>Data de término do período de vigência da meta.</summary>
        public DateTime DataFinal { get; set; }

        /// <summary>Identificador da categoria vinculada (somente Despesa).</summary>
        public int? CategoriaId { get; set; }

        /// <summary>Nome da categoria vinculada, quando aplicável.</summary>
        public string? CategoriaNome { get; set; }

        /// <summary>Identificador do cartão de crédito vinculado (somente Despesa).</summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>Nome do cartão de crédito vinculado, quando aplicável.</summary>
        public string? CartaoNome { get; set; }

        /// <summary>
        /// Identificador da conta bancária vinculada (somente Patrimônio).
        /// Quando nulo, o progresso considera todas as contas do usuário.
        /// </summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>Nome da conta bancária vinculada, quando aplicável.</summary>
        public string? ContaNome { get; set; }
    }
}