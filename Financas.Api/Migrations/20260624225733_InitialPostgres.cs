using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email_pendente = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password = table.Column<string>(type: "text", nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email_confirmado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    token_confirmacao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    token_expiracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cartao_credito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nome_cartao_credito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    limite_cartao_credito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    dia_fechamento = table.Column<int>(type: "integer", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    status_cartao_credito = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartao_credito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cartao_credito_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categorias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contas_bancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nome_conta_bancaria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_conta_bancaria = table.Column<int>(type: "integer", nullable: false),
                    saldo_conta_bancaria = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_bancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contas_bancarias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cartao_credito_id = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fechamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_pago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status_fatura = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_faturas_cartao_credito_cartao_credito_id",
                        column: x => x.cartao_credito_id,
                        principalTable: "cartao_credito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metas_gasto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: true),
                    cartao_credito_id = table.Column<int>(type: "integer", nullable: true),
                    valor_meta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    conta_bancaria_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_meta = table.Column<int>(type: "int", nullable: false),
                    data_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    data_final = table.Column<DateTime>(type: "date", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        name: "FK_metas_gasto_contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metas_gasto_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    data_lancamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: true),
                    conta_bancaria_id = table.Column<int>(type: "integer", nullable: true),
                    cartao_credito_id = table.Column<int>(type: "integer", nullable: true),
                    fatura_id = table.Column<int>(type: "integer", nullable: true),
                    numero_parcela = table.Column<int>(type: "integer", nullable: false),
                    total_parcelas = table.Column<int>(type: "integer", nullable: false),
                    lancamento_pai_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lancamentos_cartao_credito_cartao_credito_id",
                        column: x => x.cartao_credito_id,
                        principalTable: "cartao_credito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamentos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamentos_contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamentos_faturas_fatura_id",
                        column: x => x.fatura_id,
                        principalTable: "faturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamentos_lancamentos_lancamento_pai_id",
                        column: x => x.lancamento_pai_id,
                        principalTable: "lancamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamentos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos_fatura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fatura_id = table.Column<int>(type: "integer", nullable: false),
                    valor_pago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_pagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    conta_bancaria_id = table.Column<int>(type: "integer", nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamentos_fatura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagamentos_fatura_contas_bancarias_conta_bancaria_id",
                        column: x => x.conta_bancaria_id,
                        principalTable: "contas_bancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagamentos_fatura_faturas_fatura_id",
                        column: x => x.fatura_id,
                        principalTable: "faturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cartao_credito_usuario_id",
                table: "cartao_credito",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_usuario_id",
                table: "categorias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_contas_bancarias_usuario_id",
                table: "contas_bancarias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_cartao_credito_id",
                table: "faturas",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_cartao_credito_id_data_inicio_data_fechamento",
                table: "faturas",
                columns: new[] { "cartao_credito_id", "data_inicio", "data_fechamento" });

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_cartao_credito_id",
                table: "lancamentos",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_categoria_id",
                table: "lancamentos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_conta_bancaria_id",
                table: "lancamentos",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_fatura_id",
                table: "lancamentos",
                column: "fatura_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_lancamento_pai_id",
                table: "lancamentos",
                column: "lancamento_pai_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_usuario_id",
                table: "lancamentos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_cartao_credito_id",
                table: "metas_gasto",
                column: "cartao_credito_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_categoria_id",
                table: "metas_gasto",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_conta_bancaria_id",
                table: "metas_gasto",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_metas_gasto_usuario_id",
                table: "metas_gasto",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_fatura_conta_bancaria_id",
                table: "pagamentos_fatura",
                column: "conta_bancaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_fatura_fatura_id",
                table: "pagamentos_fatura",
                column: "fatura_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamentos");

            migrationBuilder.DropTable(
                name: "metas_gasto");

            migrationBuilder.DropTable(
                name: "pagamentos_fatura");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "contas_bancarias");

            migrationBuilder.DropTable(
                name: "faturas");

            migrationBuilder.DropTable(
                name: "cartao_credito");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
