-- =====================================================================================
-- Prumo — carga de dados de DEMONSTRAÇÃO (PostgreSQL) — cenário POSITIVO
--
-- Objetivo: a partir de um único usuário já cadastrado (o seu login Google), popular o banco
-- para que TODAS as páginas tenham conteúdo, contando a história de um portfólio bem gerido:
--   * projetos no prazo e dentro do orçamento (saúde "Saudável", sem risco de estouro);
--   * todos os projetos alinhados a objetivos estratégicos, OKRs perto da meta;
--   * business cases com VPL positivo e retornos já realizados;
--   * equipes alocadas aos projetos com ocupação "Adequada" (70–100%);
--   * dependências sem conflito, roadmap em dia, sincronizações do Jira sem falhas.
--
-- Telas cobertas: Portfólios (visão geral, membros, critérios, priorização, dashboard,
-- projetos, dependências, relatórios), Projeto (detalhe, equipes alocadas, orçamento,
-- lançamentos, business case, retornos, indicadores), Equipes / Capacidade (com código de
-- convite), OKRs, Notificações, Integração Jira e Admin > Usuários.
--
-- Como usar:
--   1. (Opcional) Se houver mais de um usuário, troque o NULL em "demo_ctx" pelo seu e-mail.
--      Com NULL o script usa o usuário cadastrado há mais tempo.
--   2. Rode o arquivo inteiro de uma vez (psql, DBeaver, pgAdmin, Azure Data Studio):
--        psql "<connection string>" -f 01_seed_demo.sql
--   3. Saia e entre de novo na aplicação: os perfis vão no JWT, então o perfil
--      Administrador adicionado aqui só vale após um novo login (e evita a tela de boas-vindas).
--
-- Requer o banco atualizado até a migration AddProjectTeams (tabela "ProjectTeams" e
-- coluna "Teams"."InviteCode").
--
-- O script é idempotente: remove a carga anterior (mesma lógica de 02_remover_demo.sql)
-- antes de inserir. Todos os IDs de demonstração começam com "de".
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
        RAISE EXCEPTION 'Nenhum usuário encontrado em "Users". Faça login uma vez antes de rodar a carga.';
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
DELETE FROM "ProjectTeams"        WHERE "ProjectId"::text LIKE 'de%' OR "TeamId"::text LIKE 'de%';
DELETE FROM "Projects"            WHERE "Id"::text LIKE 'de%' OR "PortfolioId"::text LIKE 'de%' OR "OwnerId"::text LIKE 'de%';
DELETE FROM "PriorityCriteria"    WHERE "PortfolioId"::text LIKE 'de%';
DELETE FROM "Teams"               WHERE "Id"::text LIKE 'de%';
DELETE FROM "Portfolios"          WHERE "Id"::text LIKE 'de%' OR "OwnerId"::text LIKE 'de%';
DELETE FROM "Objectives"          WHERE "Id"::text LIKE 'de%';
DELETE FROM "Users"               WHERE "Id"::text LIKE 'de%';

-- -------------------------------------------------------------------------------------
-- 2. Usuários: você vira Administrador (vê tudo) + 5 colegas fictícios
-- -------------------------------------------------------------------------------------
UPDATE "Users" SET "IsActive" = true, "Active" = true WHERE "Id" = (SELECT me FROM demo_ctx);

INSERT INTO "UserRoles" ("UserId", "Role")
SELECT me, 'Administrador' FROM demo_ctx
ON CONFLICT DO NOTHING;

INSERT INTO "Users" ("Id", "Name", "Email", "IsActive", "Active", "CreatedDate", "UpdatedDate") VALUES
('de100000-0000-4000-8000-000000000001', 'Ana Souza',    'ana.souza@prumo.demo',    true, true, now() - interval '260 days', NULL),
('de100000-0000-4000-8000-000000000002', 'Bruno Lima',   'bruno.lima@prumo.demo',   true, true, now() - interval '250 days', NULL),
('de100000-0000-4000-8000-000000000003', 'Carla Mendes', 'carla.mendes@prumo.demo', true, true, now() - interval '240 days', NULL),
('de100000-0000-4000-8000-000000000004', 'Diego Rocha',  'diego.rocha@prumo.demo',  true, true, now() - interval '230 days', NULL),
('de100000-0000-4000-8000-000000000005', 'Elisa Prado',  'elisa.prado@prumo.demo',  true, true, now() - interval '220 days', NULL);

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
   'Digitalização dos canais de atendimento e modernização da plataforma tecnológica.',
   'Aumentar em 30% a receita digital e reduzir em 20% o custo de atendimento até o fim do ano.',
   'Monitoramento', interval '250 days'),
  ('de200000-0000-4000-8000-000000000002', 'Eficiência Operacional',
   'Automação de processos internos, ERP e logística.',
   'Reduzir em 15% o custo operacional do back-office.',
   'Monitoramento', interval '220 days'),
  ('de200000-0000-4000-8000-000000000003', 'Inovação 2027',
   'Apostas do próximo ciclo de planejamento, com critérios já definidos.',
   'Validar duas novas linhas de produto digitais.',
   'Configurado', interval '12 days')
) AS v(id, name, descr, goal, status, age);

INSERT INTO "PortfolioMembers" ("PortfolioId", "UserId", "CreatedDate") VALUES
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000001', now() - interval '240 days'),
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002', now() - interval '240 days'),
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000003', now() - interval '235 days'),
('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000004', now() - interval '230 days'),
('de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001', now() - interval '210 days'),
('de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000003', now() - interval '210 days'),
('de200000-0000-4000-8000-000000000003', 'de100000-0000-4000-8000-000000000003', now() - interval '10 days'),
('de200000-0000-4000-8000-000000000003', 'de100000-0000-4000-8000-000000000005', now() - interval '10 days');

