using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class CompetenciaFatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_faturas_cartao_credito_id_data_inicio_data_fechamento",
                table: "faturas");

            // 1. Adiciona a coluna SEM valor padrão fixo e SEM NOT NULL ainda.
            //    O EF gerou "nullable: false" com defaultValue = 0001-01-01, o que faz
            //    TODAS as faturas já existentes caírem na mesma competência incorreta —
            //    e derruba o índice único logo em seguida caso algum cartão já tenha mais
            //    de uma fatura no banco. Por isso a coluna entra nullable, é preenchida
            //    (backfill) a partir de "data_fechamento", e só então vira NOT NULL.
            migrationBuilder.AddColumn<DateTime>(
                name: "competencia",
                table: "faturas",
                type: "date",
                nullable: true);

            // 2. Backfill: deriva a competência das faturas já existentes a partir do
            //    mês/ano da data de fechamento já gravada, preservando o histórico
            //    real sem qualquer perda de dado.
            migrationBuilder.Sql(@"
                UPDATE faturas
                SET competencia = date_trunc('month', data_fechamento)::date
                WHERE competencia IS NULL;
            ");

            // 3. Só agora a coluna passa a ser obrigatória, já com todos os registros
            //    existentes preenchidos corretamente.
            migrationBuilder.AlterColumn<DateTime>(
                name: "competencia",
                table: "faturas",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            // 4. Índice único (Cartão + Competência) criado por último, depois que os
            //    dados já estão corretos — evita falha por violação de unicidade.
            migrationBuilder.CreateIndex(
                name: "ix_faturas_cartao_competencia",
                table: "faturas",
                columns: new[] { "cartao_credito_id", "competencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_faturas_cartao_competencia",
                table: "faturas");

            migrationBuilder.DropColumn(
                name: "competencia",
                table: "faturas");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_cartao_credito_id_data_inicio_data_fechamento",
                table: "faturas",
                columns: new[] { "cartao_credito_id", "data_inicio", "data_fechamento" });
        }
    }
}