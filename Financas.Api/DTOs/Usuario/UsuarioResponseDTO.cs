namespace Financas.Api.DTOs.Usuario
{
    public class UsuarioResponseDTO
    {
        // Propriedades para retornar informações do usuário
        public int Id { get; set; }

        // O nome de usuário é útil para exibir informações do usuário, mas não deve ser usado para autenticação
        public string Username { get; set; } = string.Empty;

        // O email é útil para exibir informações do usuário, mas não deve ser usado para autenticação
        public string Email { get; set; } = string.Empty;

        // Indica se o email do usuário foi confirmado, o que pode ser importante para funcionalidades que exigem um email verificado
        public bool EmailConfirmado { get; set; }

        // A data de cadastro pode ser útil para exibir informações sobre quando o usuário se registrou no sistema
        public DateTime DataCadastro { get; set; }
    }
}
