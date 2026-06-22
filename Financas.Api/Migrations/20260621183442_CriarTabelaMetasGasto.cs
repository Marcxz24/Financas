using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaMetasGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "metas_gasto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    categoria_id = table.Column<int>(type: "int", nullable: true),
                    cartao_credito_id = table.Column<int>(type: "int", nullable: true),
                    valor_meta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    mes = table.Column<int>(type: "int", nullable: false),
                    ano = table.Column<int>(type: "int", nullable: false),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metas_gasto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metas_gasto_cartao_credito_cartao_credito_id",
                        column: x => x.cartao_credito_id,
                        principalTable: "cartao_credito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metas_gasto_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metas_gasto_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_cartao_credito_id",
                table: "metas_gasto",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_categoria_id",
                table: "metas_gasto",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_usuario_id",
                table: "metas_gasto",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_usuario_id_mes_ano",
                table: "metas_gasto",
                columns: new[] { "usuario_id", "mes", "ano" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metas_gasto");
        }
    }
}
