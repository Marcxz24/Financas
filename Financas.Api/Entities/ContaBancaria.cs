using Financas.Api.Entities.Enums;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Representa uma conta bancária vinculada a um usuário, contendo informações de saldo e histórico de lançamentos.
    /// </summary>
    public class ContaBancaria
    {
        /// <summary>
        /// Identificador único da conta (Chave Primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do usuário proprietário da conta (Chave Estrangeira).
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o usuário dono desta conta.
        /// O uso de 'null!' indica ao compilador que o EF Core carregará esta propriedade.
        /// </summary>
        public Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Nome descritivo da conta (ex: "Nubank", "Carteira", "Banco do Brasil").
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Define a modalidade da conta com base no enum TipoContaBancaria.
        /// </summary>
        public TipoContaBancaria Tipo { get; set; }

        /// <summary>
        /// Valor monetário disponível na conta. Deve ser atualizado a cada lançamento.
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>
        /// Coleção de entradas e saídas (transações) vinculadas a esta conta bancária.
        /// </summary>
        public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();

        /// <summary>
        /// Transferências onde esta conta é a conta de origem.
        /// </summary>
        public ICollection<Transferencia> TransferenciasOrigem { get; set; } = new List<Transferencia>();

        /// <summary>
        /// Transferências onde esta conta é a conta de destino.
        /// </summary>
        public ICollection<Transferencia> TransferenciasDestino { get; set; } = new List<Transferencia>();
    }
}
