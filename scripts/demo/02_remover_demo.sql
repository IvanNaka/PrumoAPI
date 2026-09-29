-- =====================================================================================
-- Prumo — remove a carga de DEMONSTRAÇÃO criada por 01_seed_demo.sql (PostgreSQL)
--
-- Apaga tudo que tem ID começando com "de", incluindo o que foi criado durante a apresentação
-- dentro dos portfólios de demonstração (projetos, critérios, avaliações, relatórios...).
-- NÃO mexe no seu usuário nem no perfil Administrador que a carga adicionou a ele.
-- =====================================================================================

BEGIN;

DELETE FROM "Alerts"              WHERE "Id"::text LIKE 'de%' OR "EntityId"::text LIKE 'de%';
DELETE FROM "Reports"             WHERE "Id"::text LIKE 'de%' OR "PortfolioId"::text LIKE 'de%';
DELETE FROM "Integrations"        WHERE "Id"::text LIKE 'de%';
DELETE FROM "RoadmapItems"        WHERE "PortfolioId"::text LIKE 'de%' OR "ProjectId"::text LIKE 'de%';
DELETE FROM "ProjectDependencies" WHERE "PortfolioId"::text LIKE 'de%' OR "ProjectId"::text LIKE 'de%' OR "DependsOnProjectId"::text LIKE 'de%';
DELETE FROM "ProjectEvaluation"   WHERE "ProjectId"::text LIKE 'de%' OR "PriorityCriteriaId"::text LIKE 'de%'
                                     OR "ProjectId" IN (SELECT "Id" FROM "Projects" WHERE "PortfolioId"::text LIKE 'de%')
                                     OR "PriorityCriteriaId" IN (SELECT "Id" FROM "PriorityCriteria" WHERE "PortfolioId"::text LIKE 'de%');
DELETE FROM "Projects"            WHERE "Id"::text LIKE 'de%' OR "PortfolioId"::text LIKE 'de%' OR "OwnerId"::text LIKE 'de%';
DELETE FROM "PriorityCriteria"    WHERE "PortfolioId"::text LIKE 'de%';
DELETE FROM "Teams"               WHERE "Id"::text LIKE 'de%';
DELETE FROM "Portfolios"          WHERE "Id"::text LIKE 'de%' OR "OwnerId"::text LIKE 'de%';
DELETE FROM "Objectives"          WHERE "Id"::text LIKE 'de%';
DELETE FROM "Users"               WHERE "Id"::text LIKE 'de%';

COMMIT;
