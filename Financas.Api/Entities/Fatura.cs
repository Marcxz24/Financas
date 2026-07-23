using Financas.Api.Entities.Enums;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Representa um ciclo de cobrança de um cartão de crédito.
    /// Agrupa os lançamentos realizados enquanto a fatura estiver aberta.
    /// O fechamento do ciclo é sempre manual, realizado pelo usuário.
    /// </summary>
    public class Fatura
    {
        /// <summary>
        /// Identificador único da fatura no banco de dados.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do cartão de crédito ao qual esta fatura pertence.
        /// </summary>
        public int CartaoCreditoId { get; set; }

        /// <summary>
        /// Propriedade de navegação para acessar os dados do cartão de crédito vinculado.
        /// </summary>
        public virtual CartaoCredito CartaoCredito { get; set; } = null!;

        /// <summary>
        /// Identificador histórico do ciclo, armazenado como o primeiro dia do mês de
        /// referência (ex: 01/07/2026 representa a fatura "Julho/2026").
        /// Não é utilizado para abertura ou fechamento automático de faturas.
        /// </summary>
        public DateTime Competencia { get; set; }

        /// <summary>
        /// Instante em que o ciclo de cobrança começou.
        /// Preenchido automaticamente na criação da fatura e nunca alterado posteriormente.
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Instante em que o usuário fechou manualmente a fatura.
        /// Permanece nulo enquanto a fatura estiver <see cref="FaturaStatus.Aberta"/>.
        /// </summary>
        public DateTime? DataFechamento { get; set; }

        /// <summary>
        /// Data limite para o pagamento da fatura.
        /// Calculada na criação com base no <see cref="CartaoCredito.DiaVencimento"/>
        /// e congelada após o encerramento do ciclo.
        /// </summary>
        public DateTime DataVencimento { get; set; }

        /// <summary>
        /// Soma de todos os lançamentos vinculados a esta fatura.
        /// </summary>
        public decimal ValorTotal { get; set; }

        /// <summary>
        /// Valor efetivamente pago pelo usuário para esta fatura. 
        /// Permite o controle de pagamentos parciais ou totais.
        /// </summary>
        public decimal ValorPago { get; set; } = 0;

        /// <summary>
        /// Define a situação atual da fatura (ex: Aberta, Fechada ou Paga).
        /// Utilizado para controlar o fluxo de vencimento, liberação de limite e
        /// permissão de novos lançamentos.
        /// </summary>
        public FaturaStatus Status { get; set; }

        /// <summary>
        /// Coleção de lançamentos (compras/despesas) vinculados a este ciclo de fatura.
        /// Permite o rastreio detalhado de cada item que compõe o ValorTotal.
        /// </summary>
        public virtual ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();

        /// <summary>
        /// Histórico de pagamentos realizados para esta fatura.
        /// Permite registrar múltiplos pagamentos parciais até a quitação total do ciclo.
        /// </summary>
        public virtual ICollection<PagamentoFatura> Pagamentos { get; set; } = new List<PagamentoFatura>();
    }
}
