using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T12_Lancamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetExpenses_Budgets_BudgetId",
                table: "BudgetExpenses");

            migrationBuilder.DropColumn(
                name: "DiscountRateMonthly",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ExpectedReturn",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Budgets");

            migrationBuilder.RenameColumn(
                name: "BudgetId",
                table: "BudgetExpenses",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetExpenses_BudgetId",
                table: "BudgetExpenses",
                newName: "IX_BudgetExpenses_ProjectId");

            // O lançamento passa a apontar direto para o projeto (antes apontava para o Budget).
            migrationBuilder.Sql(@"
UPDATE ""BudgetExpenses"" e SET ""ProjectId"" = b.""ProjectId""
FROM ""Budgets"" b WHERE b.""Id"" = e.""ProjectId"";
UPDATE ""BudgetExpenses"" SET ""Description"" = left(""Description"", 300);");

            // Orcamento 1:1 com Projeto e com o mesmo valor do orçamento aprovado.
            migrationBuilder.Sql(@"
UPDATE ""Budgets"" b SET ""TotalAmount"" = p.""ApprovedBudget"" FROM ""Projects"" p WHERE p.""Id"" = b.""ProjectId"";
INSERT INTO ""Budgets"" (""Id"", ""ProjectId"", ""TotalAmount"", ""Currency"", ""CreatedDate"", ""Active"")
SELECT gen_random_uuid(), p.""Id"", p.""ApprovedBudget"", 'BRL', now(), true FROM ""Projects"" p
WHERE NOT EXISTS (SELECT 1 FROM ""Budgets"" b WHERE b.""ProjectId"" = p.""Id"");");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BudgetExpenses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "BudgetExpenses",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "BudgetExpenses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetExpenses_Projects_ProjectId",
                table: "BudgetExpenses",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetExpenses_Projects_ProjectId",
                table: "BudgetExpenses");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "BudgetExpenses",
                newName: "BudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetExpenses_ProjectId",
                table: "BudgetExpenses",
                newName: "IX_BudgetExpenses_BudgetId");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRateMonthly",
                table: "Budgets",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedReturn",
                table: "Budgets",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BudgetExpenses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "BudgetExpenses",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "BudgetExpenses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetExpenses_Budgets_BudgetId",
                table: "BudgetExpenses",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