-- -------------------------------------------------------------------------------------
-- 4. Critérios de priorização
--    Regra atual da API: a soma dos pesos de cada portfólio é exatamente 10.
--    Beneficio = quanto maior a nota, melhor; Custo = quanto menor, melhor.
-- -------------------------------------------------------------------------------------
INSERT INTO "PriorityCriteria" ("Id", "PortfolioId", "UserId", "Name", "Description", "Type", "ValueWeight", "Active", "CreatedDate")
SELECT v.id::uuid, v.pid::uuid, c.me, v.name, v.descr, v.tipo, v.peso, true, now() - interval '230 days'
FROM demo_ctx c, (VALUES
  -- Transformação Digital: 3 + 2,5 + 1,5 + 2 + 1 = 10
  ('de300000-0000-4000-8000-000000000011', 'de200000-0000-4000-8000-000000000001', 'Valor estratégico',      'Alinhamento com os objetivos da empresa.',           'Beneficio', 3.00),
  ('de300000-0000-4000-8000-000000000012', 'de200000-0000-4000-8000-000000000001', 'Retorno financeiro',     'Receita incremental ou redução de custo esperada.',  'Beneficio', 2.50),
  ('de300000-0000-4000-8000-000000000013', 'de200000-0000-4000-8000-000000000001', 'Experiência do cliente', 'Impacto na satisfação e no NPS.',                    'Beneficio', 1.50),
  ('de300000-0000-4000-8000-000000000014', 'de200000-0000-4000-8000-000000000001', 'Risco técnico',          'Incerteza tecnológica e de execução.',               'Custo',     2.00),
  ('de300000-0000-4000-8000-000000000015', 'de200000-0000-4000-8000-000000000001', 'Esforço',                'Tamanho relativo da iniciativa.',                    'Custo',     1.00),
  -- Eficiência Operacional: 4 + 3 + 3 = 10
  ('de300000-0000-4000-8000-000000000021', 'de200000-0000-4000-8000-000000000002', 'Redução de custo',       'Economia anual estimada.',                           'Beneficio', 4.00),
  ('de300000-0000-4000-8000-000000000022', 'de200000-0000-4000-8000-000000000002', 'Ganho de produtividade', 'Horas liberadas das equipes operacionais.',          'Beneficio', 3.00),
  ('de300000-0000-4000-8000-000000000023', 'de200000-0000-4000-8000-000000000002', 'Complexidade',           'Complexidade de implantação.',                       'Custo',     3.00),
  -- Inovação 2027: 4 + 3 + 3 = 10
  ('de300000-0000-4000-8000-000000000031', 'de200000-0000-4000-8000-000000000003', 'Potencial de mercado',   'Tamanho da oportunidade.',                           'Beneficio', 4.00),
  ('de300000-0000-4000-8000-000000000032', 'de200000-0000-4000-8000-000000000003', 'Grau de inovação',       'Diferenciação frente à concorrência.',               'Beneficio', 3.00),
  ('de300000-0000-4000-8000-000000000033', 'de200000-0000-4000-8000-000000000003', 'Incerteza',              'Risco de não validar a hipótese.',                   'Custo',     3.00)
) AS v(id, pid, name, descr, tipo, peso);

-- -------------------------------------------------------------------------------------
-- 5. Projetos — todos no prazo; os concluídos terminaram antes da data prevista
-- -------------------------------------------------------------------------------------
INSERT INTO "Projects" ("Id", "PortfolioId", "OwnerId", "Name", "Description", "StartDate", "EndDate", "CompletedAt",
                        "ApprovedBudget", "Status", "Priority", "StrategicCategory", "EvaluationStatus", "JiraProjectKey",
                        "CurrentScore", "RankingPosition", "LastPrioritizationDate", "Active", "CreatedDate", "UpdatedDate")
SELECT v.id::uuid, v.pid::uuid, COALESCE(v.owner::uuid, c.me), v.name, v.descr,
       CURRENT_DATE + v.ini, CURRENT_DATE + v.fim,
       CASE WHEN v.status = 'Concluido' THEN (CURRENT_DATE + v.fim - 6)::timestamptz + interval '17 hours' END,
       v.budget, v.status, v.prio, v.cat, v.aval, v.jira,
       NULL, NULL, NULL, true, now() - make_interval(days => GREATEST(10, 15 - v.ini)), now() - interval '1 day'
FROM demo_ctx c, (VALUES
  -- Transformação Digital
  ('de400000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', NULL,
   'Portal do Cliente 2.0',  'Portal self-service com segunda via, alteração cadastral e acompanhamento de pedidos.', -120,  60, 450000.00, 'EmAndamento', 'Alta',    'Grow',      'Aprovado',    'PORT'),
  ('de400000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000001',
   'App Mobile',             'Aplicativo iOS/Android com login social, extrato e notificações push.',               -90,   75, 380000.00, 'EmAndamento', 'Critica', 'Transform', 'Aprovado',    'APP'),
  ('de400000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002',
   'Migração para Nuvem',    'Migração dos sistemas legados para a nuvem pública.',                                 -150,  45, 600000.00, 'EmAndamento', 'Alta',    'Run',       'Aprovado',    'CLOUD'),
  ('de400000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000003',
   'Plataforma de Dados',    'Data lake e painéis de BI para as áreas de negócio.',                                   20, 200, 300000.00, 'Planejado',   'Media',   'Transform', 'Priorizado',  'DATA'),
  ('de400000-0000-4000-8000-000000000005', 'de200000-0000-4000-8000-000000000001', NULL,
   'Chatbot de Atendimento', 'Assistente virtual no site e no WhatsApp.',                                           -240, -25, 150000.00, 'Concluido',   'Media',   'Grow',      'Aprovado',    'BOT'),
  -- Eficiência Operacional
  ('de400000-0000-4000-8000-000000000006', 'de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001',
   'Automação Financeira (RPA)', 'Robôs para conciliação bancária e contas a pagar.',                               -60, 120, 220000.00, 'EmAndamento', 'Alta',    'Run',       'Aprovado',    'RPA'),
  ('de400000-0000-4000-8000-000000000007', 'de200000-0000-4000-8000-000000000002', NULL,
   'Novo ERP',               'Substituição do ERP atual, em ondas por área.',                                        -30, 300, 900000.00, 'EmAndamento', 'Media',   'Run',       'Aprovado',    'ERP'),
  ('de400000-0000-4000-8000-000000000008', 'de200000-0000-4000-8000-000000000002', NULL,
   'Otimização Logística',   'Roteirização inteligente de entregas.',                                                 30, 180, 180000.00, 'Planejado',   'Media',   'Grow',      'Priorizado',  'LOG'),
  ('de400000-0000-4000-8000-000000000009', 'de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001',
   'Nota Fiscal Digital',    'Recebimento e escrituração automática de notas de fornecedores.',                     -200, -40, 120000.00, 'Concluido',   'Alta',    'Run',       'Aprovado',    'NFD'),
  -- Inovação 2027 (ainda em ideação, sem notas)
  ('de400000-0000-4000-8000-000000000010', 'de200000-0000-4000-8000-000000000003', 'de100000-0000-4000-8000-000000000003',
   'Marketplace de Parceiros', 'Loja de serviços de parceiros integrada ao portal.',                                 90, 330, 520000.00, 'Rascunho',    'Media',   'Transform', 'NaoAvaliado', NULL),
  ('de400000-0000-4000-8000-000000000011', 'de200000-0000-4000-8000-000000000003', NULL,
   'Assistente com IA Generativa', 'Copiloto para os atendentes com base de conhecimento.',                          60, 240, 350000.00, 'Rascunho',    'Alta',    'Transform', 'NaoAvaliado', NULL)
) AS v(id, pid, owner, name, descr, ini, fim, budget, status, prio, cat, aval, jira);

