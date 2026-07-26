using Financas.Api.Entities.Enums;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Classe que representa a entidade de Lançamento Financeiro.
    /// Utilizada para registrar tanto Despesas quanto Receitas no sistema.
    /// </summary>
    public class Lancamento
    {
        /// <summary>
        /// Identificador único do lançamento (Chave Primária).
        /// Geralmente configurado como Auto-Incremento no banco de dados.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Descrição ou título da movimentação (ex: "Aluguel", "Salário", "Mercado").
        /// </summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Valor monetário da transação. 
        /// O tipo 'decimal' é o mais indicado para evitar erros de precisão em cálculos financeiros.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Data e hora em que a transação ocorreu ou foi registrada.
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Define a categoria do lançamento. 
        /// Comumente utilizado para diferenciar "Receita" de "Despesa".
        /// </summary>
        public TipoLancamento Tipo { get; set; }

        /// <summary>
        /// Chave Estrangeira (FK) que vincula este lançamento a um usuário específico.
        /// </summary>
        public int UsuarioId { get; set; }

        /// <summary>
        /// Chave estrangeira (FK) que armazena o identificador da categoria vinculada.
        /// O uso do tipo 'int?' (nullable) permite que um lançamento exista temporariamente 
        /// sem uma categoria definida (ex: lançamentos pendentes de classificação).
        /// </summary>
        public int? CategoriaId { get; set; }

        /// <summary>
        /// Identificador da conta bancária à qual este lançamento pertence (Chave Estrangeira).
        /// É anulável para permitir lançamentos que ainda não foram vinculados a uma conta específica.
        /// </summary>
        public int? ContaBancariaId { get; set; }

        /// <summary>
        /// Identificador do cofrinho vinculado a este lançamento financeiro.
        /// </summary>
        public int? CofrinhoId { get; set; }

        /// <summary>
        /// Identificador do cartão de crédito utilizado. 
        /// Se preenchido, indica que o lançamento é uma despesa de crédito e não um débito em conta.
        /// </summary>
        public int? CartaoCreditoId { get; set; }

        /// <summary>
        /// Identificador da fatura à qual este lançamento foi vinculado.
        /// Essencial para agrupar compras por ciclo mensal de pagamento.
        /// </summary>
        public int? FaturaId { get; set; }

        /// <summary>
        /// Indica o número da parcela atual em um lançamento parcelado.
        /// </summary>
        public int NumeroParcela { get; set; } = 1;

        /// <summary>
        /// Indica o total de parcelas em que o lançamento foi dividido.
        /// </summary>
        public int TotalParcelas { get; set; } = 1;

        /// <summary>
        /// Refere-se ao lançamento original em casos de lançamentos parcelados.
        /// </summary>
        public int? LancamentoPaiId { get; set; }

        /// <summary>
        /// Propriedade de navegação virtual para acessar os detalhes da fatura vinculada.
        /// Permite que o Entity Framework carregue os dados da fatura (ex: data de vencimento) junto ao lançamento.
        /// </summary>
        public virtual Fatura? Fatura { get; set; }

        /// <summary>
        /// Propriedade de navegação para acessar os detalhes do lançamento pai, caso este seja uma parcela de um lançamento maior.
        /// Permite que o sistema mantenha a hierarquia entre lançamentos parcelados e seu lançamento original, facilitando consultas e relatórios.
        /// </sumary>
        public Lancamento? LancamentoPai { get; set; }

        /// <summary>
        /// Propriedade de navegação que permite acessar os dados completos da categoria associada.
        /// É marcada como anulável para evitar avisos de compilador (null safety) e indicar 
        /// que o carregamento depende de um JOIN explícito (.Include) na consulta.
        /// </summary>
        public Categoria? Categoria { get; set; }

        /// <summary>
        /// Propriedade de Navegação. 
        /// Permite que o Entity Framework carregue os dados do objeto Usuario associado.
        /// </summary>
        public Usuario Usuario { get; set; } = null!;

        /// <summary>
        /// Propriedade de navegação para acessar os detalhes da conta bancária vinculada ao lançamento.
        /// O uso de 'null!' indica que o EF Core gerencia a instância desta entidade.
        /// </summary>
        public ContaBancaria ContaBancaria { get; set; } = null!;

        /// <summary>
        /// Propriedade de navegação para acessar os detalhes do cartão de crédito associado.
        /// Sendo anulável, permite que o sistema identifique lançamentos feitos via dinheiro ou débito direto.
        /// </summary>
        public CartaoCredito? CartaoCredito { get; set; }

        /// <summary>
        /// Propriedade de navegação para acessar os detalhes do cofrinho associado.
        /// </summary>
        public Cofrinho? Cofrinho { get; set; }
    }
}