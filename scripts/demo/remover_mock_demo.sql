-- =============================================================================
-- Prumo · Remove o mock da demonstração (desfaz criar_mock_demo.sql)
-- -----------------------------------------------------------------------------
-- Apaga o portfólio "Estratégia Digital 2027" (de300000-…-000000000001) e tudo
-- que pertence a ele, INCLUSIVE o que foi criado ou alterado durante a demo
-- (notas, critérios, projetos, dependências, relatórios gerados, notificações),
-- além das 2 equipes e dos 3 OKRs do mock.
-- Não toca em usuários, perfis, integrações nem em outros portfólios.
--
-- Uso:  psql "<connection string>" -v ON_ERROR_STOP=1 -f remover_mock_demo.sql
-- Pode rodar mais de uma vez (se não houver mock, não faz nada).
-- =============================================================================
\set ON_ERROR_STOP on
BEGIN;

CREATE TEMP TABLE demo_portfolio ON COMMIT DROP AS
SELECT 'de300000-0000-4000-8000-000000000001'::uuid AS id;

CREATE TEMP TABLE demo_projects ON COMMIT DROP AS
SELECT "Id" AS id FROM "Projects" WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio);

CREATE TEMP TABLE demo_teams ON COMMIT DROP AS
SELECT "Id" AS id FROM "Teams"
 WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio)
    OR "Id" IN ('de300000-0000-4000-8000-000000000401', 'de300000-0000-4000-8000-000000000402');

CREATE TEMP TABLE demo_objectives ON COMMIT DROP AS
SELECT unnest(ARRAY['de300000-0000-4000-8000-000000000301',
                    'de300000-0000-4000-8000-000000000302',
                    'de300000-0000-4000-8000-000000000303']::uuid[]) AS id;

-- Issues e worklogs ("Jira")
DELETE FROM "Worklogs"  WHERE "IssueId" IN (SELECT "Id" FROM "Issues" WHERE "ProjectId" IN (SELECT id FROM demo_projects));
DELETE FROM "Issues"    WHERE "ProjectId" IN (SELECT id FROM demo_projects);

-- Financeiro
DELETE FROM "CashFlowForecasts" WHERE "BusinessCaseId" IN (SELECT "Id" FROM "BusinessCases" WHERE "ProjectId" IN (SELECT id FROM demo_projects));
DELETE FROM "BusinessCases"     WHERE "ProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "RealizedReturns"   WHERE "ProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "BudgetExpenses"    WHERE "ProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "Budgets"           WHERE "ProjectId" IN (SELECT id FROM demo_projects);

-- Priorização, dependências e vínculos dos projetos
DELETE FROM "ProjectEvaluation"   WHERE "ProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "ProjectDependencies" WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio)
                                     OR "ProjectId" IN (SELECT id FROM demo_projects)
                                     OR "DependsOnProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "ProjectObjectives"   WHERE "ProjectId" IN (SELECT id FROM demo_projects) OR "ObjectiveId" IN (SELECT id FROM demo_objectives);
DELETE FROM "ProjectMembers"      WHERE "ProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "ProjectTeams"        WHERE "ProjectId" IN (SELECT id FROM demo_projects) OR "TeamId" IN (SELECT id FROM demo_teams);
DELETE FROM "RoadmapItems"        WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio) OR "ProjectId" IN (SELECT id FROM demo_projects);
DELETE FROM "Reports"             WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio);

-- Notificações geradas a partir do mock
DELETE FROM "Alerts" WHERE "EntityId" IN (SELECT id FROM demo_portfolio UNION ALL SELECT id FROM demo_projects
                                          UNION ALL SELECT id FROM demo_teams UNION ALL SELECT id FROM demo_objectives);

-- Equipes
DELETE FROM "TeamUsers" WHERE "TeamId" IN (SELECT id FROM demo_teams);
DELETE FROM "Teams"     WHERE "Id" IN (SELECT id FROM demo_teams);

-- Projetos e critérios
DELETE FROM "Projects"         WHERE "Id" IN (SELECT id FROM demo_projects);
DELETE FROM "PriorityCriteria" WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio);

-- OKRs
DELETE FROM "PortfolioObjectives" WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio) OR "ObjectiveId" IN (SELECT id FROM demo_objectives);
DELETE FROM "KeyResults"          WHERE "ObjectiveId" IN (SELECT id FROM demo_objectives);
DELETE FROM "Objectives"          WHERE "Id" IN (SELECT id FROM demo_objectives);

-- Portfólio
DELETE FROM "PortfolioMembers" WHERE "PortfolioId" IN (SELECT id FROM demo_portfolio);
DELETE FROM "Portfolios"       WHERE "Id" IN (SELECT id FROM demo_portfolio);

COMMIT;

SELECT CASE WHEN EXISTS (SELECT 1 FROM "Portfolios" WHERE "Id" = 'de300000-0000-4000-8000-000000000001')
            THEN 'ATENÇÃO: o portfólio do mock ainda existe'
            ELSE 'Mock da demo removido.' END AS resultado;
