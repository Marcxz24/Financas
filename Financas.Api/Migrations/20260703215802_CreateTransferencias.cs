using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateTransferencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transferencias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_transferencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    conta_origem_id = table.Column<int>(type: "integer", nullable: false),
                    conta_destino_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias", x => x.id);
                    table.ForeignKey(
                        name: "FK_transferencias_contas_bancarias_conta_destino_id",
                        column: x => x.conta_destino_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_contas_bancarias_conta_origem_id",
                        column: x => x.conta_origem_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_conta_destino_id",
                table: "transferencias",
                column: "conta_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_conta_origem_id",
                table: "transferencias",
                column: "conta_origem_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_usuario_id",
                table: "transferencias",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transferencias");
        }
    }
}
