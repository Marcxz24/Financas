using Financas.Api.Entities;
using Financas.Api.Entities.Enums;

public class MetasGasto
{
    /// <summary>
    /// Identificador único da meta de gasto.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificador do usuário dono da meta.
    /// </summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Nome descritivo da meta, usado para identificação pelo usuário.
    /// </summary>
    public string Nome { get; set; } = null!;

    /// <summary>
    /// Categoria opcional associada à meta, usada para filtrar os lançamentos.
    /// </summary>
    public int? CategoriaId { get; set; }

    /// <summary>
    /// Cartão de crédito opcional associado à meta, caso o controle seja por cartão.
    /// </summary>
    public int? CartaoCreditoId { get; set; }

    /// <summary>
    /// Valor financeiro definido como objetivo da meta.
    /// </summary>
    public decimal ValorMeta { get; set; }

    /// <summary>
    /// Define se a meta é baseada em despesas ou receitas.
    /// </summary>
    public TipoMeta TipoMeta { get; set; } = TipoMeta.Despesa;

    /// <summary>
    /// Data de início do período em que a meta começa a ser considerada.
    /// </summary>
    public DateTime DataInicio { get; set; }

    /// <summary>
    /// Data final do período de validade da meta.
    /// </summary>
    public DateTime DataFinal { get; set; }

    /// <summary>
    /// Data de criação da meta no sistema, usada para auditoria e rastreio.
    /// </summary>
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navegação para o usuário proprietário da meta.
    /// </summary>
    public virtual Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Navegação opcional para a categoria vinculada à meta.
    /// </summary>
    public virtual Categoria? Categoria { get; set; }

    /// <summary>
    /// Navegação opcional para o cartão de crédito vinculado à meta.
    /// </summary>
    public virtual CartaoCredito? CartaoCredito { get; set; }
}