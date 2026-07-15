using Financas.Api.Entities.Enums;

namespace Financas.Api.Entities
{
    /// <summary>
    /// Representa o ciclo mensal de gastos de um cartão de crédito.
    /// Agrupa os lançamentos realizados dentro de uma competência (ano/mês) específica.
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
        /// Representa a competência (ciclo mensal) desta fatura, armazenada como o primeiro dia
        /// do mês de referência (ex: 01/07/2026 representa a fatura "Julho/2026").
        /// É a chave estável e imutável que identifica o ciclo, independente de alterações
        /// futuras no dia de fechamento ou vencimento do cartão. Enquanto <see cref="DataInicio"/>,
        /// <see cref="DataFechamento"/> e <see cref="DataVencimento"/> podem ser recalculadas
        /// (para faturas ainda abertas) conforme a configuração vigente do cartão, a
        /// <see cref="Competencia"/> nunca muda depois de criada, garantindo que sempre exista
        /// no máximo uma fatura por cartão/mês.
        /// </summary>
        public DateTime Competencia { get; set; }

        /// <summary>
        /// Data de início do período de faturamento (dia seguinte ao fechamento do ciclo anterior).
        /// Para faturas com status <see cref="FaturaStatus.Aberta"/>, este valor é recalculado
        /// automaticamente a partir da configuração atual do cartão sempre que a fatura é
        /// consultada ou recebe um novo lançamento. Para faturas já encerradas
        /// (<see cref="FaturaStatus.Fechada"/>, <see cref="FaturaStatus.Paga"/> ou
        /// <see cref="FaturaStatus.Atrasada"/>), o valor é congelado e passa a representar o
        /// histórico real e imutável do ciclo.
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data e hora em que a fatura é efetivamente "cortada", impedindo novos lançamentos
        /// para este ciclo. O fechamento ocorre sempre às 23:59:59.999 do dia de fechamento
        /// configurado no cartão (respeitando meses com 28, 29, 30 ou 31 dias). Assim como
        /// <see cref="DataInicio"/>, é recalculada enquanto a fatura estiver Aberta e congelada
        /// após o encerramento do ciclo.
        /// </summary>
        public DateTime DataFechamento { get; set; }

        /// <summary>
        /// Data limite para o pagamento da fatura sem incidência de juros ou multa. Segue a
        /// mesma regra de recálculo (enquanto Aberta) e congelamento (após encerrada) das
        /// demais datas do ciclo.
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
        /// Utilizado para controlar o fluxo de vencimento, liberação de limite e,
        /// principalmente, se as datas do ciclo ainda podem ser recalculadas (Aberta)
        /// ou já estão congeladas como histórico (Fechada/Paga/Atrasada).
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