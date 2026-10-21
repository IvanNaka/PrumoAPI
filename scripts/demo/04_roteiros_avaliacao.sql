-- =====================================================================================
-- Prumo — preparação dos ROTEIROS DE AVALIAÇÃO (Formulários 1, 2 e 3) — PostgreSQL
--
-- Cada participante faz login com a própria conta Google e, na tela de boas-vindas, usa
-- "Entrar em uma equipe" com o código do seu roteiro (Atividade 2 - "Entrar no time"):
--
--   Código    Roteiro                                         O que o participante recebe
--   --------  ----------------------------------------------  -----------------------------------------
--   ROTEIRO1  Form. 1 — Criação e configuração do portfólio   ProductOwner: cria portfólio, critérios e OKR.
--                                                              Começa sem portfólios (cria o próprio).
--   ROTEIRO2  Form. 2 — Gerenciamento e priorização           GerenteProjeto + ProductOwner (+ Desenvolvedor,
--                                                              dado pelo próprio onboarding, p/ integração Jira).
--                                                              Ganha um portfólio PRÓPRIO já priorizado, com
--                                                              critérios, um OKR, 3 projetos avaliados, equipe e
--                                                              issues (indicadores operacionais).
--   ROTEIRO3  Form. 3 — Análise estratégica e dashboards      GerenteProjeto + membro dos portfólios da carga
--                                                              de demonstração ("Transformação Digital" e
--                                                              "Eficiência Operacional"), com todos os indicadores.
--
-- Como funciona: o onboarding da API ("Entrar em uma equipe") cria a linha em "TeamUsers" e dá o
-- perfil Desenvolvedor. Um gatilho no banco detecta a entrada num dos times acima e, na mesma
-- transação, concede os perfis do roteiro e prepara os dados. A API recarrega os perfis antes de
-- emitir o novo token; se algum menu não aparecer, basta o participante sair e entrar de novo.
--
-- Pré-requisitos:
--   * Banco atualizado até a migration AddProjectTeams.
--   * Roteiro 3 usa os portfólios de 01_seed_demo.sql: rode-o antes deste script.
--   * Cada participante precisa ser um usuário NOVO (sem perfil). Para repetir a sessão com as
--     mesmas contas, rode 05_resetar_participantes.sql.
--
-- Pode ser rodado de novo (recria times, função e gatilho). Os códigos podem ser trocados abaixo
-- (até 8 caracteres, sem repetir códigos de outras equipes).
-- =====================================================================================

BEGIN;

-- -------------------------------------------------------------------------------------
-- 1. Times dos roteiros (IDs fixos "de11...-00000000000N", N = número do roteiro)
-- -------------------------------------------------------------------------------------
INSERT INTO "Teams" ("Id", "Name", "InviteCode", "PortfolioId", "OwnerUserId", "Active", "CreatedDate")
VALUES
  ('de110000-0000-4000-8000-000000000001', 'Avaliação — Roteiro 1 (Portfólio)',    'ROTEIRO1', NULL, NULL, true, now()),
  ('de110000-0000-4000-8000-000000000002', 'Avaliação — Roteiro 2 (Priorização)',  'ROTEIRO2', NULL, NULL, true, now()),
  ('de110000-0000-4000-8000-000000000003', 'Avaliação — Roteiro 3 (Dashboards)',   'ROTEIRO3', NULL, NULL, true, now())
ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "InviteCode" = EXCLUDED."InviteCode", "Active" = true;

-- -------------------------------------------------------------------------------------
-- 2. Utilitário: UUID com prefixo "de" (assim 02_remover_demo.sql também limpa esses dados)
-- -------------------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION prumo_demo_uuid() RETURNS uuid
LANGUAGE sql VOLATILE AS
$$ SELECT overlay(gen_random_uuid()::text PLACING 'de' FROM 1 FOR 2)::uuid $$;

