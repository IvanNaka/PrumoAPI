-- =====================================================================================
-- Prumo — carga de dados de DEMONSTRAÇÃO (PostgreSQL)
--
-- Objetivo: popular o banco a partir de um único usuário já cadastrado (o seu login Google)
-- para que TODAS as páginas da aplicação tenham conteúdo na apresentação:
--   Portfólios (visão geral, membros, critérios, priorização, dashboard, projetos,
--   dependências, relatórios), Projeto (detalhe, orçamento, lançamentos, business case,
--   retornos, indicadores), Equipes / Capacidade, OKRs, Notificações, Integração Jira e
--   Admin > Usuários.
--
-- Como usar:
--   1. (Opcional) Se houver mais de um usuário, troque o e-mail abaixo em "demo_ctx".
--      Com o valor NULL o script usa o usuário cadastrado mais antigo.
--   2. Rode o arquivo inteiro de uma vez (psql, DBeaver, pgAdmin, Azure Data Studio):
--        psql "<connection string>" -f 01_seed_demo.sql
--   3. Faça LOGOUT e LOGIN de novo na aplicação: os perfis vão no JWT, então o perfil
--      Administrador adicionado aqui só vale após um novo login.
--
-- O script é idempotente: remove a carga anterior (02_remover_demo.sql) antes de inserir.
-- Todos os IDs de demonstração começam com "de" para facilitar a remoção.
-- As datas são relativas a CURRENT_DATE, então os indicadores ficam "atuais" em qualquer dia.
-- =====================================================================================

BEGIN;

-- -------------------------------------------------------------------------------------
-- 0. Contexto: quem é o usuário da apresentação
-- -------------------------------------------------------------------------------------
DROP TABLE IF EXISTS demo_ctx;
CREATE TEMP TABLE demo_ctx AS
SELECT u."Id" AS me, u."Email" AS my_email, u."Name" AS my_name
FROM "Users" u
WHERE u."Email" = COALESCE(NULL::text /* ex.: 'seu.email@gmail.com' */, u."Email")
  AND u."Id"::text NOT LIKE 'de%'
ORDER BY u."CreatedDate"
LIMIT 1;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM demo_ctx) THEN
        RAISE EXCEPTION 'Nenhum usuário encontrado em "Users". Faça login uma vez ou cadastre o usuário antes de rodar a carga.';
    END IF;
END $$;

-- -------------------------------------------------------------------------------------
-- 1. Limpeza da carga anterior (mesma lógica de 02_remover_demo.sql)
-- -------------------------------------------------------------------------------------
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

-- -------------------------------------------------------------------------------------
-- 2. Usuários: você vira Administrador (vê tudo) + 4 colegas fictícios
-- -------------------------------------------------------------------------------------
UPDATE "Users" SET "IsActive" = true, "Active" = true WHERE "Id" = (SELECT me FROM demo_ctx);

INSERT INTO "UserRoles" ("UserId", "Role")
SELECT me, 'Administrador' FROM demo_ctx
ON CONFLICT DO NOTHING;

INSERT INTO "Users" ("Id", "Name", "Email", "IsActive", "Active", "CreatedDate", "UpdatedDate") VALUES
('de100000-0000-4000-8000-000000000001', 'Ana Souza',    'ana.souza@prumo.demo',    true,  true, now() - interval '200 days', NULL),
('de100000-0000-4000-8000-000000000002', 'Bruno Lima',   'bruno.lima@prumo.demo',   true,  true, now() - interval '190 days', NULL),
('de100000-0000-4000-8000-000000000003', 'Carla Mendes', 'carla.mendes@prumo.demo', true,  true, now() - interval '180 days', NULL),
('de100000-0000-4000-8000-000000000004', 'Diego Rocha',  'diego.rocha@prumo.demo',  true,  true, now() - interval '170 days', NULL),
('de100000-0000-4000-8000-000000000005', 'Elisa Prado',  'elisa.prado@prumo.demo',  false, true, now() - interval '160 days', now() - interval '10 days');

INSERT INTO "UserRoles" ("UserId", "Role") VALUES
('de100000-0000-4000-8000-000000000001', 'GerenteProjeto'),
('de100000-0000-4000-8000-000000000002', 'TechLead'),
('de100000-0000-4000-8000-000000000003', 'ProductOwner'),
('de100000-0000-4000-8000-000000000004', 'Desenvolvedor'),
('de100000-0000-4000-8000-000000000004', 'QA'),
('de100000-0000-4000-8000-000000000005', 'Diretoria');

