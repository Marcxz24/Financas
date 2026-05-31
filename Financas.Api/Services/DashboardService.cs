using Financas.Api.Data;
using Financas.Api.DTOs.Dashboard;
using Financas.Api.DTOs.Lancamento;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly FinancasDbContext _financasDbContext;

        // Injeção de dependência do contexto do banco de dados
        public DashboardService(FinancasDbContext financasDbContext)
        {
            _financasDbContext = financasDbContext;
        }

        public async Task<DashboardResumoResponseDto> GetResumoMensalAsync(int mes, int ano, int usuarioId, int? contaBancariaId = null)
        {
            // Filtra os lançamentos base por usuário e período (mês/ano) para reutilização na lógica abaixo
            var lancamento = _financasDbContext.Lancamentos
                .Where(l => l.UsuarioId == usuarioId &&
                    l.Data.Month == mes &&
                    l.Data.Year == ano);

            // Soma assíncrona de todos os lançamentos do tipo Receita no período
            var totalReceitas = await lancamento
                .Where(l => l.Tipo == TipoLancamento.Receita)
                .SumAsync(l => l.Valor);

            // Soma assíncrona de todos os lançamentos do tipo Despesa no período
            var totalDespesas = await lancamento
                .Where(l => l.Tipo == TipoLancamento.Despesa)
                .SumAsync(l => l.Valor);

            // Saldo bancário (regra explícita e previsível)
            decimal saldoBancarioTotal;

            // Se um ID de conta bancária for fornecido, calcula o saldo apenas para essa conta; caso contrário, soma o saldo de todas as contas do usuário
            if (contaBancariaId.HasValue && contaBancariaId.Value > 0)
            {
                // Uma única conta
                saldoBancarioTotal = await _financasDbContext.ContasBancarias
                    .Where(c => c.Id == contaBancariaId.Value &&
                                c.UsuarioId == usuarioId)
                    .Select(c => c.Saldo)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // Todas as contas do usuário
                saldoBancarioTotal = await _financasDbContext.ContasBancarias
                    .Where(c => c.UsuarioId == usuarioId)
                    .SumAsync(c => c.Saldo);
            }

            // Busca os 5 lançamentos mais recentes, projetando para o DTO de resumo
            var ultimosLancamentos = await lancamento
                .OrderByDescending(l => l.Data)
                .Take(5)
                .Select(l => new LancamentoResumoDTO
                {
                    Id = l.Id,
                    Descricao = l.Descricao,
                    Valor = l.Valor,
                    DataLancamento = l.Data,
                    Tipo = l.Tipo.ToString(), // Converte o Enum para String para o Front-end
                    ContaBancariaNome = l.ContaBancaria != null ? l.ContaBancaria.Nome : null, // Evita NullReferenceException
                    CartaoCreditoNome = l.CartaoCredito != null ? l.CartaoCredito.Nome : null // Evita NullReferenceException
                })
                .ToListAsync();

            // Retorna o objeto consolidado para o Controller enviar ao React
            return new DashboardResumoResponseDto
            {
                TotalReceitas = totalReceitas,
                TotalDespesas = totalDespesas,
                SaldoMensal = totalReceitas - totalDespesas,
                SaldoBancarioTotal = saldoBancarioTotal,
                PeriodoReferencia = $"{mes:D2}/{ano}", // Formata mês com dois dígitos (ex: 01, 02)
                UltimosLancamentos = ultimosLancamentos
            };
        }
    }
}