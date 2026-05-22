using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Usuario
{
    /// <summary>
    /// Objeto de Transferência de Dados (DTO) utilizado no fluxo de alteração de credenciais.
    /// Mapeia e valida as regras de negócio para a atualização exclusiva do nome de usuário via requisição HTTP PATCH.
    /// </summary>
    public class AtualizarUsernameDTO
    {
        /// <summary>
        /// Novo nome de usuário desejado para a conta.
        /// </summary>
        [Required(ErrorMessage = "Username é obrigatório")]
        [MinLength(6, ErrorMessage = "O usuário deve conter mais de 5 caracteres.")]
        [MaxLength(100, ErrorMessage = "O usuário deve conter no máximo 100 caracteres.")]
        public string Username { get; set; } = string.Empty;
    }
}
