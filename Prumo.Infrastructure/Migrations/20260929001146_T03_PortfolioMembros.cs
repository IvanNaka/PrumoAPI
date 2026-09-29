using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T03_PortfolioMembros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Portfolios",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Criado");

            // Status inicial dos portfólios existentes (Figura 27): com projetos -> EmAnalise;
            // só com critérios -> Configurado; senão Criado.
            migrationBuilder.Sql(@"
UPDATE ""Portfolios"" p SET ""Status"" = CASE
    WHEN EXISTS (SELECT 1 FROM ""Projects"" pr WHERE pr.""PortfolioId"" = p.""Id"") THEN 'EmAnalise'
    WHEN EXISTS (SELECT 1 FROM ""PriorityCriteria"" c WHERE c.""PortfolioId"" = p.""Id"") THEN 'Configurado'
    ELSE 'Criado' END;");

            migrationBuilder.CreateTable(
                name: "PortfolioMembers",
                columns: table => new
                {
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioMembers", x => new { x.PortfolioId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PortfolioMembers_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PortfolioMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioMembers_UserId",
                table: "PortfolioMembers",
                column: "UserId");

            // Membros iniciais: o responsável e quem já tinha acesso pelas equipes do portfólio
            // (antes o acesso era controlado pelos times).
            migrationBuilder.Sql(@"
INSERT INTO ""PortfolioMembers"" (""PortfolioId"", ""UserId"", ""CreatedDate"")
SELECT ""Id"", ""OwnerId"", now() FROM ""Portfolios""
UNION
SELECT t.""PortfolioId"", t.""OwnerUserId"", now() FROM ""Teams"" t WHERE t.""OwnerUserId"" IS NOT NULL
UNION
SELECT t.""PortfolioId"", tu.""UserId"", now() FROM ""TeamUsers"" tu JOIN ""Teams"" t ON t.""Id"" = tu.""TeamId""
ON CONFLICT DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioMembers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Portfolios");
        }
    }
}
