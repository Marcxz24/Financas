namespace Financas.Api.Entities.Enums
{
    /// <summary>
    /// Define as modalidades de contas bancárias disponíveis no sistema financeiro.
    /// </summary>
    public enum TipoContaBancaria
    {
        /// <summary>
        /// Conta tradicional de depósitos à vista, com acesso a talão de cheques, limite de crédito (cheque especial) e movimentação livre.
        /// </summary>
        Corrente = 1,

        /// <summary>
        /// Conta focada em acúmulo de capital com rendimento mensal isento de IR (para pessoa física). Não possui limite de crédito.
        /// </summary>
        Poupanca = 2,

        /// <summary>
        /// Conta aberta de forma 100% online (Fintechs). Comercial e popularmente conhecida como Conta Digital.
        /// </summary>
        Digital = 3,

        /// <summary>
        /// Conta destinada exclusivamente ao recebimento de salários, aposentadorias ou pensões. Não permite depósitos de outras fontes.
        /// </summary>
        Salario = 4,

        /// <summary>
        /// Classificação técnica do Banco Central para contas de instituições de pagamento (ex: carteiras digitais como PicPay, Mercado Pago).
        /// </summary>
        Pagamento = 5,

        /// <summary>
        /// Conta específica mantida em corretoras de valores para a custódia e movimentação de ativos financeiros (ações, FIIs, Tesouro Direto).
        /// </summary>
        Investimento = 6,

        /// <summary>
        /// Modalidade de conta corrente com tarifas reduzidas ou isentas, destinada a estudantes de ensino superior.
        /// </summary>
        Universitaria = 7,

        /// <summary>
        /// Conta mantida em moeda estrangeira (Dólar, Euro), comum em bancos digitais modernos para uso no exterior (ex: Nomad, Inter Global).
        /// </summary>
        Internacional = 8,

        /// <summary>
        /// Representa dinheiro em espécie (cédulas e moedas) mantido fisicamente pelo usuário.
        /// É utilizada para controlar o saldo disponível em carteira e participa normalmente
        /// dos cálculos de patrimônio, receitas, despesas e movimentações financeiras.
        /// </summary>
        Fisica = 9
    }
}
