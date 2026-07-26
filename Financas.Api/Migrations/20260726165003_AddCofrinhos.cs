using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCofrinhos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cofrinho_id",
                table: "lancamentos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cofrinhos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    saldo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cofrinhos", x => x.id);
                    table.ForeignKey(
                        name: "FK_cofrinhos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_cofrinho_id",
                table: "lancamentos",
                column: "cofrinho_id");

            migrationBuilder.CreateIndex(
                name: "ix_cofrinhos_usuario_id",
                table: "cofrinhos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_cofrinhos_usuario_nome",
                table: "cofrinhos",
                columns: new[] { "usuario_id", "nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_lancamentos_cofrinhos_cofrinho_id",
                table: "lancamentos",
                column: "cofrinho_id",
                principalTable: "cofrinhos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lancamentos_cofrinhos_cofrinho_id",
                table: "lancamentos");

            migrationBuilder.DropTable(
                name: "cofrinhos");

            migrationBuilder.DropIndex(
                name: "IX_lancamentos_cofrinho_id",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "cofrinho_id",
                table: "lancamentos");
        }
    }
}
