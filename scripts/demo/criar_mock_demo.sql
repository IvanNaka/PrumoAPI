-- =============================================================================
-- Prumo · Mock para a demonstração ao vivo
-- -----------------------------------------------------------------------------
-- Cria o portfólio "Estratégia Digital 2027" com critérios, 8 projetos, notas,
-- score/ranking já calculados, OKRs, orçamento, business cases, dependências,
-- 2 equipes e issues/worklogs para o dashboard e a capacidade.
--
-- Uso: cole no SQL Editor do Supabase e clique em Run, ou:
--        psql "<connection string>" -v ON_ERROR_STOP=1 -f criar_mock_demo.sql
-- Desfazer:  psql "<connection string>" -v ON_ERROR_STOP=1 -f remover_mock_demo.sql
--
-- Tudo roda em uma única transação: se algo falhar, nada é gravado.
-- Os ids fixos começam com de300000-…, e o script de remoção usa esses ids.
-- Rode no dia da apresentação: a capacidade das equipes é calculada no mês
-- corrente, e os worklogs são lançados no mês em que o script roda.
-- =============================================================================
BEGIN;

DO $$
DECLARE
    v_portfolio uuid := 'de300000-0000-4000-8000-000000000001';
    -- critérios
    c_valor uuid := 'de300000-0000-4000-8000-000000000101';
    c_okr   uuid := 'de300000-0000-4000-8000-000000000102';
    c_esf   uuid := 'de300000-0000-4000-8000-000000000103';
    c_risco uuid := 'de300000-0000-4000-8000-000000000104';
    -- projetos
    p_app      uuid := 'de300000-0000-4000-8000-000000000201';
    p_openfin  uuid := 'de300000-0000-4000-8000-000000000202';
    p_ia       uuid := 'de300000-0000-4000-8000-000000000203';
    p_nuvem    uuid := 'de300000-0000-4000-8000-000000000204';
    p_erp      uuid := 'de300000-0000-4000-8000-000000000205';
    p_bi       uuid := 'de300000-0000-4000-8000-000000000206';
    p_lgpd     uuid := 'de300000-0000-4000-8000-000000000207';
    p_parceiro uuid := 'de300000-0000-4000-8000-000000000208';
    -- OKRs
    o_cliente uuid := 'de300000-0000-4000-8000-000000000301';
    o_receita uuid := 'de300000-0000-4000-8000-000000000302';
    o_seguro  uuid := 'de300000-0000-4000-8000-000000000303';
    -- equipes
    t_canais uuid := 'de300000-0000-4000-8000-000000000401';
    t_plat   uuid := 'de300000-0000-4000-8000-000000000402';

    u_po uuid; u_gp uuid; u_tl uuid;
    hoje date := current_date;
    ini_mes date := date_trunc('month', current_date)::date;
    agora timestamptz := now();
    r record;
    bc uuid;
    iss uuid;
    seq int := 0;
    m int;
