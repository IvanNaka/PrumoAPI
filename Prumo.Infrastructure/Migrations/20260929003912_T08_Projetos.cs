using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T08_Projetos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""Projects"" SET ""Name"" = left(""Name"", 150);");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedBudget",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Projects",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "JiraProjectKey",
                table: "Projects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Projects",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "StrategicCategory",
                table: "Projects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Run");

            // Projetos existentes: datas e orçamento vêm do orçamento (Budget) quando houver; sem ele,
            // início = data de criação e término = início + 6 meses; orçamento 0; categoria Run.
            // Projetos concluídos recebem DataConclusao = última atualização.
            migrationBuilder.Sql(@"
UPDATE ""Projects"" p SET
    ""StartDate"" = COALESCE(b.""StartDate""::date, p.""CreatedDate""::date),
    ""EndDate"" = GREATEST(COALESCE(b.""EndDate""::date, (p.""CreatedDate"" + interval '6 months')::date),
                           COALESCE(b.""StartDate""::date, p.""CreatedDate""::date)),
    ""ApprovedBudget"" = COALESCE(b.""TotalAmount"", 0)
FROM ""Projects"" p2 LEFT JOIN ""Budgets"" b ON b.""ProjectId"" = p2.""Id""
WHERE p2.""Id"" = p.""Id"";

UPDATE ""Projects"" SET ""CompletedAt"" = COALESCE(""UpdatedDate"", ""CreatedDate"")
WHERE ""Status"" = 'Concluido' AND ""CompletedAt"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedBudget",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "JiraProjectKey",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StrategicCategory",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
