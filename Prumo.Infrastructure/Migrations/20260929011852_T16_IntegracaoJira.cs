using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T16_IntegracaoJira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalData");

            migrationBuilder.DropColumn(
                name: "LastSyncStatus",
                table: "Integrations");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Integrations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Integrations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Integrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "Integrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "Integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Integrations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // Integrações existentes: o token era gravado sem criptografia (e no formato email:token),
            // então precisam ser reconfiguradas -> ErroConexao. Só uma configuração por provedor.
            migrationBuilder.Sql(@"
UPDATE ""Integrations"" SET ""Status"" = 'ErroConexao', ""Token"" = '',
    ""SyncIntervalMinutes"" = LEAST(1440, GREATEST(15, ""SyncIntervalMinutes""));
DELETE FROM ""Integrations"" i USING ""Integrations"" newer
WHERE i.""Type"" = newer.""Type"" AND (i.""CreatedDate"", i.""Id"") < (newer.""CreatedDate"", newer.""Id"");");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    IssuesProcessed = table.Column<int>(type: "integer", nullable: false),
                    WorklogsProcessed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationSyncLogs_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Type",
                table: "Integrations",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationSyncLogs_IntegrationId_StartedAt",
                table: "IntegrationSyncLogs",
                columns: new[] { "IntegrationId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "IntegrationSyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_Type",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Integrations");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Integrations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Integrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncStatus",
                table: "Integrations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawDataJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalData_IntegrationId",
                table: "ExternalData",
                column: "IntegrationId");
        }
    }
}