BEGIN
    IF EXISTS (SELECT 1 FROM "Portfolios" WHERE "Id" = v_portfolio) THEN
        RAISE EXCEPTION 'O mock da demo já existe. Rode remover_mock_demo.sql antes de criar de novo.';
    END IF;
    IF EXISTS (SELECT 1 FROM "Teams" WHERE "Name" IN ('Squad Canais Digitais', 'Squad Plataforma')
                                       OR "InviteCode" IN ('DEMOCAN7', 'DEMOPLT7')) THEN
        RAISE EXCEPTION 'Já existe uma equipe com o nome ou código de convite usado pelo mock.';
    END IF;

    -- Usuários de cada perfil: dá preferência às contas do SeedValidacao (e-mails padrão),
    -- senão pega o primeiro usuário ativo com o perfil.
    SELECT u."Id" INTO u_po FROM "Users" u JOIN "UserRoles" ur ON ur."UserId" = u."Id"
     WHERE ur."Role" = 'ProductOwner' AND u."IsActive" ORDER BY (u."Email" = 'po@prumo') DESC, u."CreatedDate" LIMIT 1;
    SELECT u."Id" INTO u_gp FROM "Users" u JOIN "UserRoles" ur ON ur."UserId" = u."Id"
     WHERE ur."Role" = 'GerenteProjeto' AND u."IsActive" ORDER BY (u."Email" = 'gerente@prumo') DESC, u."CreatedDate" LIMIT 1;
    SELECT u."Id" INTO u_tl FROM "Users" u JOIN "UserRoles" ur ON ur."UserId" = u."Id"
     WHERE ur."Role" = 'TechLead' AND u."IsActive" ORDER BY (u."Email" = 'techlead@prumo') DESC, u."CreatedDate" LIMIT 1;
    IF u_po IS NULL OR u_gp IS NULL OR u_tl IS NULL THEN
        RAISE EXCEPTION 'Faltam usuários com os perfis ProductOwner, GerenteProjeto e TechLead.';
    END IF;

    -- ---------------------------------------------------------------- portfólio
    INSERT INTO "Portfolios" ("Id","Name","Description","Goal","OwnerId","Status","Active","CreatedDate")
    VALUES (v_portfolio, 'Estratégia Digital 2027',
            'Iniciativas que levam os canais de atendimento e a plataforma de dados para o digital.',
            'Dobrar a receita dos canais digitais e elevar o NPS para 75 até dez/2027.',
            u_gp, 'Priorizado', true, agora);

    -- Todos os usuários ativos com algum perfil de negócio viram membros do portfólio.
    INSERT INTO "PortfolioMembers" ("PortfolioId","UserId","CreatedDate")
    SELECT DISTINCT v_portfolio, u."Id", agora
      FROM "Users" u JOIN "UserRoles" ur ON ur."UserId" = u."Id"
     WHERE u."IsActive" AND ur."Role" IN ('ProductOwner','GerenteProjeto','Diretoria','TechLead','Desenvolvedor','QA');

    -- ---------------------------------------------------------------- critérios (soma dos pesos = 10)
    INSERT INTO "PriorityCriteria" ("Id","Name","Description","Type","ValueWeight","PortfolioId","UserId","Active","CreatedDate") VALUES
      (c_valor, 'Valor de negócio',        'Receita ou economia esperada.',                   'Beneficio', 4, v_portfolio, u_po, true, agora),
      (c_okr,   'Alinhamento aos OKRs',    'Quanto o projeto move os objetivos da empresa.',  'Beneficio', 3, v_portfolio, u_po, true, agora),
      (c_esf,   'Esforço',                 'Tamanho do trabalho (quanto menor, melhor).',     'Custo',     2, v_portfolio, u_po, true, agora),
      (c_risco, 'Risco',                   'Incerteza técnica e de negócio (quanto menor, melhor).', 'Custo', 1, v_portfolio, u_po, true, agora);

    -- ---------------------------------------------------------------- projetos
    -- CreatedDate escalonado para o desempate do ranking ficar estável.
    INSERT INTO "Projects" ("Id","PortfolioId","Name","Description","StrategicCategory","Status","Priority","ApprovedBudget",
                            "StartDate","EndDate","JiraProjectKey","OwnerId","EvaluationStatus","CompletedAt","Active","CreatedDate")
    SELECT x.id, v_portfolio, x.nome, x.descr, x.cat, x.st, x.prio, x.orc, hoje + x.ini, hoje + x.fim, x.jira, u_gp,
           'NaoAvaliado', NULL, true, agora - (x.ordem || ' minutes')::interval
      FROM (VALUES
        (p_app,      'Super App do Cliente',        'App único para conta, pagamentos e atendimento.',          'Transform','EmAndamento','Critica', 500000, -150, 210, 'SAPP', 8),
        (p_openfin,  'Open Finance',                'Integração com o ecossistema de Open Finance.',             'Grow',     'EmAndamento','Alta',    350000, -120, 180, 'OPFI', 7),
        (p_ia,       'Atendimento com IA',          'Assistente virtual com IA generativa no app e no site.',    'Grow',     'EmRisco',    'Alta',    180000, -100,  60, 'ATIA', 6),
        (p_nuvem,    'Migração do Core para Nuvem', 'Saída do data center próprio para nuvem pública.',          'Transform','EmAndamento','Alta',    600000, -240, 120, 'CORE', 5),
        (p_erp,      'Modernização do ERP',         'Atualização de versão e fim das customizações legadas.',    'Run',      'EmAndamento','Media',   220000, -200,  90, NULL,   4),
        (p_bi,       'Painel de BI Executivo',      'Indicadores comerciais e financeiros em tempo real.',       'Grow',     'Planejado',  'Media',   120000,   15, 200, NULL,   3),
        (p_lgpd,     'LGPD e Segurança de Dados',   'Mascaramento, gestão de consentimento e trilhas de auditoria.','Run',  'EmAndamento','Critica', 160000,  -90, 150, 'LGPD', 2),
        (p_parceiro, 'Portal do Parceiro',          'Portal de autoatendimento para parceiros comerciais.',      'Grow',     'Rascunho',   'Baixa',    90000,   45, 260, NULL,   1)
      ) AS x(id, nome, descr, cat, st, prio, orc, ini, fim, jira, ordem);

    -- ---------------------------------------------------------------- notas (1 a 5)
    -- Ordem: valor, OKR, esforço, risco. O Portal do Parceiro fica com avaliação incompleta.
    FOR r IN SELECT * FROM (VALUES
        (p_app,     5,5,4,3), (p_openfin, 4,5,3,3), (p_ia,   4,4,2,4), (p_nuvem, 4,4,5,4),
        (p_erp,     3,2,3,2), (p_bi,      3,3,2,2), (p_lgpd, 4,3,2,2)) AS n(pid, v, o, e, rk)
    LOOP
        INSERT INTO "ProjectEvaluation" ("Id","ProjectId","PriorityCriteriaId","UserId","Score","EvaluatedAt","Active","CreatedDate") VALUES
          (gen_random_uuid(), r.pid, c_valor, u_po, r.v,  agora, true, agora),
          (gen_random_uuid(), r.pid, c_okr,   u_po, r.o,  agora, true, agora),
          (gen_random_uuid(), r.pid, c_esf,   u_po, r.e,  agora, true, agora),
          (gen_random_uuid(), r.pid, c_risco, u_po, r.rk, agora, true, agora);
    END LOOP;
    INSERT INTO "ProjectEvaluation" ("Id","ProjectId","PriorityCriteriaId","UserId","Score","EvaluatedAt","Active","CreatedDate") VALUES
      (gen_random_uuid(), p_parceiro, c_valor, u_po, 4, agora, true, agora),
      (gen_random_uuid(), p_parceiro, c_okr,   u_po, 3, agora, true, agora);

    -- Score (F1), mesma fórmula do PrioritizationCalculator:
    -- média ponderada (custo usa 6 - nota) -> (média - 1) / 4 × 100, 2 casas.
    UPDATE "Projects" p SET "CurrentScore" = s.score, "EvaluationStatus" = 'Priorizado', "LastPrioritizationDate" = agora
      FROM (SELECT e."ProjectId",
                   round(((sum(c."ValueWeight" * CASE WHEN c."Type" = 'Custo' THEN 6 - e."Score" ELSE e."Score" END)
                           / sum(c."ValueWeight")) - 1) / 4 * 100, 2) AS score
              FROM "ProjectEvaluation" e JOIN "PriorityCriteria" c ON c."Id" = e."PriorityCriteriaId"
             WHERE c."PortfolioId" = v_portfolio
             GROUP BY e."ProjectId"
            HAVING count(*) = (SELECT count(*) FROM "PriorityCriteria" WHERE "PortfolioId" = v_portfolio)) s
     WHERE p."Id" = s."ProjectId";
    UPDATE "Projects" SET "EvaluationStatus" = 'Avaliando' WHERE "Id" = p_parceiro;

    -- Ranking (F2): score desc, prioridade desc, data de criação asc.
    UPDATE "Projects" p SET "RankingPosition" = k.pos
      FROM (SELECT "Id", row_number() OVER (ORDER BY "CurrentScore" DESC,
                     CASE "Priority" WHEN 'Critica' THEN 3 WHEN 'Alta' THEN 2 WHEN 'Media' THEN 1 ELSE 0 END DESC,
                     "CreatedDate") AS pos
              FROM "Projects" WHERE "PortfolioId" = v_portfolio AND "CurrentScore" IS NOT NULL) k
     WHERE p."Id" = k."Id";

    -- ---------------------------------------------------------------- OKRs
    INSERT INTO "Objectives" ("Id","Title","Description","StartDate","EndDate","Active","CreatedDate") VALUES
      (o_cliente, 'Encantar o cliente nos canais digitais', 'Experiência digital como principal canal de relacionamento.',
       make_date(extract(year FROM hoje)::int, 1, 1), make_date(extract(year FROM hoje)::int + 1, 12, 31), true, agora),
      (o_receita, 'Dobrar a receita digital', 'Novos produtos e parcerias via canais digitais.',
       make_date(extract(year FROM hoje)::int, 1, 1), make_date(extract(year FROM hoje)::int + 1, 12, 31), true, agora),
      (o_seguro,  'Operar com segurança e eficiência', 'Conformidade, disponibilidade e custo da operação.',
       make_date(extract(year FROM hoje)::int, 1, 1), make_date(extract(year FROM hoje)::int + 1, 12, 31), true, agora);
    INSERT INTO "KeyResults" ("Id","ObjectiveId","Title","TargetValue","CurrentValue","Active","CreatedDate") VALUES
      (gen_random_uuid(), o_cliente, 'NPS dos canais digitais',                       75,   58, true, agora),
      (gen_random_uuid(), o_cliente, 'Atendimentos resolvidos sem humano (%)',        60,   32, true, agora),
      (gen_random_uuid(), o_receita, 'Receita digital anual (R$ mi)',                 40,   23, true, agora),
      (gen_random_uuid(), o_receita, 'Parceiros integrados via API',                  25,    9, true, agora),
      (gen_random_uuid(), o_seguro,  'Disponibilidade dos sistemas críticos (%)',     99.9, 99.4, true, agora),
      (gen_random_uuid(), o_seguro,  'Redução do custo de infraestrutura (%)',        20,    8, true, agora);
    INSERT INTO "PortfolioObjectives" ("PortfolioId","ObjectiveId","CreatedDate") VALUES
      (v_portfolio, o_cliente, agora), (v_portfolio, o_receita, agora), (v_portfolio, o_seguro, agora);
    -- O Portal do Parceiro fica sem OKR (bom para mostrar o indicador de alinhamento).
    INSERT INTO "ProjectObjectives" ("ProjectId","ObjectiveId","Active","CreatedDate") VALUES
      (p_app, o_cliente, true, agora), (p_app, o_receita, true, agora),
      (p_openfin, o_receita, true, agora),
      (p_ia, o_cliente, true, agora),
      (p_nuvem, o_seguro, true, agora),
      (p_erp, o_seguro, true, agora),
      (p_bi, o_receita, true, agora),
      (p_lgpd, o_seguro, true, agora);

    -- ---------------------------------------------------------------- orçamento (Burn Rate)
    -- Um lançamento por mês desde o início. O "Atendimento com IA" estoura o orçamento.
    FOR r IN SELECT * FROM (VALUES
        (p_app, 5, 52000), (p_openfin, 4, 38000), (p_ia, 3, 68000), (p_nuvem, 8, 41000),
        (p_erp, 6, 22000), (p_lgpd, 3, 15000), (p_bi, 1, 4000)) AS b(pid, meses, valor)
    LOOP
        FOR m IN 1..r.meses LOOP
            INSERT INTO "BudgetExpenses" ("Id","ProjectId","Amount","Category","Description","Date","Active","CreatedDate")
            VALUES (gen_random_uuid(), r.pid, r.valor, CASE WHEN m % 2 = 1 THEN 'Custo' ELSE 'Despesa' END,
                    CASE WHEN m % 2 = 1 THEN 'Serviços e licenças' ELSE 'Infraestrutura em nuvem' END,
                    hoje - (m * 30 - 10), true, agora);
        END LOOP;
    END LOOP;

    -- ---------------------------------------------------------------- business case (VPL) e retornos
    -- O Painel de BI tem VPL negativo de propósito (contraste na demo).
    FOR r IN SELECT * FROM (VALUES
        (p_app, 500000, 42000, 24), (p_openfin, 350000, 30000, 18), (p_ia, 180000, 16000, 18),
        (p_nuvem, 600000, 35000, 24), (p_bi, 120000, 5000, 12)) AS c(pid, inv, fluxo, meses)
    LOOP
        bc := gen_random_uuid();
        INSERT INTO "BusinessCases" ("Id","ProjectId","InitialInvestment","AnnualDiscountRate","Active","CreatedDate")
        VALUES (bc, r.pid, r.inv, 12, true, agora);
        FOR m IN 1..r.meses LOOP
            INSERT INTO "CashFlowForecasts" ("Id","BusinessCaseId","Month","Value") VALUES (gen_random_uuid(), bc, m, r.fluxo);
        END LOOP;
    END LOOP;
    FOR r IN SELECT * FROM (VALUES (p_app, 36000), (p_openfin, 27000), (p_nuvem, 30000)) AS rr(pid, valor) LOOP
        FOR m IN 1..3 LOOP
            INSERT INTO "RealizedReturns" ("Id","ProjectId","Date","Value","Description","Active","CreatedDate")
            VALUES (gen_random_uuid(), r.pid, hoje - (m * 30), r.valor, 'Retorno realizado do mês', true, agora);
        END LOOP;
    END LOOP;

    -- ---------------------------------------------------------------- dependências
    INSERT INTO "ProjectDependencies" ("Id","PortfolioId","ProjectId","DependsOnProjectId","UserId","Reason","Active","CreatedDate") VALUES
      (gen_random_uuid(), v_portfolio, p_openfin,  p_app,     u_gp, 'O consentimento do Open Finance é feito dentro do Super App.', true, agora),
      (gen_random_uuid(), v_portfolio, p_ia,       p_app,     u_gp, 'O assistente roda no Super App.',                             true, agora),
      (gen_random_uuid(), v_portfolio, p_bi,       p_nuvem,   u_gp, 'O BI lê do data lake que nasce com a migração.',              true, agora),
      (gen_random_uuid(), v_portfolio, p_parceiro, p_openfin, u_gp, 'Parceiros consomem as APIs do Open Finance.',                 true, agora);

    -- ---------------------------------------------------------------- equipes (capacidade)
    INSERT INTO "Teams" ("Id","Name","InviteCode","PortfolioId","OwnerUserId","Active","CreatedDate") VALUES
      (t_canais, 'Squad Canais Digitais', 'DEMOCAN7', v_portfolio, u_tl, true, agora),
      (t_plat,   'Squad Plataforma',      'DEMOPLT7', v_portfolio, u_tl, true, agora);
    INSERT INTO "TeamUsers" ("Id","TeamId","Name","Email","HourlyCost","MonthlyCapacityHours","UserId","Active","CreatedDate") VALUES
      (gen_random_uuid(), t_canais, 'Marina Alves',   'marina.alves@prumo.demo',   135, 160, NULL, true, agora),
      (gen_random_uuid(), t_canais, 'Rafael Costa',   'rafael.costa@prumo.demo',   120, 160, NULL, true, agora),
      (gen_random_uuid(), t_canais, 'Juliana Prado',  'juliana.prado@prumo.demo',  110, 120, NULL, true, agora),
      (gen_random_uuid(), t_plat,   'Eduardo Ramos',  'eduardo.ramos@prumo.demo',  150, 160, NULL, true, agora),
      (gen_random_uuid(), t_plat,   'Fernanda Melo',  'fernanda.melo@prumo.demo',  125, 160, NULL, true, agora),
      (gen_random_uuid(), t_plat,   'Lucas Moreira',  'lucas.moreira@prumo.demo',   95, 160, NULL, true, agora);

    -- ---------------------------------------------------------------- issues e worklogs (dados "do Jira")
    -- Mesmo padrão do SeedValidacao: 5 concluídas + 3 abertas por projeto.
    -- Ocupação (demanda aberta / capacidade): Squad Canais Digitais ≈ 130% (sobrecarregada),
    -- Squad Plataforma ≈ 80% (adequada);
    -- o "Atendimento com IA" tem as abertas atrasadas.
    FOR r IN SELECT * FROM (VALUES
        (p_app,     'SAPP', 'canais', 80, false),
        (p_openfin, 'OPFI', 'canais', 64, false),
        (p_ia,      'ATIA', 'canais', 64, true),
        (p_nuvem,   'CORE', 'plat',   60, false),
        (p_lgpd,    'LGPD', 'plat',   48, false),
        (p_erp,     'ERPM', 'plat',   40, false)) AS i(pid, chave, squad, estim, atrasadas)
    LOOP
        FOR m IN 1..8 LOOP
            seq := seq + 1;
            iss := gen_random_uuid();
            INSERT INTO "Issues" ("Id","ProjectId","ExternalId","Source","Title","Type","Status","Done","CreatedAt","CompletedAt",
                                  "DueDate","AssigneeEmail","EstimateHours","SpentHours","UpdatedAt")
            SELECT iss, r.pid, 'DEMO-' || r.chave || '-' || m, 'Seed',
                   (ARRAY['Cadastro e login','Tela inicial','Integração com a API','Notificações push','Correção de falha no fluxo',
                          'Relatório de uso','Ajustes de performance','Correção de falha na sincronização'])[m] || ' — ' || r.chave,
                   (ARRAY['Story','Story','Task','Feature','Bug','Task','Story','Bug'])[m],
                   CASE WHEN m <= 5 THEN 'Concluído' ELSE 'Em andamento' END,
                   m <= 5,
                   agora - ((90 - m * 10) || ' days')::interval,
                   CASE WHEN m <= 5 THEN agora - ((60 - m * 10) || ' days')::interval END,
                   CASE WHEN m <= 5 THEN hoje - (55 - m * 10)
                        WHEN r.atrasadas THEN hoje - (m - 4)
                        ELSE hoje + (m * 5) END,
                   (SELECT "Email" FROM "TeamUsers" WHERE "TeamId" = CASE r.squad WHEN 'canais' THEN t_canais ELSE t_plat END
                     ORDER BY "Email" OFFSET (m % 3) LIMIT 1),
                   CASE WHEN m <= 5 THEN 16 ELSE r.estim END,
                   CASE WHEN m <= 5 THEN 14 ELSE 6 END,
                   agora;
            -- Worklogs: concluídas no passado; abertas no mês corrente (entram na capacidade).
            INSERT INTO "Worklogs" ("Id","IssueId","ExternalId","AuthorEmail","Hours","Date")
            SELECT gen_random_uuid(), iss, 'DEMO-W-' || seq || '-' || w, i."AssigneeEmail",
                   CASE WHEN m <= 5 THEN 7 ELSE 3 END,
                   CASE WHEN m <= 5 THEN hoje - (60 - m * 10) - w
                        ELSE greatest(ini_mes, hoje - w) END
              FROM "Issues" i, generate_series(0, 1) w
             WHERE i."Id" = iss;
        END LOOP;
    END LOOP;

    RAISE NOTICE 'Mock da demo criado: portfólio "Estratégia Digital 2027" (%).', v_portfolio;
END $$;

COMMIT;

-- Conferência rápida do ranking
SELECT "RankingPosition" AS pos, "Name" AS projeto, "CurrentScore" AS score, "Priority" AS prioridade, "EvaluationStatus" AS avaliacao
  FROM "Projects" WHERE "PortfolioId" = 'de300000-0000-4000-8000-000000000001'
 ORDER BY "RankingPosition" NULLS LAST, "Name";
