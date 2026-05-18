namespace Financas.Api.DTOs.Usuario
{
    /// <summary>
    /// Objeto de Transferência de Dados (DTO) utilizado para encapsular a credencial de autenticação externa
    /// proveniente do Google Identity Services (OAuth 2.0).
    /// </summary>
    public class GoogleLoginDTO
    {
        /// <summary>
        /// Obtém ou define o Identity Token (JWT) gerado e assinado pelo Google após a autenticação do usuário no front-end.
        /// </summary>
        /// <value>
        /// Uma string representando o token bruto que será submetido à validação criptográfica no back-end.
        /// </value>
        /// <remarks>
        /// Inicializado implicitamente como string vazia (<see cref="string.Empty"/>) para mitigar exceções de referência nula (NullReferenceException).
        /// </remarks>
        public string Token { get; set; } = string.Empty;
    }
}
