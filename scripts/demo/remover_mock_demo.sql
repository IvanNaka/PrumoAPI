-- =============================================================================
-- Prumo · Remove o mock da demonstração (desfaz criar_mock_demo.sql)
-- -----------------------------------------------------------------------------
-- Apaga o portfólio "Estratégia Digital 2027" (de300000-…-000000000001) e tudo
-- que pertence a ele, INCLUSIVE o que foi criado ou alterado durante a demo
-- (notas, critérios, projetos, dependências, relatórios gerados, notificações),
-- além das 2 equipes e dos 3 OKRs do mock.
-- Não toca em usuários, perfis, integrações nem em outros portfólios.
--
-- Uso: cole no SQL Editor do Supabase e clique em Run, ou:
--      psql "<connection string>" -v ON_ERROR_STOP=1 -f remover_mock_demo.sql
--
-- É um bloco único (sem tabelas temporárias) e pula tabelas que não existirem
-- no banco. Pode rodar mais de uma vez: sem mock, não faz nada.
-- =============================================================================
DO $$
DECLARE
    v_portfolio  uuid   := 'de300000-0000-4000-8000-000000000001';
    v_projects   uuid[];
    v_teams      uuid[];
    v_objectives uuid[] := ARRAY['de300000-0000-4000-8000-000000000301',
                                 'de300000-0000-4000-8000-000000000302',
                                 'de300000-0000-4000-8000-000000000303']::uuid[];
    v_entities   uuid[];
    passo        record;
    n            bigint;
    total        bigint := 0;
BEGIN
    SELECT coalesce(array_agg("Id"), '{}') INTO v_projects FROM "Projects" WHERE "PortfolioId" = v_portfolio;
    SELECT coalesce(array_agg("Id"), '{}') INTO v_teams FROM "Teams"
     WHERE "PortfolioId" = v_portfolio
        OR "Id" IN ('de300000-0000-4000-8000-000000000401', 'de300000-0000-4000-8000-000000000402');
    v_entities := ARRAY[v_portfolio] || v_projects || v_teams || v_objectives;

    -- Ordem: filhos antes dos pais. $1 = portfólio, $2 = projetos, $3 = equipes, $4 = OKRs, $5 = todos os ids.
    FOR passo IN SELECT * FROM (VALUES
        (1,  'Worklogs',            'DELETE FROM "Worklogs" WHERE "IssueId" IN (SELECT "Id" FROM "Issues" WHERE "ProjectId" = ANY($2))'),
        (2,  'Issues',              'DELETE FROM "Issues" WHERE "ProjectId" = ANY($2)'),
        (3,  'CashFlowForecasts',   'DELETE FROM "CashFlowForecasts" WHERE "BusinessCaseId" IN (SELECT "Id" FROM "BusinessCases" WHERE "ProjectId" = ANY($2))'),
        (4,  'BusinessCases',       'DELETE FROM "BusinessCases" WHERE "ProjectId" = ANY($2)'),
        (5,  'RealizedReturns',     'DELETE FROM "RealizedReturns" WHERE "ProjectId" = ANY($2)'),
        (6,  'BudgetExpenses',      'DELETE FROM "BudgetExpenses" WHERE "ProjectId" = ANY($2)'),
        (7,  'Budgets',             'DELETE FROM "Budgets" WHERE "ProjectId" = ANY($2)'),
        (8,  'ProjectEvaluation',   'DELETE FROM "ProjectEvaluation" WHERE "ProjectId" = ANY($2) OR "PriorityCriteriaId" IN (SELECT "Id" FROM "PriorityCriteria" WHERE "PortfolioId" = $1)'),
        (9,  'ProjectDependencies', 'DELETE FROM "ProjectDependencies" WHERE "PortfolioId" = $1 OR "ProjectId" = ANY($2) OR "DependsOnProjectId" = ANY($2)'),
        (10, 'ProjectObjectives',   'DELETE FROM "ProjectObjectives" WHERE "ProjectId" = ANY($2) OR "ObjectiveId" = ANY($4)'),
        (11, 'ProjectMembers',      'DELETE FROM "ProjectMembers" WHERE "ProjectId" = ANY($2)'),
        (12, 'ProjectTeams',        'DELETE FROM "ProjectTeams" WHERE "ProjectId" = ANY($2) OR "TeamId" = ANY($3)'),
        (13, 'RoadmapItems',        'DELETE FROM "RoadmapItems" WHERE "PortfolioId" = $1 OR "ProjectId" = ANY($2)'),
        (14, 'Reports',             'DELETE FROM "Reports" WHERE "PortfolioId" = $1'),
        (15, 'Alerts',              'DELETE FROM "Alerts" WHERE "EntityId" = ANY($5)'),
        (16, 'TeamUsers',           'DELETE FROM "TeamUsers" WHERE "TeamId" = ANY($3)'),
        (17, 'Teams',               'DELETE FROM "Teams" WHERE "Id" = ANY($3)'),
        (18, 'Projects',            'DELETE FROM "Projects" WHERE "Id" = ANY($2)'),
        (19, 'PriorityCriteria',    'DELETE FROM "PriorityCriteria" WHERE "PortfolioId" = $1'),
        (20, 'PortfolioObjectives', 'DELETE FROM "PortfolioObjectives" WHERE "PortfolioId" = $1 OR "ObjectiveId" = ANY($4)'),
        (21, 'KeyResults',          'DELETE FROM "KeyResults" WHERE "ObjectiveId" = ANY($4)'),
        (22, 'Objectives',          'DELETE FROM "Objectives" WHERE "Id" = ANY($4)'),
        (23, 'PortfolioMembers',    'DELETE FROM "PortfolioMembers" WHERE "PortfolioId" = $1'),
        (24, 'Portfolios',          'DELETE FROM "Portfolios" WHERE "Id" = $1')
    ) AS s(ordem, tabela, comando) ORDER BY ordem
    LOOP
        IF to_regclass(format('%I', passo.tabela)) IS NULL THEN
            RAISE NOTICE 'Tabela % não existe neste banco; pulando.', passo.tabela;
            CONTINUE;
        END IF;
        EXECUTE passo.comando USING v_portfolio, v_projects, v_teams, v_objectives, v_entities;
        GET DIAGNOSTICS n = ROW_COUNT;
        total := total + n;
        IF n > 0 THEN
            RAISE NOTICE '%: % linha(s) removida(s).', passo.tabela, n;
        END IF;
    END LOOP;

    RAISE NOTICE 'Remoção concluída: % linha(s) no total.', total;
END $$;

SELECT CASE WHEN EXISTS (SELECT 1 FROM "Portfolios" WHERE "Id" = 'de300000-0000-4000-8000-000000000001')
            THEN 'ATENÇÃO: o portfólio do mock ainda existe'
            ELSE 'Mock da demo removido.' END AS resultado;
