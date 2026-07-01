using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlterarDataLancamentoParaTimestampSemTimezone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "data_lancamento",
                table: "lancamentos",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "data_lancamento",
                table: "lancamentos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
