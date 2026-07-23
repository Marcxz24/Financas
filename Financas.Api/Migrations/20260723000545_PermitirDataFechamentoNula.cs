using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class PermitirDataFechamentoNula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_faturas_cartao_competencia",
                table: "faturas");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_fechamento",
                table: "faturas",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.CreateIndex(
                name: "ix_faturas_cartao_competencia",
                table: "faturas",
                columns: new[] { "cartao_credito_id", "competencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_faturas_cartao_competencia",
                table: "faturas");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_fechamento",
                table: "faturas",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_faturas_cartao_competencia",
                table: "faturas",
                columns: new[] { "cartao_credito_id", "competencia" },
                unique: true);
        }
    }
}
