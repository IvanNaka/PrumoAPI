-- =====================================================================================
-- Prumo — RESETA OS PARTICIPANTES dos roteiros de avaliação (PostgreSQL)
--
-- Remove quem entrou nos times ROTEIRO1/2/3 (04_roteiros_avaliacao.sql) e tudo o que essas
-- pessoas criaram: portfólios próprios (com projetos, critérios, avaliações, dependências,
-- roadmap), OKRs ligados a esses portfólios, relatórios gerados e notificações.
-- Os usuários são apagados: no próximo login Google eles voltam à tela de boas-vindas e
-- podem refazer o roteiro do zero (inclusive a Atividade 2 - "Entrar no time").
--
-- Não mexe nos portfólios da carga de demonstração (só tira o participante deles), nos
-- times dos roteiros nem na integração Jira (que é única no sistema).
-- OKRs criados no Roteiro 1 sem vínculo com nenhum portfólio não têm dono no banco e ficam.
--
-- Para resetar só uma pessoa, troque o NULL abaixo pelo e-mail dela.
-- =====================================================================================

BEGIN;

DROP TABLE IF EXISTS demo_participantes;
CREATE TEMP TABLE demo_participantes AS
SELECT DISTINCT u."Id" AS uid
FROM "TeamUsers" tu
JOIN "Users" u ON u."Id" = tu."UserId"
WHERE tu."TeamId" IN ('de110000-0000-4000-8000-000000000001',
                      'de110000-0000-4000-8000-000000000002',
                      'de110000-0000-4000-8000-000000000003')
  AND lower(u."Email") = lower(COALESCE(NULL::text /* ex.: 'participante@gmail.com' */, u."Email"))
  AND NOT EXISTS (SELECT 1 FROM "UserRoles" r WHERE r."UserId" = u."Id" AND r."Role" = 'Administrador');

DROP TABLE IF EXISTS demo_pf_participantes;
CREATE TEMP TABLE demo_pf_participantes AS
SELECT p."Id" AS pid FROM "Portfolios" p WHERE p."OwnerId" IN (SELECT uid FROM demo_participantes);

-- OKRs ligados somente a portfólios/projetos dos participantes
DROP TABLE IF EXISTS demo_okr_participantes;
CREATE TEMP TABLE demo_okr_participantes AS
SELECT o."Id" AS oid
FROM "Objectives" o
WHERE (EXISTS (SELECT 1 FROM "PortfolioObjectives" po WHERE po."ObjectiveId" = o."Id" AND po."PortfolioId" IN (SELECT pid FROM demo_pf_participantes))
       OR EXISTS (SELECT 1 FROM "ProjectObjectives" pj JOIN "Projects" p ON p."Id" = pj."ProjectId"
                  WHERE pj."ObjectiveId" = o."Id" AND p."PortfolioId" IN (SELECT pid FROM demo_pf_participantes)))
  AND NOT EXISTS (SELECT 1 FROM "PortfolioObjectives" po WHERE po."ObjectiveId" = o."Id" AND po."PortfolioId" NOT IN (SELECT pid FROM demo_pf_participantes))
  AND NOT EXISTS (SELECT 1 FROM "ProjectObjectives" pj JOIN "Projects" p ON p."Id" = pj."ProjectId"
                  WHERE pj."ObjectiveId" = o."Id" AND p."PortfolioId" NOT IN (SELECT pid FROM demo_pf_participantes));

DELETE FROM "Reports"             WHERE "GeneratedById" IN (SELECT uid FROM demo_participantes) OR "PortfolioId" IN (SELECT pid FROM demo_pf_participantes);
DELETE FROM "RoadmapItems"        WHERE "PortfolioId" IN (SELECT pid FROM demo_pf_participantes);
DELETE FROM "ProjectDependencies" WHERE "PortfolioId" IN (SELECT pid FROM demo_pf_participantes) OR "UserId" IN (SELECT uid FROM demo_participantes);
DELETE FROM "ProjectEvaluation"   WHERE "UserId" IN (SELECT uid FROM demo_participantes)
                                     OR "ProjectId" IN (SELECT "Id" FROM "Projects" WHERE "PortfolioId" IN (SELECT pid FROM demo_pf_participantes));
DELETE FROM "Projects"            WHERE "PortfolioId" IN (SELECT pid FROM demo_pf_participantes) OR "OwnerId" IN (SELECT uid FROM demo_participantes);
DELETE FROM "PriorityCriteria"    WHERE "PortfolioId" IN (SELECT pid FROM demo_pf_participantes) OR "UserId" IN (SELECT uid FROM demo_participantes);
DELETE FROM "Teams"               WHERE "PortfolioId" IN (SELECT pid FROM demo_pf_participantes);
DELETE FROM "Portfolios"          WHERE "Id" IN (SELECT pid FROM demo_pf_participantes);
DELETE FROM "Objectives"          WHERE "Id" IN (SELECT oid FROM demo_okr_participantes);
DELETE FROM "TeamUsers"           WHERE "UserId" IN (SELECT uid FROM demo_participantes);
DELETE FROM "Users"               WHERE "Id" IN (SELECT uid FROM demo_participantes);

SELECT (SELECT count(*) FROM demo_participantes) AS participantes_removidos,
       (SELECT count(*) FROM demo_pf_participantes) AS portfolios_removidos,
       (SELECT count(*) FROM demo_okr_participantes) AS okrs_removidos;

COMMIT;
