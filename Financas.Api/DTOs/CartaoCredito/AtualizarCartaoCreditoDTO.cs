using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.CartaoCredito
{
    /// <summary>
    /// DTO para atualização de dados de um Cartão de Crédito existente.
    /// Todas as propriedades são opcionais para permitir atualizações parciais.
    /// Alterações não modificam faturas já existentes.
    /// </summary>
    public class AtualizarCartaoCreditoDTO
    {
        /// <summary>
        /// Novo nome para o cartão. Se nulo, mantém o nome atual.
        /// </summary>
        [MaxLength(100, ErrorMessage = "O Nome do Cartão não pode ultrapassar 100 Caracteres.")]
        public string? Nome { get; set; }

        /// <summary>
        /// Novo limite de crédito. Se nulo, o limite não será alterado.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "O Limite deve ser um valor positivo.")]
        public decimal? Limite { get; set; }

        /// <summary>
        /// Novo dia de vencimento da fatura (1 a 31).
        /// Afeta apenas faturas criadas após a alteração.
        /// </summary>
        [Range(1, 31, ErrorMessage = "O Dia do Vencimento deve ser entre 1 e 31.")]
        public int? DiaVencimento { get; set; }

        /// <summary>
        /// Altera a situação do cartão (ex: Ativo para Bloqueado).
        /// </summary>
        public StatusCartaoCredito? Status { get; set; }
    }
}
