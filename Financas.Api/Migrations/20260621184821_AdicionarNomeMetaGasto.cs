using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNomeMetaGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nome",
                table: "metas_gasto",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nome",
                table: "metas_gasto");
        }
    }
}
