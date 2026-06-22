using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financas.Api.Migrations
{
    /// <inheritdoc />
    public partial class RecriarMetasGasto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS metas_gasto;

                CREATE TABLE metas_gasto (
                    id INT AUTO_INCREMENT PRIMARY KEY,

                    usuario_id INT NOT NULL,
                    nome VARCHAR(150) NOT NULL,

                    categoria_id INT NULL,
                    cartao_credito_id INT NULL,

                    valor_meta DECIMAL(18,2) NOT NULL,

                    data_inicio DATE NOT NULL,
                    data_final DATE NOT NULL,

                    data_criacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

                    INDEX IX_metas_gasto_usuario_id (usuario_id),

                    CONSTRAINT FK_metas_gasto_usuario
                        FOREIGN KEY (usuario_id)
                        REFERENCES usuarios(id)
                        ON DELETE CASCADE,

                    CONSTRAINT FK_metas_gasto_categoria
                        FOREIGN KEY (categoria_id)
                        REFERENCES categorias(id)
                        ON DELETE RESTRICT,

                    CONSTRAINT FK_metas_gasto_cartao
                        FOREIGN KEY (cartao_credito_id)
                        REFERENCES cartao_credito(id)
                        ON DELETE RESTRICT
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS metas_gasto;");
        }
    }
}