-- -------------------------------------------------------------------------------------
-- 3. Portfólios (você é o responsável) + membros
-- -------------------------------------------------------------------------------------
INSERT INTO "Portfolios" ("Id", "Name", "Description", "Goal", "OwnerId", "Status", "Active", "CreatedDate", "UpdatedDate")
SELECT v.id::uuid, v.name, v.descr, v.goal, c.me, v.status, true, now() - v.age, now() - interval '2 days'
FROM demo_ctx c, (VALUES
  ('de200000-0000-4000-8000-000000000001', 'Transformação Digital',
   'Iniciativas de digitalização dos canais de atendimento e modernização da plataforma.',
   'Aumentar em 30% a receita digital e reduzir em 20% o custo de atendimento até o fim do ano.',
   'Monitoramento', interval '180 days'),
  ('de200000-0000-4000-8000-000000000002', 'Eficiência Operacional',
   'Automação de processos internos, ERP e logística.',
   'Reduzir em 15% o custo operacional do back-office.',
   'Priorizado', interval '120 days'),
  ('de200000-0000-4000-8000-000000000003', 'Inovação 2027',
   'Portfólio recém-criado para as apostas do próximo ciclo de planejamento.',
   'Validar 3 novas linhas de produto.',
   'Criado', interval '5 days')
) AS v(id, name, descr, goal, status, age);

INSERT INTO "PortfolioMembers" ("PortfolioId", "UserId", "CreatedDate") VALUES
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000001', now() - interval '170 days'),
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002', now() - interval '170 days'),
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000003', now() - interval '165 days'),
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000004', now() - interval '160 days'),
('de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001', now() - interval '110 days'),
('de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000003', now() - interval '110 days');

-- -------------------------------------------------------------------------------------
-- 4. Critérios de priorização (peso 0–10; Beneficio = maior melhor, Custo = menor melhor)
-- -------------------------------------------------------------------------------------
INSERT INTO "PriorityCriteria" ("Id", "PortfolioId", "UserId", "Name", "Description", "Type", "ValueWeight", "Active", "CreatedDate")
SELECT v.id::uuid, v.pid::uuid, c.me, v.name, v.descr, v.tipo, v.peso, true, now() - interval '160 days'
FROM demo_ctx c, (VALUES
  ('de300000-0000-4000-8000-000000000011', 'de200000-0000-4000-8000-000000000001', 'Valor estratégico',    'Alinhamento com os objetivos da empresa.',        'Beneficio', 3.00),
  ('de300000-0000-4000-8000-000000000012', 'de200000-0000-4000-8000-000000000001', 'Retorno financeiro',   'Receita incremental ou redução de custo esperada.', 'Beneficio', 2.50),
  ('de300000-0000-4000-8000-000000000013', 'de200000-0000-4000-8000-000000000001', 'Urgência regulatória', 'Exigência legal ou de compliance.',                'Beneficio', 1.50),
  ('de300000-0000-4000-8000-000000000014', 'de200000-0000-4000-8000-000000000001', 'Risco técnico',        'Incerteza tecnológica e de execução.',             'Custo',     2.00),
  ('de300000-0000-4000-8000-000000000015', 'de200000-0000-4000-8000-000000000001', 'Esforço',              'Tamanho relativo da iniciativa.',                  'Custo',     1.00),
  ('de300000-0000-4000-8000-000000000021', 'de200000-0000-4000-8000-000000000002', 'Redução de custo',     'Economia anual estimada.',                         'Beneficio', 3.00),
  ('de300000-0000-4000-8000-000000000022', 'de200000-0000-4000-8000-000000000002', 'Satisfação do cliente','Impacto no NPS.',                                  'Beneficio', 2.00),
  ('de300000-0000-4000-8000-000000000023', 'de200000-0000-4000-8000-000000000002', 'Complexidade',         'Complexidade de implantação.',                     'Custo',     1.50)
) AS v(id, pid, name, descr, tipo, peso);

-- -------------------------------------------------------------------------------------
-- 5. Projetos
-- -------------------------------------------------------------------------------------
INSERT INTO "Projects" ("Id", "PortfolioId", "OwnerId", "Name", "Description", "StartDate", "EndDate", "CompletedAt",
                        "ApprovedBudget", "Status", "Priority", "StrategicCategory", "EvaluationStatus", "JiraProjectKey",
                        "CurrentScore", "RankingPosition", "LastPrioritizationDate", "Active", "CreatedDate", "UpdatedDate")
SELECT v.id::uuid, v.pid::uuid, COALESCE(v.owner::uuid, c.me), v.name, v.descr,
       CURRENT_DATE + v.ini, CURRENT_DATE + v.fim,
       CASE WHEN v.status = 'Concluido' THEN (CURRENT_DATE + v.fim)::timestamptz END,
       v.budget, v.status, v.prio, v.cat, v.aval, v.jira,
       NULL, NULL, NULL, true, now() - make_interval(days => GREATEST(10, 10 - v.ini)), now() - interval '3 days'
