using Financas.Api.DTOs.Lancamento;

namespace Financas.Api.DTOs.Dashboard
{
    /// <summary>
    /// DTO de resposta para o Dashboard.
    /// Consolida os dados financeiros que serão exibidos nos cards e na lista principal.
    /// </summary>
    public class DashboardResumoResponseDto
    {
        // Id do último lançamento registrado, usado para navegação rápida no front-end
        public int Id { get; set; }

        // Soma total de entradas do mês atual
        public decimal TotalReceitas { get; set; }

        // Soma total de saídas do mês atual
        public decimal TotalDespesas { get; set; }

        // Resultado da conta: TotalReceitas - TotalDespesas
        public decimal SaldoMensal { get; set; }

        // Patrimônio total somando todas as contas bancárias do usuário
        public decimal SaldoBancarioTotal { get; set; }

        // String formatada (ex: "05/2026") para exibição no front-end
        public string PeriodoReferencia { get; set; } = string.Empty;

        // Lista reduzida com os últimos 5 movimentos financeiros
        public List<LancamentoResumoDTO> LancamentosDoMes { get; set; } = new();
    }
}