-- -------------------------------------------------------------------------------------
-- 3. Roteiro 2: portfólio próprio do participante, pronto para cadastrar/avaliar/priorizar
-- -------------------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION prumo_roteiro2_preparar(p_user uuid) RETURNS void
LANGUAGE plpgsql AS
$$
DECLARE
    v_nome   text;
    v_curto  text;
    v_tag    text := upper(substr(md5(p_user::text), 1, 4));
    v_pf     uuid := prumo_demo_uuid();
    v_team   uuid := prumo_demo_uuid();
    v_obj    uuid := prumo_demo_uuid();
    v_c1 uuid := prumo_demo_uuid(); v_c2 uuid := prumo_demo_uuid();
    v_c3 uuid := prumo_demo_uuid(); v_c4 uuid := prumo_demo_uuid();
    v_p1 uuid := prumo_demo_uuid(); v_p2 uuid := prumo_demo_uuid(); v_p3 uuid := prumo_demo_uuid();
BEGIN
    -- Já preparado (ex.: gatilho disparado de novo)? Não duplica.
    IF EXISTS (SELECT 1 FROM "Portfolios" WHERE "OwnerId" = p_user AND "Id"::text LIKE 'de%') THEN
        RETURN;
    END IF;

    SELECT "Name" INTO v_nome FROM "Users" WHERE "Id" = p_user;
    v_curto := split_part(COALESCE(NULLIF(trim(v_nome), ''), 'Participante'), ' ', 1);

    -- Portfólio
    INSERT INTO "Portfolios" ("Id", "Name", "Description", "Goal", "OwnerId", "Status", "Active", "CreatedDate")
    VALUES (v_pf, left('Portfólio de ' || v_curto || ' (' || v_tag || ')', 150),
            'Portfólio de avaliação: canais digitais da empresa.',
            'Aumentar a receita digital em 25% no ano.',
            p_user, 'Priorizado', true, now() - interval '30 days');

    -- Critérios (soma dos pesos = 10)
    INSERT INTO "PriorityCriteria" ("Id", "PortfolioId", "UserId", "Name", "Description", "Type", "ValueWeight", "Active", "CreatedDate") VALUES
      (v_c1, v_pf, p_user, 'Valor estratégico',  'Alinhamento com os objetivos da empresa.',          'Beneficio', 4.00, true, now() - interval '30 days'),
      (v_c2, v_pf, p_user, 'Retorno financeiro', 'Receita incremental ou redução de custo esperada.', 'Beneficio', 3.00, true, now() - interval '30 days'),
      (v_c3, v_pf, p_user, 'Risco técnico',      'Incerteza tecnológica e de execução.',              'Custo',     2.00, true, now() - interval '30 days'),
      (v_c4, v_pf, p_user, 'Esforço',            'Tamanho relativo da iniciativa.',                   'Custo',     1.00, true, now() - interval '30 days');

    -- OKR (usado na Atividade 5 - "Associar projeto ao OKR")
    INSERT INTO "Objectives" ("Id", "Title", "Description", "StartDate", "EndDate", "Active", "CreatedDate")
    VALUES (v_obj, left('Crescer a receita digital — ' || v_curto || ' (' || v_tag || ')', 200),
            'Objetivo estratégico do portfólio de avaliação.',
            date_trunc('year', CURRENT_DATE)::date, (date_trunc('year', CURRENT_DATE) + interval '1 year - 1 day')::date,
            true, now() - interval '30 days');
    INSERT INTO "KeyResults" ("Id", "ObjectiveId", "Title", "TargetValue", "CurrentValue", "Active", "CreatedDate") VALUES
      (prumo_demo_uuid(), v_obj, 'Receita digital (R$ mil/mês)', 400, 310, true, now() - interval '30 days'),
      (prumo_demo_uuid(), v_obj, 'Clientes ativos no app (mil)',  30,  22, true, now() - interval '30 days');
    INSERT INTO "PortfolioObjectives" ("PortfolioId", "ObjectiveId", "CreatedDate") VALUES (v_pf, v_obj, now() - interval '30 days');

    -- Projetos já avaliados (para o ranking ter com o que comparar)
    INSERT INTO "Projects" ("Id", "PortfolioId", "OwnerId", "Name", "Description", "StartDate", "EndDate", "CompletedAt",
                            "ApprovedBudget", "Status", "Priority", "StrategicCategory", "EvaluationStatus", "JiraProjectKey",
                            "Active", "CreatedDate") VALUES
      (v_p1, v_pf, p_user, 'Portal do Cliente',      'Portal self-service com segunda via e acompanhamento de pedidos.',
       CURRENT_DATE - 90, CURRENT_DATE + 90,  NULL, 300000, 'EmAndamento', 'Alta',  'Grow',      'Priorizado', 'P' || v_tag, true, now() - interval '100 days'),
      (v_p2, v_pf, p_user, 'Migração para Nuvem',    'Migração dos sistemas legados para a nuvem.',
       CURRENT_DATE - 120, CURRENT_DATE + 60, NULL, 450000, 'EmAndamento', 'Alta',  'Run',       'Priorizado', 'C' || v_tag, true, now() - interval '130 days'),
      (v_p3, v_pf, p_user, 'Plataforma de Dados',    'Data lake e painéis de BI.',
       CURRENT_DATE + 30, CURRENT_DATE + 210, NULL, 250000, 'Planejado',   'Media', 'Transform', 'Priorizado', NULL,          true, now() - interval '20 days');

    INSERT INTO "Budgets" ("Id", "ProjectId", "TotalAmount", "Currency", "Active", "CreatedDate")
    SELECT prumo_demo_uuid(), p."Id", p."ApprovedBudget", 'BRL', true, p."CreatedDate"
    FROM "Projects" p WHERE p."PortfolioId" = v_pf;

    INSERT INTO "ProjectObjectives" ("ProjectId", "ObjectiveId", "Active", "CreatedDate") VALUES
      (v_p1, v_obj, true, now() - interval '30 days'),
      (v_p2, v_obj, true, now() - interval '30 days');

    INSERT INTO "ProjectEvaluation" ("Id", "ProjectId", "PriorityCriteriaId", "UserId", "Score", "EvaluatedAt", "Active", "CreatedDate")
    SELECT prumo_demo_uuid(), v.p, v.c, p_user, v.nota, now() - interval '15 days', true, now() - interval '15 days'
    FROM (VALUES (v_p1, v_c1, 5), (v_p1, v_c2, 4), (v_p1, v_c3, 2), (v_p1, v_c4, 3),
                 (v_p2, v_c1, 4), (v_p2, v_c2, 3), (v_p2, v_c3, 3), (v_p2, v_c4, 4),
                 (v_p3, v_c1, 4), (v_p3, v_c2, 4), (v_p3, v_c3, 2), (v_p3, v_c4, 2)) AS v(p, c, nota);

    -- Score (F1) e ranking (F2), com a mesma fórmula da API
    WITH scores AS (
        SELECT e."ProjectId",
               round(((sum(pc."ValueWeight" * CASE WHEN pc."Type" = 'Custo' THEN 6 - e."Score" ELSE e."Score" END)
                       / sum(pc."ValueWeight")) - 1) / 4 * 100, 2) AS score
        FROM "ProjectEvaluation" e JOIN "PriorityCriteria" pc ON pc."Id" = e."PriorityCriteriaId"
        WHERE pc."PortfolioId" = v_pf
        GROUP BY e."ProjectId"
    ), ranked AS (
        SELECT s."ProjectId", s.score,
               row_number() OVER (ORDER BY s.score DESC,
                                  CASE p."Priority" WHEN 'Critica' THEN 4 WHEN 'Alta' THEN 3 WHEN 'Media' THEN 2 ELSE 1 END DESC,
                                  p."CreatedDate") AS pos
        FROM scores s JOIN "Projects" p ON p."Id" = s."ProjectId"
    )
    UPDATE "Projects" p
    SET "CurrentScore" = r.score, "RankingPosition" = r.pos, "LastPrioritizationDate" = now() - interval '14 days'
    FROM ranked r WHERE p."Id" = r."ProjectId";

    -- Equipe do portfólio (capacidade) alocada aos projetos
    INSERT INTO "Teams" ("Id", "Name", "InviteCode", "PortfolioId", "OwnerUserId", "Active", "CreatedDate")
    VALUES (v_team, left('Squad ' || v_curto || ' (' || v_tag || ')', 100),
            translate(upper(substr(md5(v_team::text), 1, 8)), '01', 'XY'), v_pf, p_user, true, now() - interval '60 days');
    INSERT INTO "TeamUsers" ("Id", "TeamId", "UserId", "Name", "Email", "MonthlyCapacityHours", "HourlyCost", "Active", "CreatedDate") VALUES
      (prumo_demo_uuid(), v_team, NULL, 'Fernanda Alves', 'fernanda.alves@prumo.demo', 160,  90, true, now() - interval '60 days'),
      (prumo_demo_uuid(), v_team, NULL, 'Gustavo Reis',   'gustavo.reis@prumo.demo',   120,  85, true, now() - interval '60 days'),
      (prumo_demo_uuid(), v_team, NULL, 'Helena Castro',  'helena.castro@prumo.demo',  160, 130, true, now() - interval '60 days');
    INSERT INTO "ProjectTeams" ("ProjectId", "TeamId", "Active", "CreatedDate") VALUES
      (v_p1, v_team, true, now() - interval '60 days'),
      (v_p2, v_team, true, now() - interval '60 days');

    -- Issues "do Jira" (qualidade, lead time, throughput e ocupação) nos dois projetos em andamento
    INSERT INTO "Issues" ("Id", "ProjectId", "ExternalId", "Source", "Title", "Type", "Status", "Done", "AssigneeEmail",
                          "EstimateHours", "SpentHours", "DueDate", "CreatedAt", "UpdatedAt", "CompletedAt")
    SELECT prumo_demo_uuid(), x.pid, x.chave || '-' || (100 + g.i), 'Jira',
           (ARRAY['Tela de login com SSO','Integração com pagamentos','Otimizar consulta de pedidos','Segunda via em PDF',
                  'Painel de uso','Notificações por e-mail','Pipeline de deploy','Testes de regressão',
                  'Histórico de atendimentos','Monitoramento e alertas'])[1 + g.i % 10],
           CASE WHEN g.i % 7 = 0 THEN 'Bug' WHEN g.i % 4 = 0 THEN 'Feature' WHEN g.i % 3 = 0 THEN 'Task' ELSE 'Story' END,
           CASE WHEN g.i <= 10 THEN 'Concluído' WHEN g.i <= 12 THEN 'Em andamento' ELSE 'A fazer' END,
           g.i <= 10,
           (ARRAY['fernanda.alves@prumo.demo','gustavo.reis@prumo.demo','helena.castro@prumo.demo'])[1 + g.i % 3],
           8 + (g.i % 4) * 4 + CASE WHEN g.i > 10 THEN 40 ELSE 0 END,
           CASE WHEN g.i <= 10 THEN 8 + (g.i % 4) * 4 WHEN g.i <= 12 THEN 6 ELSE 0 END,
           CASE WHEN g.i <= 10 THEN CURRENT_DATE - 80 + g.i * 7 + 10 ELSE CURRENT_DATE + 7 * (g.i - 9) END,
           CASE WHEN g.i <= 10 THEN now() - make_interval(days => 85 - g.i * 7) ELSE now() - make_interval(days => 2 * (15 - g.i)) END,
           now() - make_interval(days => GREATEST(1, 80 - g.i * 7)),
           CASE WHEN g.i <= 10 THEN now() - make_interval(days => 80 - g.i * 7) END
    FROM (VALUES (v_p1, 'P' || v_tag), (v_p2, 'C' || v_tag)) AS x(pid, chave)
    CROSS JOIN generate_series(1, 14) AS g(i);

    INSERT INTO "Worklogs" ("Id", "IssueId", "ExternalId", "AuthorEmail", "Date", "Hours")
    SELECT prumo_demo_uuid(), i."Id", 'WL-' || i."ExternalId" || '-' || k, i."AssigneeEmail",
           CASE WHEN i."Done" THEN (i."CreatedAt" + (i."CompletedAt" - i."CreatedAt") * k / 2.0)::date ELSE CURRENT_DATE - k END,
           round(i."SpentHours" / 2, 2)
    FROM "Issues" i CROSS JOIN generate_series(1, 2) AS k
    WHERE i."ProjectId" IN (v_p1, v_p2) AND i."SpentHours" > 0;