FROM demo_ctx c, (VALUES
  -- Transformação Digital
  ('de400000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', NULL,
   'Portal do Cliente 2.0', 'Novo portal self-service com autoatendimento e segunda via.', -120,   60, 450000.00, 'EmAndamento', 'Alta',    'Grow',      'Priorizado', 'PORT'),
  ('de400000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000001',
   'App Mobile',            'Aplicativo iOS/Android com login social e notificações push.', -90,    20, 380000.00, 'EmRisco',     'Critica', 'Transform', 'Priorizado', 'APP'),
  ('de400000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002',
   'Migração para Nuvem',   'Migração dos sistemas legados para a nuvem pública.',          -150,   90, 600000.00, 'EmAndamento', 'Alta',    'Run',       'Aprovado',   'CLOUD'),
  ('de400000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000003',
   'Plataforma de Dados',   'Data lake e painéis de BI para as áreas de negócio.',           15,   200, 300000.00, 'Planejado',   'Media',   'Transform', 'Priorizado', 'DATA'),
  ('de400000-0000-4000-8000-000000000005', 'de200000-0000-4000-8000-000000000001', NULL,
   'Chatbot de Atendimento','Assistente virtual no site e WhatsApp.',                       -240,  -20, 150000.00, 'Concluido',   'Media',   'Grow',      'Aprovado',   'BOT'),
  -- Eficiência Operacional
  ('de400000-0000-4000-8000-000000000006', 'de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001',
   'Automação Financeira (RPA)', 'Robôs para conciliação bancária e contas a pagar.',        -60,  120, 220000.00, 'EmAndamento', 'Alta',    'Run',       'Priorizado', 'RPA'),
  ('de400000-0000-4000-8000-000000000007', 'de200000-0000-4000-8000-000000000002', NULL,
   'Novo ERP',              'Substituição do ERP atual.',                                   -30,  300, 900000.00, 'Suspenso',    'Media',   'Run',       'Priorizado', 'ERP'),
  ('de400000-0000-4000-8000-000000000008', 'de200000-0000-4000-8000-000000000002', NULL,
   'Otimização Logística',  'Roteirização inteligente de entregas.',                         30,  180, 180000.00, 'Rascunho',    'Baixa',   'Grow',      'NaoAvaliado', NULL)
) AS v(id, pid, owner, name, descr, ini, fim, budget, status, prio, cat, aval, jira);

INSERT INTO "ProjectMembers" ("ProjectId", "UserId", "Active", "CreatedDate")
SELECT p."Id", m.uid::uuid, true, now() - interval '60 days'
FROM "Projects" p
CROSS JOIN (VALUES ('de100000-0000-4000-8000-000000000002'), ('de100000-0000-4000-8000-000000000004')) m(uid)
WHERE p."Id"::text LIKE 'de4%' AND p."PortfolioId" = 'de200000-0000-4000-8000-000000000001'
UNION ALL
SELECT p."Id", c.me, true, now() - interval '60 days'
FROM "Projects" p, demo_ctx c
WHERE p."Id"::text LIKE 'de4%' AND p."OwnerId" <> c.me;

-- -------------------------------------------------------------------------------------
-- 6. Avaliações (notas 1–5) + score/ranking calculados com a mesma fórmula da API (F1/F2)
-- -------------------------------------------------------------------------------------
INSERT INTO "ProjectEvaluation" ("Id", "ProjectId", "PriorityCriteriaId", "UserId", "Score", "EvaluatedAt", "Active", "CreatedDate")
SELECT ('de500000-0000-4000-8000-0000000' || right(v.p, 2) || right(v.c, 3))::uuid,
       ('de400000-0000-4000-8000-0000000000' || v.p)::uuid,
       ('de300000-0000-4000-8000-000000000' || v.c)::uuid,
       c.me, v.nota, now() - interval '15 days', true, now() - interval '15 days'
FROM demo_ctx c, (VALUES
  -- projeto, critério, nota
  ('01','011',5),('01','012',4),('01','013',3),('01','014',2),('01','015',3),
  ('02','011',5),('02','012',5),('02','013',2),('02','014',4),('02','015',4),
  ('03','011',4),('03','012',3),('03','013',5),('03','014',3),('03','015',5),
  ('04','011',4),('04','012',4),('04','013',1),('04','014',3),('04','015',3),
  ('05','011',3),('05','012',3),('05','013',2),('05','014',2),('05','015',2),
  ('06','021',5),('06','022',3),('06','023',2),
  ('07','021',4),('07','022',3),('07','023',5)
) AS v(p, c, nota);

WITH scores AS (
    SELECT e."ProjectId",
           round(((sum(pc."ValueWeight" * CASE WHEN pc."Type" = 'Custo' THEN 6 - e."Score" ELSE e."Score" END)
                   / sum(pc."ValueWeight")) - 1) / 4 * 100, 2) AS score
    FROM "ProjectEvaluation" e
    JOIN "PriorityCriteria" pc ON pc."Id" = e."PriorityCriteriaId"
    WHERE e."Id"::text LIKE 'de%'
    GROUP BY e."ProjectId"
), ranked AS (
    SELECT s."ProjectId", s.score,
           row_number() OVER (PARTITION BY p."PortfolioId"
                              ORDER BY s.score DESC,
                                       CASE p."Priority" WHEN 'Critica' THEN 4 WHEN 'Alta' THEN 3 WHEN 'Media' THEN 2 ELSE 1 END DESC,
                                       p."CreatedDate") AS pos
    FROM scores s JOIN "Projects" p ON p."Id" = s."ProjectId"
)
UPDATE "Projects" p
SET "CurrentScore" = r.score, "RankingPosition" = r.pos, "LastPrioritizationDate" = now() - interval '14 days'
FROM ranked r
WHERE p."Id" = r."ProjectId";

