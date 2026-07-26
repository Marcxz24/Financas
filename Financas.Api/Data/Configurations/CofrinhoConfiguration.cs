using Financas.Api.Entities;
using Financas.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financas.Api.Data.Configurations
{
    public class CofrinhoConfiguration : IEntityTypeConfiguration<Cofrinho>
    {
        /// <summary>
        /// Configuração de mapeamento da entidade Cofrinho para o banco de dados (Fluent API).
        /// Define regras de nomenclatura, tipos de dados, índices e relacionamentos.
        /// </summary>
        public void Configure(EntityTypeBuilder<Cofrinho> builder)
        {
            // 1. Tabela: Define o nome físico da tabela no banco de dados.
            builder.ToTable("cofrinhos");

            // 2. Chave Primária: Define o campo Id como identificador único da tabela.
            builder.HasKey(c => c.Id);

            // 3. Identificador: Define o nome da coluna da chave primária.
            builder.Property(c => c.Id)
                .HasColumnName("id");

            // 4. Chave Estrangeira (Usuário): Todo cofrinho pertence obrigatoriamente a um usuário.
            builder.Property(c => c.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            // 5. Nome do Cofrinho: Limita a 100 caracteres para otimizar armazenamento
            // e impedir nomes excessivamente grandes.
            builder.Property(c => c.Nome)
                .HasColumnName("nome")
                .IsRequired()
                .HasMaxLength(100);

            // 6. Saldo: Armazena o saldo atual do cofrinho utilizando decimal(18,2),
            // garantindo precisão para valores monetários.
            builder.Property(c => c.Saldo)
                .HasColumnName("saldo")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // 7. Data de Criação: Registra o momento em que o cofrinho foi criado.
            // Utiliza timestamp sem fuso horário, seguindo o padrão adotado no projeto.
            builder.Property(c => c.DataCriacao)
                .HasColumnName("data_criacao")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            // 8. Status do Cofrinho: Converte o Enum StatusCofrinho para inteiro
            // ao persistir no banco de dados.
            builder.Property(c => c.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            // 9. Índice: Otimiza consultas realizadas por usuário.
            builder.HasIndex(c => c.UsuarioId)
                .HasDatabaseName("ix_cofrinhos_usuario_id");

            // 10. Índice Único: Garante que um mesmo usuário não possa possuir
            // dois cofrinhos com o mesmo nome, preservando a consistência dos dados.
            builder.HasIndex(c => new { c.UsuarioId, c.Nome })
                .IsUnique()
                .HasDatabaseName("ix_cofrinhos_usuario_nome");

            // 11. Relacionamento com Usuário: Vincula o cofrinho ao seu proprietário.
            // O uso do Restrict impede a exclusão de um usuário que ainda possua
            // cofrinhos cadastrados, preservando a integridade e rastreabilidade financeira.
            builder.HasOne(c => c.Usuario)
                .WithMany(u => u.Cofrinhos)
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
