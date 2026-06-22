using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarTipoMeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tipo_meta",
                table: "metas_gasto",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo_meta",
                table: "metas_gasto");
        }
    }
}