-- -------------------------------------------------------------------------------------
-- 7. Financeiro: orçamento, lançamentos mensais, business case (VPL) e retornos realizados
-- -------------------------------------------------------------------------------------
INSERT INTO "Budgets" ("Id", "ProjectId", "TotalAmount", "Currency", "Active", "CreatedDate")
SELECT ('de600000-0000-4000-8000-0000000000' || right(p."Id"::text, 2))::uuid, p."Id", p."ApprovedBudget", 'BRL', true, p."CreatedDate"
FROM "Projects" p WHERE p."Id"::text LIKE 'de4%';

-- Lançamentos: um "Custo" (horas/serviços) e uma "Despesa" (licenças/infra) por mês decorrido.
-- O App Mobile gasta acima do planejado (estouro de orçamento para o dashboard).
INSERT INTO "BudgetExpenses" ("Id", "ProjectId", "Date", "Description", "Amount", "Category", "Active", "CreatedDate")
SELECT gen_random_uuid(), p."Id", d.dt::date,
       CASE cat WHEN 'Custo' THEN 'Horas da equipe — ' ELSE 'Licenças e infraestrutura — ' END || to_char(d.dt, 'MM/YYYY'),
       round((p."ApprovedBudget" / GREATEST(1, (p."EndDate" - p."StartDate") / 30.0))
             * CASE cat WHEN 'Custo' THEN 0.75 ELSE 0.20 END
             * CASE WHEN p."Name" = 'App Mobile' THEN 1.55 ELSE 1 END
             * (0.9 + (extract(month FROM d.dt)::int % 3) * 0.08), 2),
       cat, true, d.dt
FROM "Projects" p
CROSS JOIN LATERAL generate_series(p."StartDate" + 10, LEAST(CURRENT_DATE, p."EndDate"), interval '1 month') AS d(dt)
CROSS JOIN (VALUES ('Custo'), ('Despesa')) AS c(cat)
WHERE p."Id"::text LIKE 'de4%' AND p."Status" NOT IN ('Rascunho', 'Planejado');

INSERT INTO "BusinessCases" ("Id", "ProjectId", "InitialInvestment", "AnnualDiscountRate", "Active", "CreatedDate")
SELECT ('de700000-0000-4000-8000-0000000000' || right(p."Id"::text, 2))::uuid, p."Id", p."ApprovedBudget", 12.00, true, p."CreatedDate"
FROM "Projects" p
WHERE p."Id"::text LIKE 'de4%' AND p."Status" <> 'Rascunho';

-- Fluxo de caixa previsto: 24 meses de benefício, com rampa nos 3 primeiros meses.
INSERT INTO "CashFlowForecasts" ("Id", "BusinessCaseId", "Month", "Value")
SELECT gen_random_uuid(), bc."Id", m,
       round(bc."InitialInvestment" / 14.0 * LEAST(1, m / 3.0), 2)
FROM "BusinessCases" bc CROSS JOIN generate_series(1, 24) AS m
WHERE bc."Id"::text LIKE 'de7%';

-- Retornos já realizados (projetos concluídos ou com entregas parciais).
INSERT INTO "RealizedReturns" ("Id", "ProjectId", "Date", "Description", "Value", "Active", "CreatedDate")
SELECT gen_random_uuid(), v.pid::uuid, CURRENT_DATE - v.dias, v.descr, v.valor, true, now() - make_interval(days => v.dias)
FROM (VALUES
  ('de400000-0000-4000-8000-000000000005', 15, 'Redução de chamados no call center',   14000.00),
  ('de400000-0000-4000-8000-000000000005', 45, 'Redução de chamados no call center',   12500.00),
  ('de400000-0000-4000-8000-000000000005', 75, 'Redução de chamados no call center',   11000.00),
  ('de400000-0000-4000-8000-000000000001', 20, 'Adesão à segunda via digital',          9000.00),
  ('de400000-0000-4000-8000-000000000001', 50, 'Adesão à segunda via digital',          6000.00),
  ('de400000-0000-4000-8000-000000000003', 30, 'Desligamento de servidores on-premise', 18000.00),
  ('de400000-0000-4000-8000-000000000006', 10, 'Horas economizadas em conciliação',     7500.00)
) AS v(pid, dias, descr, valor);

-- -------------------------------------------------------------------------------------
-- 8. OKRs: objetivos, key results e vínculos com portfólios e projetos
-- -------------------------------------------------------------------------------------
INSERT INTO "Objectives" ("Id", "Title", "Description", "StartDate", "EndDate", "Active", "CreatedDate") VALUES
('de800000-0000-4000-8000-000000000001', 'Crescer a receita digital', 'Aumentar a participação dos canais digitais na receita.',
  date_trunc('year', CURRENT_DATE)::date, (date_trunc('year', CURRENT_DATE) + interval '1 year - 1 day')::date, true, now() - interval '170 days'),
