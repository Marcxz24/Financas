using Financas.Api.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Cofrinho
{
    /// <summary>
    /// DTO utilizado para atualização parcial dos dados cadastrais de um cofrinho.
    /// Todos os campos são opcionais, permitindo alterar apenas as informações desejadas.
    /// </summary>
    public class AtualizarCofrinhoDTO
    {
        /// <summary>
        /// Novo nome do cofrinho.
        /// Caso informado, deve possuir no máximo 100 caracteres.
        /// </summary>
        [MaxLength(100, ErrorMessage = "O nome do cofrinho não pode ultrapassar 100 caracteres.")]
        public string? Nome { get; set; }

        /// <summary>
        /// Novo status do cofrinho.
        /// Permite ativar ou desativar o cofrinho sem alterar seus demais dados.
        /// </summary>
        public StatusCofrinho? Status { get; set; }
    }
}
