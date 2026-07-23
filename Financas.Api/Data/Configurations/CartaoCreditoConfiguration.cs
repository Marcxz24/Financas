using Financas.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financas.Api.Data.Configurations
{
    /// <summary>
    /// Configuração de mapeamento da entidade Cartão de Crédito para o banco de dados (Fluent API).
    /// Define regras de nomenclatura, tipos de dados, índices e relacionamentos.
    /// </summary>
    public class CartaoCreditoConfiguration : IEntityTypeConfiguration<CartaoCredito>
    {
        /// <summary>
        /// Configuração de mapeamento da entidade Cartão de Crédito para o banco de dados (Fluent API).
        /// Define regras de nomenclatura, tipos de dados, índices e relacionamentos.
        /// </summary>
        public void Configure(EntityTypeBuilder<CartaoCredito> builder)
        {
            // 1. Tabela: Define o nome físico da tabela no banco de dados.
            builder.ToTable("cartao_credito");

            // 2. Chave Primária: Define o campo Id como o identificador único da tabela.
            builder.HasKey(key => key.Id);

            // 3. Chave Estrangeira (Propriedade): Mapeia o nome da coluna e obriga a presença de um usuário.
            builder.Property(c => c.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            // 4. Nome do Cartão: Define limite de caracteres (100) para evitar desperdício de espaço no MySQL.
            builder.Property(c => c.Nome)
                .HasColumnName("nome_cartao_credito")
                .IsRequired()
                .HasMaxLength(100);

            // 5. Limite do Cartão: Define o tipo decimal(18,2) para suportar valores grandes com 2 casas decimais.
            builder.Property(c => c.Limite)
                .HasColumnName("limite_cartao_credito")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // 6. Dia de Vencimento: Define o dia de vencimento do cartão.
            builder.Property(c => c.DiaVencimento)
                .HasColumnName("dia_vencimento")
                .IsRequired();

            // 7. Status do Cartão: Converte o Enum StatusCartaoCredito para inteiro ao salvar no banco.
            builder.Property(c => c.Status)
                .HasColumnName("status_cartao_credito")
                .HasConversion<int>()
                .IsRequired();

            // Índices para consultas frequentes.
            builder.HasIndex(c => c.UsuarioId);

            // 8. Relacionamento com Usuário: Vincula o cartão de crédito ao usuário dono.
            // O uso do Restrict garante que você não delete um usuário que ainda possua 
            // cartões de crédito, preservando a rastreabilidade financeira.
            builder.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