('de800000-0000-4000-8000-000000000002', 'Excelência no atendimento', 'Melhorar a experiência e reduzir o custo de atendimento.',
  date_trunc('year', CURRENT_DATE)::date, (date_trunc('year', CURRENT_DATE) + interval '1 year - 1 day')::date, true, now() - interval '170 days'),
('de800000-0000-4000-8000-000000000003', 'Operação enxuta', 'Reduzir custos operacionais com automação.',
  date_trunc('year', CURRENT_DATE)::date, (date_trunc('year', CURRENT_DATE) + interval '1 year - 1 day')::date, true, now() - interval '110 days');

INSERT INTO "KeyResults" ("Id", "ObjectiveId", "Title", "TargetValue", "CurrentValue", "Active", "CreatedDate") VALUES
('de900000-0000-4000-8000-000000000011', 'de800000-0000-4000-8000-000000000001', 'Receita digital (R$ mil/mês)',        500, 340, true, now() - interval '170 days'),
('de900000-0000-4000-8000-000000000012', 'de800000-0000-4000-8000-000000000001', 'Usuários ativos no app (mil)',         50,  18, true, now() - interval '170 days'),
('de900000-0000-4000-8000-000000000021', 'de800000-0000-4000-8000-000000000002', 'NPS',                                  70,  61, true, now() - interval '170 days'),
('de900000-0000-4000-8000-000000000022', 'de800000-0000-4000-8000-000000000002', '% de atendimentos resolvidos no bot',  60,  52, true, now() - interval '170 days'),
('de900000-0000-4000-8000-000000000031', 'de800000-0000-4000-8000-000000000003', 'Processos automatizados',              20,   7, true, now() - interval '110 days'),
('de900000-0000-4000-8000-000000000032', 'de800000-0000-4000-8000-000000000003', 'Redução do custo de back-office (%)',  15,   4, true, now() - interval '110 days');

INSERT INTO "PortfolioObjectives" ("PortfolioId", "ObjectiveId", "CreatedDate") VALUES
('de200000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000001', now() - interval '165 days'),
('de200000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000002', now() - interval '165 days'),
('de200000-0000-4000-8000-000000000002', 'de800000-0000-4000-8000-000000000003', now() - interval '105 days');

-- A Plataforma de Dados fica sem objetivo de propósito (aparece como "desalinhada" nos indicadores).
INSERT INTO "ProjectObjectives" ("ProjectId", "ObjectiveId", "Active", "CreatedDate") VALUES
('de400000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000001', true, now() - interval '100 days'),
('de400000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000002', true, now() - interval '100 days'),
('de400000-0000-4000-8000-000000000002', 'de800000-0000-4000-8000-000000000001', true, now() - interval '90 days'),
('de400000-0000-4000-8000-000000000003', 'de800000-0000-4000-8000-000000000002', true, now() - interval '140 days'),
('de400000-0000-4000-8000-000000000005', 'de800000-0000-4000-8000-000000000002', true, now() - interval '230 days'),
('de400000-0000-4000-8000-000000000006', 'de800000-0000-4000-8000-000000000003', true, now() - interval '55 days'),
('de400000-0000-4000-8000-000000000007', 'de800000-0000-4000-8000-000000000003', true, now() - interval '25 days');

-- -------------------------------------------------------------------------------------
-- 9. Dependências entre projetos
-- -------------------------------------------------------------------------------------
INSERT INTO "ProjectDependencies" ("Id", "PortfolioId", "ProjectId", "DependsOnProjectId", "UserId", "Reason", "Active", "CreatedDate")
SELECT v.id::uuid, v.pf::uuid, v.p::uuid, v.d::uuid, c.me, v.reason, true, now() - interval '80 days'
FROM demo_ctx c, (VALUES
  ('dea00000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000001', 'O app consome as APIs do novo portal.'),
  ('dea00000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000004', 'de400000-0000-4000-8000-000000000003', 'O data lake será provisionado na nuvem.'),
  ('dea00000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000003', 'O portal será hospedado na nova infraestrutura.'),
  ('dea00000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000006', 'de400000-0000-4000-8000-000000000007', 'Robôs dependem das APIs do novo ERP.')
) AS v(id, pf, p, d, reason);

-- -------------------------------------------------------------------------------------
-- 10. Roadmap
-- -------------------------------------------------------------------------------------
INSERT INTO "RoadmapItems" ("Id", "PortfolioId", "ProjectId", "Title", "Description", "StartDate", "EndDate", "Status", "Order", "Active", "CreatedDate")
SELECT v.id::uuid, v.pf::uuid, v.p::uuid, v.title, v.descr,
       (CURRENT_DATE + v.ini)::timestamptz, (CURRENT_DATE + v.fim)::timestamptz, v.status, v.ord, true, now() - interval '100 days'
