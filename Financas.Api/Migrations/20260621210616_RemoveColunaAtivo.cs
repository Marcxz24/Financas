using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColunaAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ativo",
                table: "metas_gasto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "metas_gasto",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }
    }
}
