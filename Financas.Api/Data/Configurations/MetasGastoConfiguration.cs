using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financas.Api.Data.Configurations
{
    /// <summary>
    /// Configuração de mapeamento da entidade MetasGasto no Entity Framework Core.
    /// Define tabela, colunas, relacionamentos, restrições e índices utilizados no banco de dados.
    /// </summary>
    public class MetaGastoConfiguration : IEntityTypeConfiguration<MetasGasto>
    {
        public void Configure(EntityTypeBuilder<MetasGasto> builder)
        {
            // Define o nome da tabela no banco de dados
            builder.ToTable("metas_gasto");

            // Chave primária da entidade
            builder.HasKey(m => m.Id);

            // Mapeamento do usuário responsável pela meta
            builder.Property(m => m.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            // Nome da meta com limite de tamanho para evitar strings excessivamente grandes
            builder.Property(m => m.Nome)
                .HasColumnName("nome")
                .HasMaxLength(150)
                .IsRequired();

            // Categoria associada à meta (campo opcional)
            builder.Property(m => m.CategoriaId)
                .HasColumnName("categoria_id")
                .IsRequired(false);

            // Cartão de crédito associado à meta (campo opcional)
            builder.Property(m => m.CartaoCreditoId)
                .HasColumnName("cartao_credito_id")
                .IsRequired(false);

            // Valor monetário da meta com precisão definida para evitar erros de arredondamento
            builder.Property(m => m.ValorMeta)
                .HasColumnName("valor_meta")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Tipo da meta (Despesa ou Receita), armazenado como inteiro no banco
            builder.Property(m => m.TipoMeta)
                .HasColumnName("tipo_meta")
                .HasColumnType("int")
                .IsRequired();

            // Data inicial do período de vigência da meta
            builder.Property(m => m.DataInicio)
                .HasColumnName("data_inicio")
                .HasColumnType("date")
                .IsRequired();

            // Data final do período de vigência da meta
            builder.Property(m => m.DataFinal)
                .HasColumnName("data_final")
                .HasColumnType("date")
                .IsRequired();

            // Data de criação do registro no sistema
            builder.Property(m => m.DataCriacao)
                .HasColumnName("data_criacao")
                .IsRequired();

            // Relacionamento: cada meta pertence a um usuário
            builder.HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento opcional com categoria (não remove meta ao excluir categoria)
            builder.HasOne(m => m.Categoria)
                .WithMany()
                .HasForeignKey(m => m.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamento opcional com cartão de crédito
            builder.HasOne(m => m.CartaoCredito)
                .WithMany()
                .HasForeignKey(m => m.CartaoCreditoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para otimizar consultas filtradas por usuário
            builder.HasIndex(m => m.UsuarioId);
        }
    }
}