using System.ComponentModel.DataAnnotations;

namespace Financas.Api.DTOs.Usuario
{
    /// <summary>
    /// Objeto de transferência de dados (DTO) utilizado para validar 
    /// a solicitação de definição de uma nova senha.
    /// </summary>
    public class DefinirSenhaDTO
    {
        // Validação da Nova Senha:
        // [Required]: Garante que o campo não seja nulo ou vazio.
        // [MinLength(6)]: Impõe uma regra de segurança mínima de complexidade.
        [Required(ErrorMessage = "A Senha é Obrigatória.")]
        [MinLength(6, ErrorMessage = "A nova senha deve conter no mínimo 6 caracteres")]
        public string NewPassword { get; set; } = string.Empty;

        // Validação da Confirmação:
        // [Required]: Garante que a confirmação também seja preenchida.
        // [Compare]: Verifica automaticamente se este campo é idêntico ao campo 'NewPassword'.
        // Caso contrário, retorna o erro definido sem precisar de lógica manual no Controller.
        [Required(ErrorMessage = "A confirmação da nova senha é obrigatória")]
        [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação devem ser iguais")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
