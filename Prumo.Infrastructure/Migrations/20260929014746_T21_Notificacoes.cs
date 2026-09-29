using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T21_Notificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alertas antigos eram por projeto (sem destinatário) e já tinham sido descartados em T01_Enums;
            // qualquer registro remanescente não tem como virar notificação de um usuário.
            migrationBuilder.Sql("DELETE FROM \"Alerts\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Projects_ProjectId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_ProjectId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "Alerts",
                newName: "UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Alerts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Alerts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "Alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Alerts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Alerts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_UserId_Status",
                table: "Alerts",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_UserId_Type_EntityId",
                table: "Alerts",
                columns: new[] { "UserId", "Type", "EntityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Users_UserId",
                table: "Alerts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Users_UserId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_UserId_Status",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_UserId_Type_EntityId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Alerts",
                newName: "ProjectId");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Alerts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Alerts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_ProjectId",
                table: "Alerts",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Projects_ProjectId",
                table: "Alerts",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
