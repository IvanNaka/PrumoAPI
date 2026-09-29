using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prumo.Infrastructure.Migrations
{
    /// <summary>
    /// T01 - Padroniza os enums (Seção 3.1 do plano de conformidade) e grava todos como texto.
    /// Converte os valores antigos que já estavam no banco.
    /// </summary>
    public partial class T01_Enums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Integration.Type era gravado como inteiro (ordem do enum IntegrationType).
            migrationBuilder.Sql(@"
ALTER TABLE ""Integrations"" ALTER COLUMN ""Type"" TYPE text USING
  CASE ""Type"" WHEN 0 THEN 'Jira' WHEN 1 THEN 'AzureDevOps'
                WHEN 2 THEN 'GitHub' WHEN 3 THEN 'Trello' END;");

            // Alert.Type (Warning/Critical/Info) não tem equivalente em TipoNotificacao e a tabela
            // não era alimentada por nenhum código; os registros antigos são descartados.
            migrationBuilder.Sql(@"DELETE FROM ""Alerts"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Alerts"" ALTER COLUMN ""Type"" TYPE text USING ""Type""::text;");

            // ProjectStatus: Active/Paused/Completed -> valores da Figura 26.
            migrationBuilder.Sql(@"
UPDATE ""Projects"" SET ""Status"" = CASE ""Status""
    WHEN 'Active' THEN 'EmAndamento'
    WHEN 'Paused' THEN 'Suspenso'
    WHEN 'Completed' THEN 'Concluido'
    ELSE ""Status"" END;");

            // RoleName: nomes antigos -> nomes da Seção 3.1. ScrumMaster deixa de existir (D02):
            // os usuários com esse perfil passam para TechLead (ver 'Dúvidas em aberto').
            migrationBuilder.Sql(@"
UPDATE ""Roles"" SET ""Name"" = CASE ""Name""
    WHEN 'Admin' THEN 'Administrador'
    WHEN 'PO' THEN 'ProductOwner'
    WHEN 'Gerente' THEN 'GerenteProjeto'
    WHEN 'DEV' THEN 'Desenvolvedor'
    ELSE ""Name"" END;

INSERT INTO ""Roles"" (""Id"", ""Name"", ""CreatedDate"", ""Active"")
SELECT gen_random_uuid(), 'TechLead', now(), true
WHERE EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'ScrumMaster')
  AND NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'TechLead');

UPDATE ""Users"" SET ""RoleId"" = (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'TechLead' LIMIT 1)
WHERE ""RoleId"" IN (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'ScrumMaster');

DELETE FROM ""Roles"" WHERE ""Name"" = 'ScrumMaster';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Integrations"" ALTER COLUMN ""Type"" TYPE integer USING
  CASE ""Type"" WHEN 'Jira' THEN 0 WHEN 'AzureDevOps' THEN 1
                WHEN 'GitHub' THEN 2 WHEN 'Trello' THEN 3 END;");
            migrationBuilder.Sql(@"DELETE FROM ""Alerts"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Alerts"" ALTER COLUMN ""Type"" TYPE integer USING 0;");
            migrationBuilder.Sql(@"
UPDATE ""Projects"" SET ""Status"" = CASE ""Status""
    WHEN 'Suspenso' THEN 'Paused'
    WHEN 'Concluido' THEN 'Completed'
    ELSE 'Active' END;");
            migrationBuilder.Sql(@"
UPDATE ""Roles"" SET ""Name"" = CASE ""Name""
    WHEN 'Administrador' THEN 'Admin'
    WHEN 'ProductOwner' THEN 'PO'
    WHEN 'GerenteProjeto' THEN 'Gerente'
    WHEN 'Desenvolvedor' THEN 'DEV'
    ELSE ""Name"" END;");
        }
    }
}