-- Membros dos projetos: você + o time de cada portfólio.
INSERT INTO "ProjectMembers" ("ProjectId", "UserId", "Active", "CreatedDate")
SELECT p."Id", m.uid::uuid, true, p."CreatedDate"
FROM "Projects" p
JOIN (VALUES
  ('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002'),
  ('de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000004'),
  ('de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000003')
) AS m(pf, uid) ON p."PortfolioId" = m.pf::uuid
WHERE p."Id"::text LIKE 'de4%' AND p."OwnerId" <> m.uid::uuid
UNION
SELECT p."Id", c.me, true, p."CreatedDate"
FROM "Projects" p, demo_ctx c
WHERE p."Id"::text LIKE 'de4%' AND p."OwnerId" <> c.me;

-- -------------------------------------------------------------------------------------
-- 6. Avaliações (notas 1–5) + score/ranking com a mesma fórmula da API (F1/F2)
-- -------------------------------------------------------------------------------------
INSERT INTO "ProjectEvaluation" ("Id", "ProjectId", "PriorityCriteriaId", "UserId", "Score", "EvaluatedAt", "Active", "CreatedDate")
SELECT ('de500000-0000-4000-8000-0000000' || v.p || v.c)::uuid,
       ('de400000-0000-4000-8000-0000000000' || v.p)::uuid,
       ('de300000-0000-4000-8000-000000000' || v.c)::uuid,
       c.me, v.nota, now() - interval '20 days', true, now() - interval '20 days'
