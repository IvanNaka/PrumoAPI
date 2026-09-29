using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T28_NormalizarPerfisLegados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bancos criados com versões antigas do enum RoleName (Admin/Strategic/Contributor/Viewer e
            // Admin/PO/Gerente/ScrumMaster/DEV) podem ter chegado ao T02 com nomes que a T01 não converteu.
            // Esses valores quebram a leitura de UserRoles (StringEnumConverter) -> converte para os nomes atuais.
            migrationBuilder.Sql(@"
INSERT INTO ""UserRoles"" (""UserId"", ""Role"")
SELECT ""UserId"", CASE ""Role""
    WHEN 'Admin' THEN 'Administrador'
    WHEN 'Strategic' THEN 'Diretoria'
    WHEN 'Contributor' THEN 'Desenvolvedor'
    WHEN 'Viewer' THEN 'Desenvolvedor'
    WHEN 'PO' THEN 'ProductOwner'
    WHEN 'Gerente' THEN 'GerenteProjeto'
    WHEN 'ScrumMaster' THEN 'TechLead'
    WHEN 'DEV' THEN 'Desenvolvedor'
    END
FROM ""UserRoles""
WHERE ""Role"" IN ('Admin', 'Strategic', 'Contributor', 'Viewer', 'PO', 'Gerente', 'ScrumMaster', 'DEV')
ON CONFLICT DO NOTHING;

DELETE FROM ""UserRoles""
WHERE ""Role"" NOT IN ('Desenvolvedor', 'QA', 'ProductOwner', 'TechLead', 'GerenteProjeto', 'Diretoria', 'Administrador');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Conversão de dados sem volta: os nomes antigos não existem mais no enum.
        }
    }
}
