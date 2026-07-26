namespace Financas.Api.DTOs.Cofrinho
{
    /// <summary>
    /// DTO utilizado para retornar os dados de um cofrinho ao cliente.
    /// Contém as principais informações cadastrais e financeiras necessárias
    /// para exibição no Front-End.
    /// </summary>
    public class CofrinhoResponseDTO
    {
        /// <summary>
        /// Identificador único do cofrinho.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do usuário proprietário do cofrinho.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Nome atribuído ao cofrinho pelo usuário.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Saldo atual disponível no cofrinho.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Data e hora em que o cofrinho foi criado.
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Situação atual do cofrinho (Ativo ou Inativo).
        /// O valor é retornado como texto para facilitar a exibição no Front-End.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}