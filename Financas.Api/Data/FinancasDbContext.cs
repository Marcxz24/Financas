using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financas.Api.Data
{
    /// <summary>
    /// Contexto principal do Entity Framework Core responsável pelo acesso ao banco de dados.
    /// Centraliza o mapeamento das entidades e configurações do sistema financeiro.
    /// </summary>
    public class FinancasDbContext : DbContext
    {
        // Configuração base do DbContext (injeção de opções como string de conexão)
        public FinancasDbContext(DbContextOptions<FinancasDbContext> options) : base(options) { }

        // Usuários do sistema
        public DbSet<Usuario> Usuarios { get; set; }

        // Lançamentos financeiros (receitas e despesas)
        public DbSet<Lancamento> Lancamentos { get; set; }

        // Categorias utilizadas para organizar lançamentos
        public DbSet<Categoria> Categorias { get; set; }

        // Contas bancárias cadastradas pelo usuário
        public DbSet<ContaBancaria> ContasBancarias { get; set; }

        // Cartões de crédito cadastrados no sistema
        public DbSet<CartaoCredito> CartaoCredito { get; set; }

        // Faturas geradas a partir dos cartões de crédito
        public DbSet<Fatura> Fatura { get; set; }

        // Pagamentos realizados nas faturas (total ou parcial)
        public DbSet<PagamentoFatura> PagamentoFatura { get; set; }

        // Metas financeiras de gasto ou receita
        public DbSet<MetasGasto> MetasGasto { get; set; }

        // Transferências entre contas bancárias do usuário
        public DbSet<Transferencia> Transferencias { get; set; }

        // Cofrinhos financeiros do usuário
        public DbSet<Cofrinho> Cofrinhos { get; set; }

        // Aplica automaticamente todas as configurações Fluent API do assembly
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancasDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}