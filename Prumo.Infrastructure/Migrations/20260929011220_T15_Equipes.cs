using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T15_Equipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Teams_Portfolios_PortfolioId", table: "Teams");
            migrationBuilder.DropForeignKey(name: "FK_Teams_Users_OwnerUserId", table: "Teams");
            migrationBuilder.DropForeignKey(name: "FK_TeamUsers_Teams_TeamId", table: "TeamUsers");
            migrationBuilder.DropForeignKey(name: "FK_TeamUsers_Users_UserId", table: "TeamUsers");
            migrationBuilder.DropPrimaryKey(name: "PK_TeamUsers", table: "TeamUsers");

            // Código de convite (item EXTRA) removido: os membros passam a ser cadastrados pelo TechLead.
            migrationBuilder.DropIndex(name: "IX_Teams_InviteCode", table: "Teams");
            migrationBuilder.DropColumn(name: "InviteCode", table: "Teams");

            // ---------- MembroEquipe ----------
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId", table: "TeamUsers", type: "uuid", nullable: true,
                oldClrType: typeof(Guid), oldType: "uuid");
            migrationBuilder.AddColumn<Guid>(
                name: "Id", table: "TeamUsers", type: "uuid", nullable: false,
                defaultValueSql: "gen_random_uuid()");
            migrationBuilder.AddColumn<string>(
                name: "Email", table: "TeamUsers", type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyCost", table: "TeamUsers", type: "numeric(10,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>(
                name: "MonthlyCapacityHours", table: "TeamUsers", type: "integer", nullable: false, defaultValue: 160);
            migrationBuilder.AddColumn<string>(
                name: "Name", table: "TeamUsers", type: "character varying(150)", maxLength: 150, nullable: false, defaultValue: "");

            // Os membros antigos eram usuários: nome e e-mail vêm do usuário; a capacidade vem do
            // último lançamento manual de capacidade (limitado a 1..300) ou 160 h; custo/hora 0.
            migrationBuilder.Sql(@"
UPDATE ""TeamUsers"" tu SET ""Name"" = left(u.""Name"", 150), ""Email"" = u.""Email""
FROM ""Users"" u WHERE u.""Id"" = tu.""UserId"";

UPDATE ""TeamUsers"" tu SET ""MonthlyCapacityHours"" = LEAST(300, GREATEST(1, round(c.""AvailableHours"")::int))
FROM (SELECT DISTINCT ON (""TeamId"", ""UserId"") ""TeamId"", ""UserId"", ""AvailableHours""
      FROM ""TeamCapacityEntries"" ORDER BY ""TeamId"", ""UserId"", ""Year"" DESC, ""Month"" DESC) c
WHERE c.""TeamId"" = tu.""TeamId"" AND c.""UserId"" = tu.""UserId"";");

            // Capacidade manual (item EXTRA) substituída pelo cálculo F9.
            migrationBuilder.DropTable(name: "TeamCapacityEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id", table: "TeamUsers", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldDefaultValueSql: "gen_random_uuid()");
            migrationBuilder.AddPrimaryKey(name: "PK_TeamUsers", table: "TeamUsers", column: "Id");
            migrationBuilder.CreateIndex(name: "IX_TeamUsers_Email", table: "TeamUsers", column: "Email");
            migrationBuilder.CreateIndex(name: "IX_TeamUsers_TeamId", table: "TeamUsers", column: "TeamId");

            // ---------- Equipe ----------
            migrationBuilder.AlterColumn<Guid>(
                name: "PortfolioId", table: "Teams", type: "uuid", nullable: true,
                oldClrType: typeof(Guid), oldType: "uuid");

            // Nome (100) único: nomes repetidos recebem sufixo.
            migrationBuilder.Sql(@"
UPDATE ""Teams"" SET ""Name"" = left(trim(""Name""), 100);
WITH dup AS (
    SELECT ""Id"", row_number() OVER (PARTITION BY lower(""Name"") ORDER BY ""CreatedDate"", ""Id"") AS n FROM ""Teams"")
UPDATE ""Teams"" t SET ""Name"" = left(t.""Name"", 90) || ' (' || dup.n || ')'
FROM dup WHERE dup.""Id"" = t.""Id"" AND dup.n > 1;");

            migrationBuilder.AlterColumn<string>(
                name: "Name", table: "Teams", type: "character varying(100)", maxLength: 100, nullable: false,
                oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200);
            migrationBuilder.CreateIndex(name: "IX_Teams_Name", table: "Teams", column: "Name", unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Portfolios_PortfolioId", table: "Teams", column: "PortfolioId",
                principalTable: "Portfolios", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_OwnerUserId", table: "Teams", column: "OwnerUserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_TeamUsers_Teams_TeamId", table: "TeamUsers", column: "TeamId",
                principalTable: "Teams", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_TeamUsers_Users_UserId", table: "TeamUsers", column: "UserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Portfolios_PortfolioId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_OwnerUserId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamUsers_Teams_TeamId",
                table: "TeamUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamUsers_Users_UserId",
                table: "TeamUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamUsers",
                table: "TeamUsers");

            migrationBuilder.DropIndex(
                name: "IX_TeamUsers_Email",
                table: "TeamUsers");

            migrationBuilder.DropIndex(
                name: "IX_TeamUsers_TeamId",
                table: "TeamUsers");

            migrationBuilder.DropIndex(
                name: "IX_Teams_Name",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "HourlyCost",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "MonthlyCapacityHours",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TeamUsers");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "TeamUsers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PortfolioId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Teams",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamUsers",
                table: "TeamUsers",
                columns: new[] { "TeamId", "UserId" });

            migrationBuilder.CreateTable(
                name: "TeamCapacityEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    AllocatedHours = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    AvailableHours = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    OccupancyPercent = table.Column<int>(type: "integer", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCapacityEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamCapacityEntries_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamCapacityEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_InviteCode",
                table: "Teams",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamCapacityEntries_TeamId_UserId_Year_Month",
                table: "TeamCapacityEntries",
                columns: new[] { "TeamId", "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamCapacityEntries_UserId",
                table: "TeamCapacityEntries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Portfolios_PortfolioId",
                table: "Teams",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_OwnerUserId",
                table: "Teams",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamUsers_Teams_TeamId",
                table: "TeamUsers",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamUsers_Users_UserId",
                table: "TeamUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
