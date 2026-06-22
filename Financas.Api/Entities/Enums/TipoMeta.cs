namespace Financas.Api.Entities.Enums
{
    /// <summary>
    /// Define o tipo de meta financeira utilizada no sistema.
    /// Isso determina se a meta será baseada em despesas ou receitas,
    /// influenciando diretamente na forma como os cálculos são feitos.
    /// </summary>
    public enum TipoMeta
    {
        // Meta baseada em gastos (controle de despesas)
        Despesa = 0,

        // Meta baseada em entradas financeiras (controle de receitas)
        Receita = 1
    }
}