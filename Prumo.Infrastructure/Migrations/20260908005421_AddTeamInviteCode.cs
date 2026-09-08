using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column as nullable first so existing rows are not rejected, then
            // backfill each existing team with a random unique code before enforcing the
            // NOT NULL + unique constraints required by the model.
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Teams",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Teams""
                SET ""InviteCode"" = upper(substring(md5(random()::text || clock_timestamp()::text || ""Id""::text) from 1 for 8))
                WHERE ""InviteCode"" IS NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "InviteCode",
                table: "Teams",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_InviteCode",
                table: "Teams",
                column: "InviteCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_InviteCode",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Teams");
        }
    }
}
