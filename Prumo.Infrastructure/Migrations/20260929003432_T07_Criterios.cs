using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T07_Criterios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEvaluation_PriorityCriteria_PriorityCriteriaId",
                table: "ProjectEvaluation");

            migrationBuilder.DropIndex(
                name: "IX_ProjectEvaluation_ProjectId",
                table: "ProjectEvaluation");

            migrationBuilder.DropIndex(
                name: "IX_PriorityCriteria_PortfolioId",
                table: "PriorityCriteria");

            // ---------- AvaliacaoCriterio: nota inteira de 1 a 5 (D06) ----------
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "ProjectEvaluation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluatedAt",
                table: "ProjectEvaluation",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            // As notas antigas eram de 0 a 10 (tela antiga de projetos): conversão linear para 1..5.
            // Mantém só a nota mais recente de cada (projeto, critério).
            migrationBuilder.Sql(@"
UPDATE ""ProjectEvaluation"" SET
    ""Score"" = LEAST(5, GREATEST(1, round(1 + ""Value"" * 4 / 10)::int)),
    ""EvaluatedAt"" = COALESCE(""UpdatedDate"", ""CreatedDate"");

DELETE FROM ""ProjectEvaluation"" e USING ""ProjectEvaluation"" newer
WHERE e.""ProjectId"" = newer.""ProjectId""
  AND e.""PriorityCriteriaId"" = newer.""PriorityCriteriaId""
  AND (e.""EvaluatedAt"", e.""Id"") < (newer.""EvaluatedAt"", newer.""Id"");");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ProjectEvaluation");

            // ---------- Projeto: campos da avaliação/priorização ----------
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentScore",
                table: "Projects",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationStatus",
                table: "Projects",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NaoAvaliado");

            migrationBuilder.Sql(@"
UPDATE ""Projects"" p SET ""EvaluationStatus"" = 'Avaliando'
WHERE EXISTS (SELECT 1 FROM ""ProjectEvaluation"" e WHERE e.""ProjectId"" = p.""Id"");");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrioritizationDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Projects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Media");

            migrationBuilder.AddColumn<int>(
                name: "RankingPosition",
                table: "Projects",
                type: "integer",
                nullable: true);

            // ---------- CriterioPrioridade ----------
            // Pesos antigos eram percentuais (soma 100). Novo intervalo (0, 10]: divide por 10;
            // peso 0 vira 1 (neutro).
            migrationBuilder.Sql(@"
UPDATE ""PriorityCriteria"" SET ""ValueWeight"" = CASE
    WHEN ""ValueWeight"" <= 0 THEN 1
    ELSE LEAST(10, GREATEST(0.01, round(""ValueWeight"" / 10, 2))) END;");

            migrationBuilder.AlterColumn<decimal>(
                name: "ValueWeight",
                table: "PriorityCriteria",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            // Nome obrigatório (100) e único no portfólio: nomes vazios ou repetidos recebem sufixo.
            migrationBuilder.Sql(@"
UPDATE ""PriorityCriteria"" SET ""Name"" = 'Critério sem nome' WHERE ""Name"" IS NULL OR trim(""Name"") = '';
UPDATE ""PriorityCriteria"" SET ""Name"" = left(trim(""Name""), 100);
WITH dup AS (
    SELECT ""Id"", row_number() OVER (PARTITION BY ""PortfolioId"", lower(""Name"") ORDER BY ""CreatedDate"", ""Id"") AS n
    FROM ""PriorityCriteria"")
UPDATE ""PriorityCriteria"" c SET ""Name"" = left(c.""Name"", 90) || ' (' || dup.n || ')'
FROM dup WHERE dup.""Id"" = c.""Id"" AND dup.n > 1;");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PriorityCriteria",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PriorityCriteria",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "PriorityCriteria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Beneficio");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluation_ProjectId_PriorityCriteriaId",
                table: "ProjectEvaluation",
                columns: new[] { "ProjectId", "PriorityCriteriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriorityCriteria_PortfolioId_Name",
                table: "PriorityCriteria",
                columns: new[] { "PortfolioId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEvaluation_PriorityCriteria_PriorityCriteriaId",
                table: "ProjectEvaluation",
                column: "PriorityCriteriaId",
                principalTable: "PriorityCriteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEvaluation_PriorityCriteria_PriorityCriteriaId",
                table: "ProjectEvaluation");

            migrationBuilder.DropIndex(
                name: "IX_ProjectEvaluation_ProjectId_PriorityCriteriaId",
                table: "ProjectEvaluation");

            migrationBuilder.DropIndex(
                name: "IX_PriorityCriteria_PortfolioId_Name",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "CurrentScore",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EvaluationStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LastPrioritizationDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RankingPosition",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EvaluatedAt",
                table: "ProjectEvaluation");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "ProjectEvaluation");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PriorityCriteria");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "ProjectEvaluation",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "ValueWeight",
                table: "PriorityCriteria",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PriorityCriteria",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluation_ProjectId",
                table: "ProjectEvaluation",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PriorityCriteria_PortfolioId",
                table: "PriorityCriteria",
                column: "PortfolioId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEvaluation_PriorityCriteria_PriorityCriteriaId",
                table: "ProjectEvaluation",
                column: "PriorityCriteriaId",
                principalTable: "PriorityCriteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