FROM (VALUES
  ('deb00000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000005', 'Chatbot em produção',        'Go-live no site e WhatsApp.',        -240, -20, 'Concluido',   1),
  ('deb00000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000003', 'Onda 1 da migração',         'Sistemas não críticos.',             -150, -30, 'Concluido',   2),
  ('deb00000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000002', 'MVP do App Mobile',          'Login, extrato e notificações.',      -90,  -5, 'Atrasado',    3),
  ('deb00000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000001', 'Portal — autoatendimento',   'Segunda via e alteração cadastral.',  -60,  60, 'EmAndamento', 4),
  ('deb00000-0000-4000-8000-000000000005', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000003', 'Onda 2 da migração',         'Sistemas críticos.',                  -30,  90, 'EmAndamento', 5),
  ('deb00000-0000-4000-8000-000000000006', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000004', 'Data lake e primeiros painéis', NULL,                                 15, 200, 'Planejado',   6),
  ('deb00000-0000-4000-8000-000000000007', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000006', 'Robôs de conciliação',       NULL,                                 -60, 120, 'EmAndamento', 1),
  ('deb00000-0000-4000-8000-000000000008', 'de200000-0000-4000-8000-000000000002', NULL,                                   'Revisão de processos',       'Mapeamento AS-IS/TO-BE.',              20,  80, 'Planejado',   2)
) AS v(id, pf, p, title, descr, ini, fim, status, ord);

-- -------------------------------------------------------------------------------------
-- 11. Equipes e membros (e-mails batem com os responsáveis das issues → capacidade/alocação)
-- -------------------------------------------------------------------------------------
INSERT INTO "Teams" ("Id", "Name", "PortfolioId", "OwnerUserId", "Active", "CreatedDate")
SELECT v.id::uuid, v.name, v.pf::uuid, COALESCE(v.owner::uuid, c.me), true, now() - interval '150 days'
FROM demo_ctx c, (VALUES
  ('dec00000-0000-4000-8000-000000000001', 'Squad Canais Digitais', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002'),
  ('dec00000-0000-4000-8000-000000000002', 'Squad Plataforma',      'de200000-0000-4000-8000-000000000001', NULL),
  ('dec00000-0000-4000-8000-000000000003', 'Squad Back-office',     'de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001')
) AS v(id, name, pf, owner);

INSERT INTO "TeamUsers" ("Id", "TeamId", "UserId", "Name", "Email", "MonthlyCapacityHours", "HourlyCost", "Active", "CreatedDate")
SELECT gen_random_uuid(), v.team::uuid, v.uid::uuid, v.name, v.email, v.cap, v.custo, true, now() - interval '140 days'
FROM (VALUES
  ('dec00000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002', 'Bruno Lima',     'bruno.lima@prumo.demo',     120, 150.00),
  ('dec00000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000004', 'Diego Rocha',    'diego.rocha@prumo.demo',    160,  95.00),
  ('dec00000-0000-4000-8000-000000000001', NULL,                                   'Fernanda Alves', 'fernanda.alves@prumo.demo', 160,  90.00),
  ('dec00000-0000-4000-8000-000000000001', NULL,                                   'Gustavo Reis',   'gustavo.reis@prumo.demo',    80,  85.00),
  ('dec00000-0000-4000-8000-000000000002', NULL,                                   'Helena Castro',  'helena.castro@prumo.demo',  160, 130.00),
  ('dec00000-0000-4000-8000-000000000002', NULL,                                   'Igor Martins',   'igor.martins@prumo.demo',   160, 110.00),
  ('dec00000-0000-4000-8000-000000000003', NULL,                                   'Juliana Costa',  'juliana.costa@prumo.demo',  140, 100.00),
  ('dec00000-0000-4000-8000-000000000003', NULL,                                   'Lucas Pereira',  'lucas.pereira@prumo.demo',  160,  80.00)
) AS v(team, uid, name, email, cap, custo);

-- Você também entra na Squad Plataforma (aparece na capacidade com o seu e-mail).
INSERT INTO "TeamUsers" ("Id", "TeamId", "UserId", "Name", "Email", "MonthlyCapacityHours", "HourlyCost", "Active", "CreatedDate")
SELECT gen_random_uuid(), 'dec00000-0000-4000-8000-000000000002', c.me, c.my_name, c.my_email, 40, 180.00, true, now() - interval '140 days'
FROM demo_ctx c;

-- -------------------------------------------------------------------------------------
-- 12. Integração Jira (pausada, sem sincronizar de verdade) + histórico de sincronizações
--     IsActive = false evita que o job tente usar o token fictício. Na tela, a configuração
--     aparece preenchida; para conectar de verdade basta informar URL/e-mail/token reais.
-- -------------------------------------------------------------------------------------
-- Se você já tiver uma integração Jira configurada, ela é mantida (ON CONFLICT pelo índice único de Type).
INSERT INTO "Integrations" ("Id", "Type", "ApiUrl", "Email", "Token", "Status", "IsActive", "SyncIntervalMinutes",
                            "FailedAttempts", "LastSyncedAt", "NextAttemptAt", "Active", "CreatedDate", "UpdatedDate")
SELECT 'ded00000-0000-4000-8000-000000000001', 'Jira', 'https://prumo-demo.atlassian.net', c.my_email, 'demo-token-nao-valido',
       'Conectada', false, 60, 0, now() - interval '35 minutes', NULL, true, now() - interval '120 days', now() - interval '35 minutes'
FROM demo_ctx c
ON CONFLICT DO NOTHING;

INSERT INTO "IntegrationSyncLogs" ("Id", "IntegrationId", "StartedAt", "FinishedAt", "Success", "IssuesProcessed", "WorklogsProcessed", "ErrorMessage")
SELECT gen_random_uuid(), 'ded00000-0000-4000-8000-000000000001',
       now() - make_interval(hours => h) - interval '35 minutes',
       now() - make_interval(hours => h) - interval '34 minutes',
       h <> 5,
       CASE WHEN h = 5 THEN 0 ELSE 30 + h % 7 END,
       CASE WHEN h = 5 THEN 0 ELSE 80 + h % 11 END,
       CASE WHEN h = 5 THEN 'Timeout ao consultar a API do Jira (tentativa 1 de 3).' END
FROM generate_series(0, 9) AS h
WHERE EXISTS (SELECT 1 FROM "Integrations" WHERE "Id" = 'ded00000-0000-4000-8000-000000000001');

-- -------------------------------------------------------------------------------------
-- 13. Issues e worklogs "importados do Jira" (lead time, throughput, bugs, capacidade)
-- -------------------------------------------------------------------------------------
INSERT INTO "Issues" ("Id", "ProjectId", "ExternalId", "Source", "Title", "Type", "Status", "Done", "AssigneeEmail",
                      "EstimateHours", "SpentHours", "DueDate", "CreatedAt", "UpdatedAt", "CompletedAt")
SELECT ('dee00000-0000-4000-8000-' || lpad(p.n::text, 2, '0') || lpad(i::text, 10, '0'))::uuid,
       p.id,
       p.jira || '-' || (100 + i),
       'Jira',
       (ARRAY['Tela de login','Integração com gateway de pagamento','Ajuste de performance na listagem',
              'Erro ao gerar segunda via','Relatório de uso','Notificações por e-mail','Cadastro de endereço',
              'Falha intermitente no deploy','Exportação para Excel','Refatoração do módulo de autenticação',
              'Tela de histórico','Correção de layout no mobile'])[1 + (i + p.n) % 12],
       CASE WHEN i % 5 = 0 THEN 'Bug' WHEN i % 4 = 0 THEN 'Feature' WHEN i % 3 = 0 THEN 'Task' ELSE 'Story' END,
       CASE WHEN i <= 9 THEN 'Concluído' WHEN i <= 12 THEN 'Em andamento' WHEN i = 13 THEN 'Em revisão' ELSE 'A fazer' END,
       i <= 9,
       p.emails[1 + i % array_length(p.emails, 1)],
       8 + (i % 4) * 4,
       CASE WHEN i <= 9 THEN 8 + (i % 4) * 4 + (i % 3) * 2 WHEN i <= 13 THEN 4 + i % 5 ELSE 0 END,
       CURRENT_DATE + (i * 5 - 40),
       now() - make_interval(days => 100 - i * 5),
       now() - make_interval(days => GREATEST(1, 60 - i * 4)),
       CASE WHEN i <= 9 THEN now() - make_interval(days => GREATEST(1, 85 - i * 9)) END
FROM (VALUES
  (1, 'de400000-0000-4000-8000-000000000001'::uuid, 'PORT',  ARRAY['diego.rocha@prumo.demo','fernanda.alves@prumo.demo','bruno.lima@prumo.demo']),
  (2, 'de400000-0000-4000-8000-000000000002'::uuid, 'APP',   ARRAY['fernanda.alves@prumo.demo','gustavo.reis@prumo.demo','diego.rocha@prumo.demo']),
  (3, 'de400000-0000-4000-8000-000000000003'::uuid, 'CLOUD', ARRAY['helena.castro@prumo.demo','igor.martins@prumo.demo']),
  (6, 'de400000-0000-4000-8000-000000000006'::uuid, 'RPA',   ARRAY['juliana.costa@prumo.demo','lucas.pereira@prumo.demo'])
) AS p(n, id, jira, emails)
CROSS JOIN generate_series(1, 16) AS i;

-- Worklogs nos últimos ~75 dias (inclui o mês corrente → gráfico de capacidade).
INSERT INTO "Worklogs" ("Id", "IssueId", "ExternalId", "AuthorEmail", "Date", "Hours")
SELECT gen_random_uuid(), iss."Id", 'WL-' || iss."ExternalId" || '-' || d,
       iss."AssigneeEmail", CURRENT_DATE - (d * 6 + (right(iss."ExternalId", 1))::int % 5), 2 + (d % 4) * 1.5
FROM "Issues" iss
CROSS JOIN generate_series(0, 11) AS d
WHERE iss."Id"::text LIKE 'dee%' AND iss."SpentHours" > 0;

-- Você também registrou horas (para aparecer na sua capacidade).
INSERT INTO "Worklogs" ("Id", "IssueId", "ExternalId", "AuthorEmail", "Date", "Hours")
SELECT gen_random_uuid(), 'dee00000-0000-4000-8000-030000000010', 'WL-ME-' || d, c.my_email, CURRENT_DATE - d * 4, 3
FROM demo_ctx c CROSS JOIN generate_series(0, 7) AS d;

-- -------------------------------------------------------------------------------------
-- 14. Notificações para você (Notificações / sino)
-- -------------------------------------------------------------------------------------
INSERT INTO "Alerts" ("Id", "UserId", "Type", "Status", "Message", "EntityType", "EntityId", "SentAt", "ReadAt", "ArchivedAt", "Active", "CreatedDate")
SELECT v.id::uuid, c.me, v.tipo, v.status, v.msg, 'Project', v.ent::uuid,
       now() - make_interval(hours => v.h),
       CASE WHEN v.status = 'Lida' THEN now() - make_interval(hours => v.h - 1) END,
       CASE WHEN v.status = 'Arquivada' THEN now() - make_interval(hours => v.h - 2) END,
       true, now() - make_interval(hours => v.h)
FROM demo_ctx c, (VALUES
  ('def00000-0000-4000-8000-000000000001', 'EstouroOrcamento', 'Enviada',  'O projeto "App Mobile" consumiu mais de 90% do orçamento aprovado.',                   'de400000-0000-4000-8000-000000000002',   2),
  ('def00000-0000-4000-8000-000000000002', 'Atraso',           'Enviada',  'O marco "MVP do App Mobile" está atrasado.',                                         'de400000-0000-4000-8000-000000000002',   5),
  ('def00000-0000-4000-8000-000000000003', 'Risco',            'Enviada',  'O projeto "App Mobile" foi marcado como Em Risco.',                                  'de400000-0000-4000-8000-000000000002',  20),
  ('def00000-0000-4000-8000-000000000004', 'Conflito',         'Lida',     'Conflito de dependência: "Automação Financeira (RPA)" depende de "Novo ERP" (Suspenso).', 'de400000-0000-4000-8000-000000000006', 30),
  ('def00000-0000-4000-8000-000000000005', 'Desalinhamento',   'Lida',     'O projeto "Plataforma de Dados" não está vinculado a nenhum objetivo estratégico.',   'de400000-0000-4000-8000-000000000004',  48),
  ('def00000-0000-4000-8000-000000000006', 'Atraso',           'Arquivada','A onda 1 da migração terminou 5 dias após o previsto.',                               'de400000-0000-4000-8000-000000000003', 200)
) AS v(id, tipo, status, msg, ent, h);

-- -------------------------------------------------------------------------------------
-- 15. Histórico de relatórios gerados
-- -------------------------------------------------------------------------------------
INSERT INTO "Reports" ("Id", "PortfolioId", "Name", "Type", "Format", "GeneratedById", "GeneratedAt")
SELECT v.id::uuid, v.pf::uuid, v.name, v.tipo, v.fmt, c.me, now() - make_interval(days => v.d)
FROM demo_ctx c, (VALUES
  ('de0f0000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', 'portfolio-transformacao-digital.pdf',  'Portfolio', 'PDF',   7),
  ('de0f0000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'executivo-transformacao-digital.xlsx', 'Executivo', 'Excel', 2),
  ('de0f0000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000002', 'portfolio-eficiencia-operacional.pdf', 'Portfolio', 'PDF',   4)
) AS v(id, pf, name, tipo, fmt, d);

COMMIT;

-- Conferência rápida
SELECT 'Portfolios' AS tabela, count(*) FROM "Portfolios" WHERE "Id"::text LIKE 'de%'
UNION ALL SELECT 'Projects',        count(*) FROM "Projects"        WHERE "Id"::text LIKE 'de%'
UNION ALL SELECT 'BudgetExpenses',  count(*) FROM "BudgetExpenses"  b JOIN "Projects" p ON p."Id" = b."ProjectId" WHERE p."Id"::text LIKE 'de%'
UNION ALL SELECT 'Issues',          count(*) FROM "Issues"          WHERE "Id"::text LIKE 'de%'
UNION ALL SELECT 'Worklogs',        count(*) FROM "Worklogs" w JOIN "Issues" i ON i."Id" = w."IssueId" WHERE i."Id"::text LIKE 'de%'
UNION ALL SELECT 'Teams',           count(*) FROM "Teams"           WHERE "Id"::text LIKE 'de%'
UNION ALL SELECT 'Alerts',          count(*) FROM "Alerts"          WHERE "Id"::text LIKE 'de%';
