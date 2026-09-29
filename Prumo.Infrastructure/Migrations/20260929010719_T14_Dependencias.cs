using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T14_Dependencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove auto-dependências (RN20) e duplicadas (RN21) antes do índice único.
            migrationBuilder.Sql(@"
DELETE FROM ""ProjectDependency"" WHERE ""ProjectId"" = ""DependsOnProjectId"";
DELETE FROM ""ProjectDependency"" d USING ""ProjectDependency"" older
WHERE d.""ProjectId"" = older.""ProjectId"" AND d.""DependsOnProjectId"" = older.""DependsOnProjectId""
  AND (d.""CreatedDate"", d.""Id"") > (older.""CreatedDate"", older.""Id"");");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependency_Portfolios_PortfolioId",
                table: "ProjectDependency");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependency_Projects_DependsOnProjectId",
                table: "ProjectDependency");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependency_Projects_ProjectId",
                table: "ProjectDependency");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependency_Users_UserId",
                table: "ProjectDependency");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectDependency",
                table: "ProjectDependency");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDependency_ProjectId",
                table: "ProjectDependency");

            migrationBuilder.RenameTable(
                name: "ProjectDependency",
                newName: "ProjectDependencies");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDependency_UserId",
                table: "ProjectDependencies",
                newName: "IX_ProjectDependencies_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDependency_PortfolioId",
                table: "ProjectDependencies",
                newName: "IX_ProjectDependencies_PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDependency_DependsOnProjectId",
                table: "ProjectDependencies",
                newName: "IX_ProjectDependencies_DependsOnProjectId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ProjectDependencies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectDependencies",
                table: "ProjectDependencies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_ProjectId_DependsOnProjectId",
                table: "ProjectDependencies",
                columns: new[] { "ProjectId", "DependsOnProjectId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependencies_Portfolios_PortfolioId",
                table: "ProjectDependencies",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependencies_Projects_DependsOnProjectId",
                table: "ProjectDependencies",
                column: "DependsOnProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependencies_Projects_ProjectId",
                table: "ProjectDependencies",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependencies_Users_UserId",
                table: "ProjectDependencies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependencies_Portfolios_PortfolioId",
                table: "ProjectDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependencies_Projects_DependsOnProjectId",
                table: "ProjectDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependencies_Projects_ProjectId",
                table: "ProjectDependencies");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDependencies_Users_UserId",
                table: "ProjectDependencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectDependencies",
                table: "ProjectDependencies");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDependencies_ProjectId_DependsOnProjectId",
                table: "ProjectDependencies");

            migrationBuilder.RenameTable(
                name: "ProjectDependencies",
                newName: "ProjectDependency");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDependencies_UserId",
                table: "ProjectDependency",
                newName: "IX_ProjectDependency_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDependencies_PortfolioId",
                table: "ProjectDependency",
                newName: "IX_ProjectDependency_PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDependencies_DependsOnProjectId",
                table: "ProjectDependency",
                newName: "IX_ProjectDependency_DependsOnProjectId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ProjectDependency",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectDependency",
                table: "ProjectDependency",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependency_ProjectId",
                table: "ProjectDependency",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependency_Portfolios_PortfolioId",
                table: "ProjectDependency",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependency_Projects_DependsOnProjectId",
                table: "ProjectDependency",
                column: "DependsOnProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependency_Projects_ProjectId",
                table: "ProjectDependency",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDependency_Users_UserId",
                table: "ProjectDependency",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
