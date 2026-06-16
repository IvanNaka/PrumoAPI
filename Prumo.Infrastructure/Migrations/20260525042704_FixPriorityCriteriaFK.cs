using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPriorityCriteriaFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriorityCriteria_Portfolios_PortfolioId1",
                table: "PriorityCriteria");

            migrationBuilder.DropIndex(
                name: "IX_PriorityCriteria_PortfolioId1",
                table: "PriorityCriteria");

            migrationBuilder.DropColumn(
                name: "PortfolioId1",
                table: "PriorityCriteria");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PriorityCriteria",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PriorityCriteria",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PortfolioId1",
                table: "PriorityCriteria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PriorityCriteria_PortfolioId1",
                table: "PriorityCriteria",
                column: "PortfolioId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PriorityCriteria_Portfolios_PortfolioId1",
                table: "PriorityCriteria",
                column: "PortfolioId1",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
