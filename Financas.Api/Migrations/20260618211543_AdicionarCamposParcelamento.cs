using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposParcelamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lancamento_pai_id",
                table: "lancamentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "numero_parcela",
                table: "lancamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_parcelas",
                table: "lancamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_lancamento_pai_id",
                table: "lancamentos",
                column: "lancamento_pai_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lancamentos_lancamentos_lancamento_pai_id",
                table: "lancamentos",
                column: "lancamento_pai_id",
                principalTable: "lancamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamentos_lancamentos_lancamento_pai_id",
                table: "lancamentos");

            migrationBuilder.DropIndex(
                name: "IX_lancamentos_lancamento_pai_id",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "lancamento_pai_id",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "numero_parcela",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "total_parcelas",
                table: "lancamentos");
        }
    }
}
