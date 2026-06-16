using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixProjectEvaluationDeletePortfolioMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEvaluation_PortfolioMetrics_PortfolioMetricId",
                table: "ProjectEvaluation");

            migrationBuilder.DropTable(
                name: "PortfolioMetrics");

            migrationBuilder.RenameColumn(
                name: "PortfolioMetricId",
                table: "ProjectEvaluation",
                newName: "PriorityCriteriaId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEvaluation_PortfolioMetricId",
                table: "ProjectEvaluation",
                newName: "IX_ProjectEvaluation_PriorityCriteriaId");

            migrationBuilder.AlterColumn<bool>(
                name: "Active",
                table: "ProjectEvaluation",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEvaluation_PriorityCriteria_PriorityCriteriaId",
                table: "ProjectEvaluation",
                column: "PriorityCriteriaId",
                principalTable: "PriorityCriteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEvaluation_PriorityCriteria_PriorityCriteriaId",
                table: "ProjectEvaluation");

            migrationBuilder.RenameColumn(
                name: "PriorityCriteriaId",
                table: "ProjectEvaluation",
                newName: "PortfolioMetricId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEvaluation_PriorityCriteriaId",
                table: "ProjectEvaluation",
                newName: "IX_ProjectEvaluation_PortfolioMetricId");

            migrationBuilder.AlterColumn<bool>(
                name: "Active",
                table: "ProjectEvaluation",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.CreateTable(
                name: "PortfolioMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueWeight = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioMetrics_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioMetrics_ProjectId",
                table: "PortfolioMetrics",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEvaluation_PortfolioMetrics_PortfolioMetricId",
                table: "ProjectEvaluation",
                column: "PortfolioMetricId",
                principalTable: "PortfolioMetrics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
