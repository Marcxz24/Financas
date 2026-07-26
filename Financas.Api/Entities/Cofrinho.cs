using Financas.Api.Entities.Enums;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Representa um cofrinho financeiro pertencente a um usuário.
    /// Permite separar parte do patrimônio em uma reserva independente,
    /// possibilitando depósitos, resgates e acompanhamento do saldo disponível.
    /// </summary>
    public class Cofrinho
    {
        /// <summary>
        /// Identificador único do cofrinho.
        /// Chave primária utilizada para identificar o registro no banco de dados.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do usuário proprietário do cofrinho.
        /// Utilizado para vincular o cofrinho ao respectivo usuário do sistema.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Navegação para o usuário proprietário do cofrinho.
        /// Permite acesso às informações do usuário através do Entity Framework.
        /// </summary>
        public Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Nome atribuído ao cofrinho.
        /// Facilita a identificação da finalidade da reserva financeira.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Saldo atualmente disponível no cofrinho.
        /// O valor é atualizado conforme operações de depósito e resgate.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Data de criação do cofrinho.
        /// Registrada utilizando o horário local da aplicação (DateTime.Now).
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Situação atual do cofrinho.
        /// Define se o cofrinho está ativo ou inativo para movimentações.
        /// </summary>
        public StatusCofrinho Status { get; set; }

        /// <summary>
        /// Coleção de lançamentos vinculados ao cofrinho.
        /// Contém o histórico das movimentações financeiras relacionadas
        /// às transferências realizadas entre contas bancárias e o cofrinho.
        /// </summary>
        public ICollection<Lancamento> Movimentacoes { get; set; } = new List<Lancamento>();
    }
}
