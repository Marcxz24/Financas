using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoverDiaFechamentoCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove a coluna dia_fechamento da tabela cartao_credito
            // Esta coluna era utilizada para definir automaticamente o fechamento da fatura,
            // funcionalidade que foi substituída por um fluxo manual gerenciado pela entidade Fatura.
            migrationBuilder.DropColumn(
                name: "dia_fechamento",
                table: "cartao_credito");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaura a coluna dia_fechamento em caso de rollback (reversão da migration)
            migrationBuilder.AddColumn<int>(
                name: "dia_fechamento",
                table: "cartao_credito",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