FROM demo_ctx c, (VALUES
  -- projeto, critério, nota
  ('01','011',5),('01','012',5),('01','013',5),('01','014',2),('01','015',3),
  ('02','011',5),('02','012',4),('02','013',5),('02','014',2),('02','015',3),
  ('03','011',5),('03','012',4),('03','013',3),('03','014',2),('03','015',3),
  ('04','011',4),('04','012',4),('04','013',3),('04','014',2),('04','015',2),
  ('05','011',4),('05','012',4),('05','013',5),('05','014',1),('05','015',2),
  ('06','021',5),('06','022',5),('06','023',2),
  ('07','021',5),('07','022',4),('07','023',3),
  ('08','021',4),('08','022',4),('08','023',2),
  ('09','021',4),('09','022',5),('09','023',1)
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
SET "CurrentScore" = r.score, "RankingPosition" = r.pos, "LastPrioritizationDate" = now() - interval '19 days'
FROM ranked r
WHERE p."Id" = r."ProjectId";

-- -------------------------------------------------------------------------------------
-- 7. OKRs: objetivos, key results (perto da meta) e vínculos — 100% dos projetos alinhados
-- -------------------------------------------------------------------------------------
INSERT INTO "Objectives" ("Id", "Title", "Description", "StartDate", "EndDate", "Active", "CreatedDate")
SELECT v.id::uuid, v.title, v.descr,
       date_trunc('year', CURRENT_DATE)::date, (date_trunc('year', CURRENT_DATE) + interval '1 year - 1 day')::date,
       true, now() - interval '240 days'
FROM (VALUES
  ('de800000-0000-4000-8000-000000000001', 'Crescer a receita digital',  'Aumentar a participação dos canais digitais na receita.'),
  ('de800000-0000-4000-8000-000000000002', 'Excelência no atendimento',  'Melhorar a experiência e reduzir o custo de atendimento.'),
  ('de800000-0000-4000-8000-000000000003', 'Operação enxuta',            'Reduzir custos operacionais com automação.'),
  ('de800000-0000-4000-8000-000000000004', 'Decisões orientadas a dados','Dar às áreas de negócio indicadores confiáveis e atualizados.')
) AS v(id, title, descr);

INSERT INTO "KeyResults" ("Id", "ObjectiveId", "Title", "TargetValue", "CurrentValue", "Active", "CreatedDate") VALUES
('de900000-0000-4000-8000-000000000011', 'de800000-0000-4000-8000-000000000001', 'Receita digital (R$ mil/mês)',         500, 465, true, now() - interval '240 days'),
('de900000-0000-4000-8000-000000000012', 'de800000-0000-4000-8000-000000000001', 'Clientes ativos no app (mil)',          50,  44, true, now() - interval '240 days'),
('de900000-0000-4000-8000-000000000021', 'de800000-0000-4000-8000-000000000002', 'NPS',                                   70,  72, true, now() - interval '240 days'),
('de900000-0000-4000-8000-000000000022', 'de800000-0000-4000-8000-000000000002', '% de atendimentos resolvidos no bot',   60,  58, true, now() - interval '240 days'),
('de900000-0000-4000-8000-000000000031', 'de800000-0000-4000-8000-000000000003', 'Processos automatizados',               20,  17, true, now() - interval '210 days'),
('de900000-0000-4000-8000-000000000032', 'de800000-0000-4000-8000-000000000003', 'Redução do custo de back-office (%)',   15,  12, true, now() - interval '210 days'),
('de900000-0000-4000-8000-000000000041', 'de800000-0000-4000-8000-000000000004', 'Sistemas na nuvem (%)',                100,  85, true, now() - interval '200 days'),
('de900000-0000-4000-8000-000000000042', 'de800000-0000-4000-8000-000000000004', 'Painéis de BI publicados',              12,   9, true, now() - interval '200 days');

INSERT INTO "PortfolioObjectives" ("PortfolioId", "ObjectiveId", "CreatedDate") VALUES
('de200000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000001', now() - interval '235 days'),
('de200000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000002', now() - interval '235 days'),
('de200000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000004', now() - interval '200 days'),
('de200000-0000-4000-8000-000000000002', 'de800000-0000-4000-8000-000000000003', now() - interval '205 days'),
('de200000-0000-4000-8000-000000000003', 'de800000-0000-4000-8000-000000000001', now() - interval '10 days');

INSERT INTO "ProjectObjectives" ("ProjectId", "ObjectiveId", "Active", "CreatedDate") VALUES
('de400000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000001', true, now() - interval '120 days'),
('de400000-0000-4000-8000-000000000001', 'de800000-0000-4000-8000-000000000002', true, now() - interval '120 days'),
('de400000-0000-4000-8000-000000000002', 'de800000-0000-4000-8000-000000000001', true, now() - interval '100 days'),
('de400000-0000-4000-8000-000000000003', 'de800000-0000-4000-8000-000000000004', true, now() - interval '160 days'),
('de400000-0000-4000-8000-000000000004', 'de800000-0000-4000-8000-000000000004', true, now() - interval '20 days'),
('de400000-0000-4000-8000-000000000005', 'de800000-0000-4000-8000-000000000002', true, now() - interval '250 days'),
('de400000-0000-4000-8000-000000000006', 'de800000-0000-4000-8000-000000000003', true, now() - interval '70 days'),
('de400000-0000-4000-8000-000000000007', 'de800000-0000-4000-8000-000000000003', true, now() - interval '40 days'),
('de400000-0000-4000-8000-000000000008', 'de800000-0000-4000-8000-000000000003', true, now() - interval '20 days'),
('de400000-0000-4000-8000-000000000009', 'de800000-0000-4000-8000-000000000003', true, now() - interval '210 days'),
('de400000-0000-4000-8000-000000000010', 'de800000-0000-4000-8000-000000000001', true, now() - interval '10 days'),
('de400000-0000-4000-8000-000000000011', 'de800000-0000-4000-8000-000000000002', true, now() - interval '10 days');

-- -------------------------------------------------------------------------------------
-- 8. Dependências — o pré-requisito sempre termina antes de quem depende dele (sem conflito)
-- -------------------------------------------------------------------------------------
INSERT INTO "ProjectDependencies" ("Id", "PortfolioId", "ProjectId", "DependsOnProjectId", "UserId", "Reason", "Active", "CreatedDate")
SELECT v.id::uuid, v.pf::uuid, v.p::uuid, v.d::uuid, c.me, v.reason, true, now() - interval '90 days'
FROM demo_ctx c, (VALUES
  ('dea00000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000001', 'O app consome as APIs do novo portal.'),
  ('dea00000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000004', 'de400000-0000-4000-8000-000000000003', 'O data lake será provisionado na nuvem.'),
  ('dea00000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000003', 'O portal será hospedado na nova infraestrutura.'),
  ('dea00000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000005', 'O app reaproveita o motor de conversação do chatbot.'),
  ('dea00000-0000-4000-8000-000000000005', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000006', 'de400000-0000-4000-8000-000000000009', 'Os robôs leem as notas já digitalizadas.'),
  ('dea00000-0000-4000-8000-000000000006', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000008', 'de400000-0000-4000-8000-000000000006', 'A roteirização usa a conciliação automática de fretes.')
) AS v(id, pf, p, d, reason);

-- -------------------------------------------------------------------------------------
-- 9. Roadmap — marcos concluídos, em andamento dentro do prazo e planejados
-- -------------------------------------------------------------------------------------
INSERT INTO "RoadmapItems" ("Id", "PortfolioId", "ProjectId", "Title", "Description", "StartDate", "EndDate", "Status", "Order", "Active", "CreatedDate")
SELECT v.id::uuid, v.pf::uuid, v.p::uuid, v.title, v.descr,
       (CURRENT_DATE + v.ini)::timestamptz, (CURRENT_DATE + v.fim)::timestamptz, v.status, v.ord, true, now() - interval '150 days'
FROM (VALUES
  ('deb00000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000005', 'Chatbot em produção',            'Go-live no site e no WhatsApp.',          -240, -31, 'Concluido',   1),
  ('deb00000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000003', 'Onda 1 da migração',             'Sistemas não críticos.',                  -150, -60, 'Concluido',   2),
  ('deb00000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000001', 'Portal — segunda via digital',   'Primeira entrega do portal.',             -120, -35, 'Concluido',   3),
  ('deb00000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000002', 'MVP do App Mobile',              'Login, extrato e notificações.',           -90, -10, 'Concluido',   4),
  ('deb00000-0000-4000-8000-000000000005', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000003', 'Onda 2 da migração',             'Sistemas críticos.',                       -60,  45, 'EmAndamento', 5),
  ('deb00000-0000-4000-8000-000000000006', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000001', 'Portal — autoatendimento completo', 'Alteração cadastral e pedidos.',         -35,  60, 'EmAndamento', 6),
  ('deb00000-0000-4000-8000-000000000007', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000002', 'App Mobile — pagamentos',        'Pix e cartão dentro do app.',              -10,  75, 'EmAndamento', 7),
  ('deb00000-0000-4000-8000-000000000008', 'de200000-0000-4000-8000-000000000001', 'de400000-0000-4000-8000-000000000004', 'Data lake e primeiros painéis',  NULL,                                        20, 200, 'Planejado',   8),
  ('deb00000-0000-4000-8000-000000000009', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000009', 'Nota Fiscal Digital em produção', NULL,                                      -200, -46, 'Concluido',   1),
  ('deb00000-0000-4000-8000-000000000010', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000006', 'Robôs de conciliação',           'Bancos e cartões.',                        -60,  50, 'EmAndamento', 2),
  ('deb00000-0000-4000-8000-000000000011', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000007', 'ERP — onda Financeiro',          'Contas a pagar e a receber.',              -30, 120, 'EmAndamento', 3),
  ('deb00000-0000-4000-8000-000000000012', 'de200000-0000-4000-8000-000000000002', 'de400000-0000-4000-8000-000000000008', 'Piloto de roteirização',         'Região Sudeste.',                           30, 110, 'Planejado',   4)
) AS v(id, pf, p, title, descr, ini, fim, status, ord);

-- -------------------------------------------------------------------------------------
-- 10. Equipes (com código de convite), membros e alocação nos projetos (ProjectTeams)
-- -------------------------------------------------------------------------------------
INSERT INTO "Teams" ("Id", "Name", "InviteCode", "PortfolioId", "OwnerUserId", "Active", "CreatedDate")
SELECT v.id::uuid, v.name, v.code, v.pf::uuid, COALESCE(v.owner::uuid, c.me), true, now() - interval '200 days'
FROM demo_ctx c, (VALUES
  ('dec00000-0000-4000-8000-000000000001', 'Squad Canais Digitais', 'CNDGT234', 'de200000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002'),
  ('dec00000-0000-4000-8000-000000000002', 'Squad Plataforma',      'PTFRMA56', 'de200000-0000-4000-8000-000000000001', NULL),
  ('dec00000-0000-4000-8000-000000000003', 'Squad Back-office',     'BKFFCE78', 'de200000-0000-4000-8000-000000000002', 'de100000-0000-4000-8000-000000000001')
) AS v(id, name, code, pf, owner)
ON CONFLICT DO NOTHING;

INSERT INTO "TeamUsers" ("Id", "TeamId", "UserId", "Name", "Email", "MonthlyCapacityHours", "HourlyCost", "Active", "CreatedDate")
SELECT gen_random_uuid(), v.team::uuid, v.uid::uuid, v.name, v.email, v.cap, v.custo, true, now() - interval '190 days'
FROM (VALUES
  ('dec00000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000002', 'Bruno Lima',     'bruno.lima@prumo.demo',     120, 150.00),
  ('dec00000-0000-4000-8000-000000000001', 'de100000-0000-4000-8000-000000000004', 'Diego Rocha',    'diego.rocha@prumo.demo',    160,  95.00),
  ('dec00000-0000-4000-8000-000000000001', NULL,                                   'Fernanda Alves', 'fernanda.alves@prumo.demo', 160,  90.00),
  ('dec00000-0000-4000-8000-000000000001', NULL,                                   'Gustavo Reis',   'gustavo.reis@prumo.demo',   120,  85.00),
  ('dec00000-0000-4000-8000-000000000002', NULL,                                   'Helena Castro',  'helena.castro@prumo.demo',  160, 130.00),
  ('dec00000-0000-4000-8000-000000000002', NULL,                                   'Igor Martins',   'igor.martins@prumo.demo',   160, 110.00),
  ('dec00000-0000-4000-8000-000000000003', NULL,                                   'Juliana Costa',  'juliana.costa@prumo.demo',  140, 100.00),
  ('dec00000-0000-4000-8000-000000000003', NULL,                                   'Lucas Pereira',  'lucas.pereira@prumo.demo',  160,  80.00)
) AS v(team, uid, name, email, cap, custo)
WHERE EXISTS (SELECT 1 FROM "Teams" t WHERE t."Id" = v.team::uuid);

-- Você também entra na Squad Plataforma (aparece na capacidade com o seu e-mail).
INSERT INTO "TeamUsers" ("Id", "TeamId", "UserId", "Name", "Email", "MonthlyCapacityHours", "HourlyCost", "Active", "CreatedDate")
SELECT gen_random_uuid(), 'dec00000-0000-4000-8000-000000000002', c.me, c.my_name, c.my_email, 40, 180.00, true, now() - interval '190 days'
FROM demo_ctx c
WHERE EXISTS (SELECT 1 FROM "Teams" t WHERE t."Id" = 'dec00000-0000-4000-8000-000000000002');

INSERT INTO "ProjectTeams" ("ProjectId", "TeamId", "Active", "CreatedDate")
SELECT v.p::uuid, v.t::uuid, true, now() - interval '100 days'
FROM (VALUES
  ('de400000-0000-4000-8000-000000000001', 'dec00000-0000-4000-8000-000000000001'),
  ('de400000-0000-4000-8000-000000000002', 'dec00000-0000-4000-8000-000000000001'),
  ('de400000-0000-4000-8000-000000000005', 'dec00000-0000-4000-8000-000000000001'),
  ('de400000-0000-4000-8000-000000000003', 'dec00000-0000-4000-8000-000000000002'),
  ('de400000-0000-4000-8000-000000000004', 'dec00000-0000-4000-8000-000000000002'),
  ('de400000-0000-4000-8000-000000000001', 'dec00000-0000-4000-8000-000000000002'),
  ('de400000-0000-4000-8000-000000000006', 'dec00000-0000-4000-8000-000000000003'),
  ('de400000-0000-4000-8000-000000000007', 'dec00000-0000-4000-8000-000000000003'),
  ('de400000-0000-4000-8000-000000000008', 'dec00000-0000-4000-8000-000000000003'),
  ('de400000-0000-4000-8000-000000000009', 'dec00000-0000-4000-8000-000000000003')
) AS v(p, t)
WHERE EXISTS (SELECT 1 FROM "Teams" t WHERE t."Id" = v.t::uuid);

-- -------------------------------------------------------------------------------------
-- 11. Integração Jira (sem falhas) + histórico de sincronizações
--     IsActive = false evita que o job tente usar o token fictício. Se você já tiver uma
--     integração Jira configurada, ela é mantida (ON CONFLICT pelo índice único de Type).
-- -------------------------------------------------------------------------------------
INSERT INTO "Integrations" ("Id", "Type", "ApiUrl", "Email", "Token", "Status", "IsActive", "SyncIntervalMinutes",
                            "FailedAttempts", "LastSyncedAt", "NextAttemptAt", "Active", "CreatedDate", "UpdatedDate")
SELECT 'ded00000-0000-4000-8000-000000000001', 'Jira', 'https://prumo-demo.atlassian.net', c.my_email, 'demo-token-nao-valido',
       'Conectada', false, 60, 0, now() - interval '20 minutes', NULL, true, now() - interval '200 days', now() - interval '20 minutes'
FROM demo_ctx c
ON CONFLICT DO NOTHING;

INSERT INTO "IntegrationSyncLogs" ("Id", "IntegrationId", "StartedAt", "FinishedAt", "Success", "IssuesProcessed", "WorklogsProcessed", "ErrorMessage")
SELECT gen_random_uuid(), 'ded00000-0000-4000-8000-000000000001',
       now() - make_interval(hours => h) - interval '20 minutes',
       now() - make_interval(hours => h) - interval '19 minutes',
       true, 40 + h % 9, 110 + h % 13, NULL
FROM generate_series(0, 11) AS h
WHERE EXISTS (SELECT 1 FROM "Integrations" WHERE "Id" = 'ded00000-0000-4000-8000-000000000001');

-- -------------------------------------------------------------------------------------
-- 12. Issues e worklogs "importados do Jira"
--     Concluídas antes do prazo, poucos bugs, nenhuma atrasada; as abertas têm prazo futuro.
--     A estimativa restante das abertas é calibrada para ~80–85% da capacidade de cada membro
--     (ocupação "Adequada").
-- -------------------------------------------------------------------------------------
DROP TABLE IF EXISTS demo_issue_plan;
CREATE TEMP TABLE demo_issue_plan AS
SELECT v.n, v.pid::uuid AS pid, v.jira, v.team::uuid AS team, v.feitas, v.abertas,
       p."StartDate" AS ini,
       LEAST(CURRENT_DATE, COALESCE(p."CompletedAt"::date, CURRENT_DATE)) AS ate,
       (SELECT array_agg(lower(tu."Email") ORDER BY tu."Name") FROM "TeamUsers" tu WHERE tu."TeamId" = v.team::uuid) AS emails
FROM (VALUES
  ( 1, 'de400000-0000-4000-8000-000000000001', 'PORT',  'dec00000-0000-4000-8000-000000000001', 18, 8),
  ( 2, 'de400000-0000-4000-8000-000000000002', 'APP',   'dec00000-0000-4000-8000-000000000001', 14, 8),
  ( 3, 'de400000-0000-4000-8000-000000000003', 'CLOUD', 'dec00000-0000-4000-8000-000000000002', 20, 6),
  ( 4, 'de400000-0000-4000-8000-000000000004', 'DATA',  'dec00000-0000-4000-8000-000000000002',  0, 4),
  ( 5, 'de400000-0000-4000-8000-000000000005', 'BOT',   'dec00000-0000-4000-8000-000000000001', 22, 0),
  ( 6, 'de400000-0000-4000-8000-000000000006', 'RPA',   'dec00000-0000-4000-8000-000000000003', 10, 5),
  ( 7, 'de400000-0000-4000-8000-000000000007', 'ERP',   'dec00000-0000-4000-8000-000000000003',  6, 6),
  ( 8, 'de400000-0000-4000-8000-000000000008', 'LOG',   'dec00000-0000-4000-8000-000000000003',  0, 3),
  ( 9, 'de400000-0000-4000-8000-000000000009', 'NFD',   'dec00000-0000-4000-8000-000000000003', 16, 0)
) AS v(n, pid, jira, team, feitas, abertas)
JOIN "Projects" p ON p."Id" = v.pid::uuid
WHERE EXISTS (SELECT 1 FROM "Teams" t WHERE t."Id" = v.team::uuid);

INSERT INTO "Issues" ("Id", "ProjectId", "ExternalId", "Source", "Title", "Type", "Status", "Done", "AssigneeEmail",
                      "EstimateHours", "SpentHours", "DueDate", "CreatedAt", "UpdatedAt", "CompletedAt")
SELECT ('dee00000-0000-4000-8000-' || lpad(x.n::text, 2, '0') || lpad(x.i::text, 10, '0'))::uuid,
       x.pid, x.jira || '-' || (100 + x.i), 'Jira',
       (ARRAY['Tela de login com SSO','Integração com gateway de pagamento','Otimizar consulta de pedidos',
              'Segunda via em PDF','Painel de uso','Notificações por e-mail','Cadastro de endereço',
              'Pipeline de deploy automatizado','Exportação para Excel','Testes automatizados de regressão',
              'Histórico de atendimentos','Acessibilidade no mobile','Monitoramento e alertas',
              'Documentação da API'])[1 + (x.i * 5 + x.n) % 14],
       CASE WHEN x.done AND x.i % 10 = 0 THEN 'Bug' WHEN x.i % 4 = 0 THEN 'Feature' WHEN x.i % 3 = 0 THEN 'Task' ELSE 'Story' END,
       CASE WHEN x.done THEN 'Concluído' WHEN x.j <= 2 THEN 'Em andamento' WHEN x.j = 3 THEN 'Em revisão' ELSE 'A fazer' END,
       x.done,
       x.emails[1 + (x.i + x.n) % array_length(x.emails, 1)],
       x.est,
       CASE WHEN x.done THEN round(x.est * (0.85 + (x.i % 4) * 0.05), 1) WHEN x.j <= 3 AND x.ini <= CURRENT_DATE THEN 4 + x.i % 3 ELSE 0 END,
       CASE WHEN x.done THEN x.criada::date + 9 + x.i % 5 ELSE GREATEST(CURRENT_DATE, x.ini) + 12 + x.j * 6 END,
       x.criada,
       CASE WHEN x.done THEN x.criada + make_interval(days => 5 + x.i % 4) ELSE now() - make_interval(hours => 3 + x.j) END,
       CASE WHEN x.done THEN x.criada + make_interval(days => 5 + x.i % 4) END
FROM (
    SELECT dp.*, g.i, g.i <= dp.feitas AS done, g.i - dp.feitas AS j,
           (8 + (g.i % 4) * 4)::numeric AS est,
           CASE WHEN g.i <= dp.feitas
                THEN dp.ini::timestamptz + interval '9 hours'
                     + make_interval(days => ((g.i - 1) * GREATEST(1, (dp.ate - dp.ini) - 12) / GREATEST(1, dp.feitas)))
                ELSE now() - make_interval(days => 3 + (g.i - dp.feitas) * 2)
           END AS criada
    FROM demo_issue_plan dp
    CROSS JOIN LATERAL generate_series(1, dp.feitas + dp.abertas) AS g(i)
) x;

-- Calibra a demanda das issues abertas: ~82% da capacidade mensal de cada membro.
WITH abertas AS (
    SELECT i."Id", i."SpentHours", tu."MonthlyCapacityHours" AS cap,
           count(*) OVER (PARTITION BY lower(i."AssigneeEmail")) AS qtd
    FROM "Issues" i
    JOIN (SELECT DISTINCT ON (lower("Email")) lower("Email") AS email, "MonthlyCapacityHours"
          FROM "TeamUsers" WHERE "TeamId"::text LIKE 'dec%' ORDER BY lower("Email")) tu
      ON tu.email = lower(i."AssigneeEmail")
    WHERE i."Id"::text LIKE 'dee%' AND NOT i."Done"
)
UPDATE "Issues" i
SET "EstimateHours" = round(a."SpentHours" + a.cap * 0.82 / a.qtd, 1)
FROM abertas a
WHERE i."Id" = a."Id";

-- Worklogs: 3 apontamentos por issue concluída (ao longo da execução) e 2 recentes por issue aberta.
INSERT INTO "Worklogs" ("Id", "IssueId", "ExternalId", "AuthorEmail", "Date", "Hours")
SELECT gen_random_uuid(), i."Id", 'WL-' || i."ExternalId" || '-' || k, i."AssigneeEmail",
       CASE WHEN i."Done"
            THEN (i."CreatedAt" + (i."CompletedAt" - i."CreatedAt") * k / 3.0)::date
            ELSE CURRENT_DATE - (k - 1) * 2 END,
       round(i."SpentHours" / CASE WHEN i."Done" THEN 3 ELSE 2 END, 2)
FROM "Issues" i
CROSS JOIN generate_series(1, 3) AS k
WHERE i."Id"::text LIKE 'dee%' AND i."SpentHours" > 0 AND (i."Done" OR k <= 2);

-- -------------------------------------------------------------------------------------
-- 13. Financeiro — orçamento, lançamentos, business case (VPL positivo) e retornos
--     Custo realizado (horas × custo/hora + lançamentos) fica em ~88% do proporcional ao
--     tempo decorrido: projeção abaixo do orçamento, sem risco de estouro.
-- -------------------------------------------------------------------------------------
INSERT INTO "Budgets" ("Id", "ProjectId", "TotalAmount", "Currency", "Active", "CreatedDate")
SELECT ('de600000-0000-4000-8000-0000000000' || right(p."Id"::text, 2))::uuid, p."Id", p."ApprovedBudget", 'BRL', true, p."CreatedDate"
FROM "Projects" p WHERE p."Id"::text LIKE 'de4%';

WITH custo_horas AS (
    SELECT i."ProjectId", sum(w."Hours" * COALESCE(c.custo, 0)) AS valor
    FROM "Worklogs" w
    JOIN "Issues" i ON i."Id" = w."IssueId"
    LEFT JOIN (SELECT lower("Email") AS email, avg("HourlyCost") AS custo FROM "TeamUsers" GROUP BY 1) c
           ON c.email = lower(w."AuthorEmail")
    WHERE i."Id"::text LIKE 'dee%'
    GROUP BY i."ProjectId"
), alvo AS (
    SELECT p."Id", p."StartDate",
           LEAST(CURRENT_DATE, COALESCE(p."CompletedAt"::date, p."EndDate")) AS ref,
           p."ApprovedBudget"
             * LEAST(1.0, (LEAST(CURRENT_DATE, COALESCE(p."CompletedAt"::date, p."EndDate")) - p."StartDate")::numeric
                          / (p."EndDate" - p."StartDate"))
             * CASE WHEN p."Status" = 'Concluido' THEN 0.93 ELSE 0.88 END
             - COALESCE(ch.valor, 0) AS restante
    FROM "Projects" p
    LEFT JOIN custo_horas ch ON ch."ProjectId" = p."Id"
    WHERE p."Id"::text LIKE 'de4%' AND p."StartDate" < CURRENT_DATE
), meses AS (
    SELECT a.*, d.dt::date AS dt, count(*) OVER (PARTITION BY a."Id") AS qtd
    FROM alvo a
    CROSS JOIN LATERAL generate_series(a."StartDate" + 5, a.ref, interval '1 month') AS d(dt)
    WHERE a.restante > 0
)
INSERT INTO "BudgetExpenses" ("Id", "ProjectId", "Date", "Description", "Amount", "Category", "Active", "CreatedDate")
SELECT gen_random_uuid(), m."Id", m.dt,
       CASE c.cat WHEN 'Custo' THEN 'Serviços de consultoria e desenvolvimento — ' ELSE 'Licenças e infraestrutura em nuvem — ' END
         || to_char(m.dt, 'MM/YYYY'),
       round(m.restante / m.qtd * CASE c.cat WHEN 'Custo' THEN 0.65 ELSE 0.35 END, 2),
       c.cat, true, m.dt
FROM meses m
CROSS JOIN (VALUES ('Custo'), ('Despesa')) AS c(cat);

-- Business case: taxa anual em % com até 4 casas (coluna decimal(7,4)); fluxos de 30 meses
-- com rampa nos 3 primeiros meses → VPL bem positivo.
INSERT INTO "BusinessCases" ("Id", "ProjectId", "InitialInvestment", "AnnualDiscountRate", "Active", "CreatedDate")
SELECT ('de700000-0000-4000-8000-0000000000' || right(p."Id"::text, 2))::uuid, p."Id", p."ApprovedBudget",
       CASE WHEN p."PortfolioId" = 'de200000-0000-4000-8000-000000000002' THEN 10.7500 ELSE 11.2500 END,
       true, p."CreatedDate"
FROM "Projects" p
WHERE p."Id"::text LIKE 'de4%';

INSERT INTO "CashFlowForecasts" ("Id", "BusinessCaseId", "Month", "Value")
SELECT gen_random_uuid(), bc."Id", m,
       round(bc."InitialInvestment" / 9.0 * LEAST(1, m / 3.0), 2)
FROM "BusinessCases" bc CROSS JOIN generate_series(1, 30) AS m
WHERE bc."Id"::text LIKE 'de7%';

-- Retornos já realizados: mensais desde a primeira entrega, acima do previsto.
INSERT INTO "RealizedReturns" ("Id", "ProjectId", "Date", "Description", "Value", "Active", "CreatedDate")
SELECT gen_random_uuid(), p."Id", d.dt::date, v.descr || ' — ' || to_char(d.dt, 'MM/YYYY'),
       round(p."ApprovedBudget" / 9.0 * v.fator, 2), true, d.dt
FROM (VALUES
  ('de400000-0000-4000-8000-000000000005', 'Redução de chamados no call center',   -200, 1.10),
  ('de400000-0000-4000-8000-000000000009', 'Horas economizadas na escrituração',   -160, 1.15),
  ('de400000-0000-4000-8000-000000000001', 'Adesão à segunda via digital',           -35, 0.60),
  ('de400000-0000-4000-8000-000000000002', 'Novas vendas pelo app',                  -10, 0.45),
  ('de400000-0000-4000-8000-000000000003', 'Desligamento de servidores on-premise',  -60, 0.50),
  ('de400000-0000-4000-8000-000000000006', 'Horas economizadas em conciliação',      -20, 0.40)
) AS v(pid, descr, desde, fator)
JOIN "Projects" p ON p."Id" = v.pid::uuid
CROSS JOIN LATERAL generate_series(CURRENT_DATE + v.desde + 5, CURRENT_DATE - 1, interval '1 month') AS d(dt);

-- -------------------------------------------------------------------------------------
-- 14. Notificações — poucas, todas tratadas (lidas/arquivadas) e uma informativa nova
-- -------------------------------------------------------------------------------------
INSERT INTO "Alerts" ("Id", "UserId", "Type", "Status", "Message", "EntityType", "EntityId", "SentAt", "ReadAt", "ArchivedAt", "Active", "CreatedDate")
SELECT v.id::uuid, c.me, v.tipo, v.status, v.msg, 'Project', v.ent::uuid,
       now() - make_interval(hours => v.h),
       CASE WHEN v.status IN ('Lida', 'Arquivada') THEN now() - make_interval(hours => v.h - 1) END,
       CASE WHEN v.status = 'Arquivada' THEN now() - make_interval(hours => v.h - 2) END,
       true, now() - make_interval(hours => v.h)
FROM demo_ctx c, (VALUES
  ('def00000-0000-4000-8000-000000000001', 'Conflito',         'Enviada',   'Nova dependência registrada: "Otimização Logística" depende de "Automação Financeira (RPA)", que termina antes. Sem conflito de datas.', 'de400000-0000-4000-8000-000000000008',   3),
  ('def00000-0000-4000-8000-000000000002', 'EstouroOrcamento', 'Lida',      'Revisão mensal: "Novo ERP" consumiu 9% do orçamento, abaixo do previsto para o período.',                                    'de400000-0000-4000-8000-000000000007',  30),
  ('def00000-0000-4000-8000-000000000003', 'Risco',            'Lida',      'Risco de disponibilidade da API do parceiro de pagamentos mitigado no "App Mobile".',                                          'de400000-0000-4000-8000-000000000002',  72),
  ('def00000-0000-4000-8000-000000000004', 'Atraso',           'Arquivada', 'O marco "Onda 1 da migração" foi recuperado e entregue dentro do prazo.',                                                      'de400000-0000-4000-8000-000000000003', 400),
  ('def00000-0000-4000-8000-000000000005', 'Desalinhamento',   'Arquivada', 'O projeto "Plataforma de Dados" foi vinculado ao objetivo "Decisões orientadas a dados".',                                     'de400000-0000-4000-8000-000000000004', 480)
) AS v(id, tipo, status, msg, ent, h);

-- -------------------------------------------------------------------------------------
-- 15. Histórico de relatórios gerados
-- -------------------------------------------------------------------------------------
INSERT INTO "Reports" ("Id", "PortfolioId", "Name", "Type", "Format", "GeneratedById", "GeneratedAt")
SELECT v.id::uuid, v.pf::uuid, v.name, v.tipo, v.fmt, c.me, now() - make_interval(days => v.d)
FROM demo_ctx c, (VALUES
  ('de0f0000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000001', 'portfolio-transformacao-digital.pdf',   'Portfolio', 'PDF',   30),
  ('de0f0000-0000-4000-8000-000000000002', 'de200000-0000-4000-8000-000000000001', 'executivo-transformacao-digital.pdf',   'Executivo', 'PDF',    7),
  ('de0f0000-0000-4000-8000-000000000003', 'de200000-0000-4000-8000-000000000001', 'executivo-transformacao-digital.xlsx',  'Executivo', 'Excel',  2),
  ('de0f0000-0000-4000-8000-000000000004', 'de200000-0000-4000-8000-000000000002', 'portfolio-eficiencia-operacional.pdf',  'Portfolio', 'PDF',    4)
) AS v(id, pf, name, tipo, fmt, d);

COMMIT;

-- -------------------------------------------------------------------------------------
-- Conferência rápida: consumo do orçamento x tempo decorrido (deve ficar abaixo de 100%)
-- -------------------------------------------------------------------------------------
SELECT p."Name" AS projeto, p."Status", p."CurrentScore" AS score, p."RankingPosition" AS posicao,
       round(100.0 * (COALESCE(e.total, 0) + COALESCE(h.total, 0)) / p."ApprovedBudget", 1) AS "% orçamento consumido",
       round(100.0 * GREATEST(0, LEAST(CURRENT_DATE, COALESCE(p."CompletedAt"::date, p."EndDate")) - p."StartDate")
             / (p."EndDate" - p."StartDate"), 1) AS "% prazo decorrido"
FROM "Projects" p
LEFT JOIN (SELECT "ProjectId", sum("Amount") AS total FROM "BudgetExpenses" GROUP BY 1) e ON e."ProjectId" = p."Id"
LEFT JOIN (SELECT i."ProjectId", sum(w."Hours" * c.custo) AS total
           FROM "Worklogs" w JOIN "Issues" i ON i."Id" = w."IssueId"
           JOIN (SELECT lower("Email") AS email, avg("HourlyCost") AS custo FROM "TeamUsers" GROUP BY 1) c ON c.email = lower(w."AuthorEmail")
           GROUP BY 1) h ON h."ProjectId" = p."Id"
WHERE p."Id"::text LIKE 'de4%'
ORDER BY p."PortfolioId", p."RankingPosition" NULLS LAST;
