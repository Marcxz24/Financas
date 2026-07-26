using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Representa a entidade de Usuário no sistema.
    /// Armazena as informações de autenticação e o vínculo com os lançamentos financeiros.
    /// </summary>
    public class Usuario
    {
        /// <summary>
        /// Identificador único do usuário (Chave Primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome de exibição ou login do usuário. 
        /// Inicializado como string vazia para evitar alertas de valores nulos.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Endereço de e-mail único utilizado para acesso e comunicações.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Armazena o novo endereço de e-mail solicitado pelo usuário.
        /// O e-mail só será movido para a propriedade principal após a confirmação via token,
        /// evitando que o usuário perca o acesso à conta por erros de digitação ou e-mails falsos.
        /// </summary>
        public string? EmailPendente { get; set; }

        /// <summary>
        /// Senha do usuário (idealmente armazenada como Hash no banco de dados).
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Data e hora em que o usuário foi criado no sistema.
        /// </summary>
        public DateTime DataCadastro { get; set; }

        /// <summary>
        /// Indica se o e-mail do usuário foi confirmado.
        /// Usuários não confirmados não podem fazer login.
        /// </summary>
        public bool EmailConfirmado { get; set; } = false;

        /// <summary>
        /// Token único gerado para confirmação do e-mail.
        /// Enviado por e-mail após o cadastro.
        /// </summary>
        public string? TokenConfirmacao { get; set; }

        /// <summary>
        /// Data e hora de expiração do token de confirmação.
        /// Token expirado obriga o usuário a solicitar um novo.
        /// </summary>
        public DateTime? TokenExpiracao { get; set; }

        /// <summary>
        /// Propriedade de Navegação: representa a coleção de lançamentos vinculados a este usuário.
        /// Define o relacionamento 1:N (Um usuário para muitos lançamentos).
        /// </summary>
        public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();

        /// <summary>
        /// Propriedade de Navegação: representa a coleção de categorias vinculadas a este usuário.
        /// Define o relacionamento 1:N (Um usuário para muitas categorias).
        /// Permite que cada usuário gerencie seu próprio conjunto de categorias de forma isolada.
        /// </summary>
        public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();

        /// <summary>
        /// Coleção de contas bancárias associadas ao usuário. 
        /// Permite que um único perfil gerencie múltiplos ativos financeiros (ex: Corrente, Poupança, Investimentos).
        /// </summary>
        public ICollection<ContaBancaria> ContasBancarias { get; set; } = new List<ContaBancaria>();

        /// <summary>
        /// Coleção de cofrinhos pertencentes ao usuário.
        /// </summary>
        public ICollection<Cofrinho> Cofrinhos { get; set; } = new List<Cofrinho>();
    }
}