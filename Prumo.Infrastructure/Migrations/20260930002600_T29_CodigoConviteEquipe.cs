using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T29_CodigoConviteEquipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Teams",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            // Equipes existentes recebem um código aleatório antes do índice único
            // (hexadecimal sem 0/1, dentro do alfabeto de Team.NewInviteCode).
            migrationBuilder.Sql(@"
UPDATE ""Teams""
SET ""InviteCode"" = translate(upper(substr(md5(random()::text || ""Id""::text), 1, 8)), '01', 'XY');");

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
