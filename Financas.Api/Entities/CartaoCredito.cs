using Financas.Api.Entities.Enums;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Representa a entidade de Cartão de Crédito do sistema.
    /// Armazena as configurações permanentes de limite, vencimento e vínculo com o usuário.
    /// O ciclo de cobrança é gerenciado exclusivamente pela entidade <see cref="Fatura"/>.
    /// </summary>
    public class CartaoCredito
    {
        /// <summary>
        /// Identificador único (Chave Primária) do cartão no banco de dados.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do usuário proprietário deste cartão.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Nome descritivo para identificação do cartão (ex: Nubank, Visa Infinite).
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Limite total de crédito aprovado para o cartão.
        /// </summary>
        public decimal Limite { get; set; }

        /// <summary>
        /// Dia do mês (1 a 31) em que a fatura vence.
        /// Utilizado apenas para calcular o vencimento de novas faturas abertas.
        /// Alterações neste valor não afetam faturas já existentes.
        /// </summary>
        public int DiaVencimento { get; set; }

        /// <summary>
        /// Define a situação atual do cartão (ex: Ativo, Bloqueado).
        /// </summary>
        public StatusCartaoCredito Status { get; set; }

        /// <summary>
        /// Referência virtual para o objeto do Usuário dono do cartão.
        /// </summary>
        public virtual Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Representa a coleção de faturas associadas a este cartão de crédito.
        /// </summary>
        public virtual ICollection<Fatura> Faturas { get; set; } = new List<Fatura>();

        /// <summary>
        /// Representa a coleção de lançamentos associados a este cartão de crédito.
        /// </summary>
        public virtual ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    }
}
