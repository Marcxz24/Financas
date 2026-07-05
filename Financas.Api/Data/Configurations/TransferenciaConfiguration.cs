using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financas.Api.Data.Configurations
{
    /// <summary>
    /// Configuração da entidade Transferencia.
    /// Responsável por mapear a estrutura da tabela no banco de dados.
    /// </summary>
    public class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
    {
        public void Configure(EntityTypeBuilder<Transferencia> builder)
        {
            // Nome da tabela
            builder.ToTable("transferencias");

            // ===============================
            // Chave Primária
            // ===============================

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // ===============================
            // Propriedades
            // ===============================

            builder.Property(t => t.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            builder.Property(t => t.ContaOrigemId)
                .HasColumnName("conta_origem_id")
                .IsRequired();

            builder.Property(t => t.ContaDestinoId)
                .HasColumnName("conta_destino_id")
                .IsRequired();

            builder.Property(t => t.Data)
                .HasColumnName("data_transferencia")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            builder.Property(t => t.Valor)
                .HasColumnName("valor")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(t => t.Observacao)
                .HasColumnName("observacao")
                .HasMaxLength(300)
                .IsRequired(false);

            // ===============================
            // Relacionamentos
            // ===============================

            builder.HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ContaOrigem)
                .WithMany(c => c.TransferenciasOrigem)
                .HasForeignKey(t => t.ContaOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ContaDestino)
                .WithMany(c => c.TransferenciasDestino)
                .HasForeignKey(t => t.ContaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}