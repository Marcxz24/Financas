using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financas.Api.Data.Configurations
{
    /// <summary>
    /// Configuração de mapeamento da entidade Fatura para o banco de dados (Fluent API).
    /// Define regras de nomenclatura, tipos de dados, índices e relacionamentos.
    /// </summary>
    public class FaturaConfiguration : IEntityTypeConfiguration<Fatura>
    {
        public void Configure(EntityTypeBuilder<Fatura> builder)
        {
            // Define o nome físico da tabela no banco de dados.
            builder.ToTable("faturas");

            // Chave primária.
            builder.HasKey(f => f.Id);

            // Mapeamento de propriedades.
            builder.Property(f => f.CartaoCreditoId)
                .HasColumnName("cartao_credito_id")
                .IsRequired();

            // 1. Competência: Representa o ciclo mensal (ano/mês) da fatura, armazenado como o
            // primeiro dia do mês de referência. É a chave natural e imutável do ciclo, usada
            // para localizar/criar a fatura correta independentemente de alterações futuras na
            // configuração do cartão. Mapeada como "date" pois apenas ano e mês são relevantes.
            builder.Property(f => f.Competencia)
                .HasColumnName("competencia")
                .HasColumnType("date")
                .IsRequired();

            // 2. Data de Início: Data em que o ciclo de compras desta fatura começou.
            // Para faturas Abertas, este valor é recalculado dinamicamente pelo FaturaService
            // a partir da configuração vigente do cartão; para faturas encerradas, representa
            // o histórico congelado e não deve mais ser alterado pela aplicação.
            builder.Property(f => f.DataInicio)
                .HasColumnName("data_inicio")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            // 3. Data de Fechamento: Instante exato (23:59:59.999 do dia configurado no cartão)
            // em que a fatura deixa de aceitar novos lançamentos.
            builder.Property(f => f.DataFechamento)
                .HasColumnName("data_fechamento")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            // 4. Data de Vencimento: Define o dia de vencimento da fatura.
            builder.Property(f => f.DataVencimento)
                .HasColumnName("data_vencimento")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            // 5. Valor Total: Define o valor total da fatura.
            builder.Property(f => f.ValorTotal)
                .HasColumnName("valor_total")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // 6. Valor Pago: Define o valor pago da fatura.
            builder.Property(f => f.ValorPago)
                .HasColumnName("valor_pago")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // 7. Status da Fatura: Converte o Enum FaturaStatus para inteiro ao salvar no banco.
            builder.Property(f => f.Status)
                .HasColumnName("status_fatura")
                .HasConversion<int>()
                .IsRequired();

            // 8. Índices para busca por cartão/ciclo.
            builder.HasIndex(f => f.CartaoCreditoId);

            // Índice único por Cartão + Competência: garante, em nível de banco de dados, que
            // nunca existam duas faturas para o mesmo cartão no mesmo mês de referência,
            // substituindo o antigo índice baseado em DataInicio/DataFechamento (que era frágil
            // por depender de datas calculadas e podia gerar duplicidade em recomputações).
            builder.HasIndex(f => new { f.CartaoCreditoId, f.Competencia })
                .IsUnique()
                .HasDatabaseName("ix_faturas_cartao_competencia");

            // 9. Relacionamento com Cartão de Crédito: Vincula a fatura ao cartão de crédito.
            // O uso do Restrict garante que você não delete um cartão de crédito que ainda possua 
            // faturas, preservando a rastreabilidade financeira.
            builder.HasOne(f => f.CartaoCredito)
                .WithMany(c => c.Faturas)
                .HasForeignKey(f => f.CartaoCreditoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}