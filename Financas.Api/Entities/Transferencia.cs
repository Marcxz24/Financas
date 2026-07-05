namespace Financas.Api.Entities
{
    /// <summary>
    /// Classe que representa uma transferência entre duas contas bancárias.
    /// Diferente de um lançamento financeiro, a transferência apenas movimenta
    /// saldo entre contas pertencentes ao usuário, não representando receita
    /// nem despesa.
    /// </summary>
    public class Transferencia
    {
        /// <summary>
        /// Identificador único da transferência (Chave Primária).
        /// Geralmente configurado como Auto-Incremento no banco de dados.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Valor monetário transferido entre as contas.
        /// O tipo decimal evita perdas de precisão em operações financeiras.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Data e hora em que a transferência foi realizada.
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Observação opcional informada pelo usuário.
        /// Exemplo: "Reserva de emergência", "Transferência para carteira".
        /// </summary>
        public string? Observacao { get; set; }

        /// <summary>
        /// Chave Estrangeira (FK) do usuário proprietário da transferência.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Chave Estrangeira (FK) da conta de origem.
        /// Será desta conta que o saldo será debitado.
        /// </summary>
        public int ContaOrigemId { get; set; }

        /// <summary>
        /// Chave Estrangeira (FK) da conta de destino.
        /// Será nesta conta que o saldo será creditado.
        /// </summary>
        public int ContaDestinoId { get; set; }

        /// <summary>
        /// Propriedade de navegação para acessar os dados completos
        /// do usuário proprietário da transferência.
        /// </summary>
        public Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Propriedade de navegação para acessar os dados completos
        /// da conta bancária de origem.
        /// </summary>
        public ContaBancaria ContaOrigem { get; set; } = null!;

        /// <summary>
        /// Propriedade de navegação para acessar os dados completos
        /// da conta bancária de destino.
        /// </summary>
        public ContaBancaria ContaDestino { get; set; } = null!;
    }
}