END;
$$;

-- -------------------------------------------------------------------------------------
-- 4. Gatilho: entrada num time de roteiro → perfis + dados
--    (nunca insere "Desenvolvedor": a própria API insere esse perfil na mesma transação)
-- -------------------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION prumo_roteiro_ao_entrar() RETURNS trigger
LANGUAGE plpgsql AS
$$
DECLARE
    v_roteiro text := right(NEW."TeamId"::text, 1);
BEGIN
    IF v_roteiro = '1' THEN
        INSERT INTO "UserRoles" ("UserId", "Role") VALUES (NEW."UserId", 'ProductOwner') ON CONFLICT DO NOTHING;

    ELSIF v_roteiro = '2' THEN
        INSERT INTO "UserRoles" ("UserId", "Role") VALUES (NEW."UserId", 'GerenteProjeto'), (NEW."UserId", 'ProductOwner')
        ON CONFLICT DO NOTHING;
        PERFORM prumo_roteiro2_preparar(NEW."UserId");

    ELSIF v_roteiro = '3' THEN
        INSERT INTO "UserRoles" ("UserId", "Role") VALUES (NEW."UserId", 'GerenteProjeto') ON CONFLICT DO NOTHING;
        INSERT INTO "PortfolioMembers" ("PortfolioId", "UserId", "CreatedDate")
        SELECT p."Id", NEW."UserId", now()
        FROM "Portfolios" p
        WHERE p."Id" IN ('de200000-0000-4000-8000-000000000001', 'de200000-0000-4000-8000-000000000002')
        ON CONFLICT DO NOTHING;
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_prumo_roteiro_ao_entrar ON "TeamUsers";
CREATE TRIGGER trg_prumo_roteiro_ao_entrar
AFTER INSERT OR UPDATE OF "UserId" ON "TeamUsers"
FOR EACH ROW
WHEN (NEW."UserId" IS NOT NULL
      AND NEW."TeamId" IN ('de110000-0000-4000-8000-000000000001',
                           'de110000-0000-4000-8000-000000000002',
                           'de110000-0000-4000-8000-000000000003'))
EXECUTE FUNCTION prumo_roteiro_ao_entrar();

COMMIT;

-- Aviso se o Roteiro 3 ainda não tem os portfólios da carga de demonstração.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Portfolios" WHERE "Id" = 'de200000-0000-4000-8000-000000000001') THEN
        RAISE WARNING 'Portfólios de demonstração não encontrados: rode 01_seed_demo.sql para o Roteiro 3 ter dados.';
    END IF;
END $$;

-- Conferência
SELECT t."Name" AS time, t."InviteCode" AS codigo,
       (SELECT count(*) FROM "TeamUsers" tu WHERE tu."TeamId" = t."Id" AND tu."UserId" IS NOT NULL) AS participantes
FROM "Teams" t
WHERE t."Id"::text LIKE 'de11%'
ORDER BY t."Name";
