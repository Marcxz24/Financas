using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContaBancariaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "conta_bancaria_id",
                table: "metas_gasto",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_conta_bancaria_id",
                table: "metas_gasto",
                column: "conta_bancaria_id");

            migrationBuilder.AddForeignKey(
                name: "FK_metas_gasto_contas_bancarias_conta_bancaria_id",
                table: "metas_gasto",
                column: "conta_bancaria_id",
                principalTable: "contas_bancarias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_metas_gasto_contas_bancarias_conta_bancaria_id",
                table: "metas_gasto");

            migrationBuilder.DropIndex(
                name: "IX_metas_gasto_conta_bancaria_id",
                table: "metas_gasto");

            migrationBuilder.DropColumn(
                name: "conta_bancaria_id",
                table: "metas_gasto");
        }
    }
}
