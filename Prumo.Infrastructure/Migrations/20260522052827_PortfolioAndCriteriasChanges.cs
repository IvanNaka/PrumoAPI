using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortfolioAndCriteriasChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMetrics");

            migrationBuilder.DropColumn(
                name: "AlignmentScore",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EffortScore",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ValueScore",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AlignmentWeight",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "EffortWeight",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "RiskWeight",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PriorityCriteria");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PriorityCriteria",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PortfolioId",
                table: "PriorityCriteria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PortfolioId1",
                table: "PriorityCriteria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PriorityCriteria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PortfolioMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueWeight = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ProjectEvaluation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioMetricId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEvaluation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEvaluation_PortfolioMetrics_PortfolioMetricId",
                        column: x => x.PortfolioMetricId,
                        principalTable: "PortfolioMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectEvaluation_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectEvaluation_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriorityCriteria_PortfolioId",
                table: "PriorityCriteria",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_PriorityCriteria_PortfolioId1",
                table: "PriorityCriteria",
                column: "PortfolioId1");

            migrationBuilder.CreateIndex(
                name: "IX_PriorityCriteria_UserId",
                table: "PriorityCriteria",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioMetrics_ProjectId",
                table: "PortfolioMetrics",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluation_PortfolioMetricId",
                table: "ProjectEvaluation",
                column: "PortfolioMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluation_ProjectId",
                table: "ProjectEvaluation",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvaluation_UserId",
                table: "ProjectEvaluation",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriorityCriteria_Portfolios_PortfolioId",
                table: "PriorityCriteria",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PriorityCriteria_Portfolios_PortfolioId1",
                table: "PriorityCriteria",
                column: "PortfolioId1",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PriorityCriteria_Users_UserId",
                table: "PriorityCriteria",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriorityCriteria_Portfolios_PortfolioId",
                table: "PriorityCriteria");

            migrationBuilder.DropForeignKey(
                name: "FK_PriorityCriteria_Portfolios_PortfolioId1",
                table: "PriorityCriteria");

            migrationBuilder.DropForeignKey(
                name: "FK_PriorityCriteria_Users_UserId",
                table: "PriorityCriteria");

            migrationBuilder.DropTable(
                name: "ProjectEvaluation");

            migrationBuilder.DropTable(
                name: "PortfolioMetrics");

            migrationBuilder.DropIndex(
                name: "IX_PriorityCriteria_PortfolioId",
                table: "PriorityCriteria");

            migrationBuilder.DropIndex(
                name: "IX_PriorityCriteria_PortfolioId1",
                table: "PriorityCriteria");

            migrationBuilder.DropIndex(
                name: "IX_PriorityCriteria_UserId",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "PortfolioId1",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PriorityCriteria");

            migrationBuilder.AddColumn<decimal>(
                name: "AlignmentScore",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "EffortScore",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriorityScore",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskScore",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueScore",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AlignmentWeight",
                table: "PriorityCriteria",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EffortWeight",
                table: "PriorityCriteria",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskWeight",
                table: "PriorityCriteria",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PriorityCriteria",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ProjectMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Effort = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Risk = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMetrics_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMetrics_ProjectId",
                table: "ProjectMetrics",
                column: "ProjectId");
        }
    }
}
