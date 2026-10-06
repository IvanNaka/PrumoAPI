-- =====================================================================================
-- Prumo — LIMPA TODAS AS TABELAS do banco (PostgreSQL)
--
-- ATENÇÃO: apaga TODOS os dados (não só os de demonstração) e não tem como desfazer.
-- Faça um backup antes se houver algo que precise ser mantido:
--     pg_dump "<connection string>" -Fc -f prumo_backup.dump
--
-- O que é mantido sempre:
--   * "__EFMigrationsHistory" — sem ela a API tentaria recriar o schema na próxima migração;
--   * "DataProtectionKeys"     — chaves de criptografia da API (tokens de integração, etc.).
--
-- Opção: manter_usuarios (abaixo)
--   * false (padrão): apaga também "Users" e "UserRoles". No próximo login Google o usuário é
--     recriado sem perfil e cai na tela de boas-vindas (ou rode 01_seed_demo.sql depois de
--     logar, que dá o perfil Administrador).
--   * true: mantém os usuários reais e seus perfis (remove só os fictícios da demo); apaga todo o resto.
--
-- Como usar:
--     psql "<connection string>" -f 03_limpar_todas_tabelas.sql
-- Depois, saia e entre de novo na aplicação.
-- =====================================================================================

DO $$
DECLARE
    manter_usuarios boolean := false;   -- troque para true para manter "Users" e "UserRoles"

    excluidas text[] := ARRAY['__EFMigrationsHistory', 'DataProtectionKeys'];
    tabelas   text;
BEGIN
    IF manter_usuarios THEN
        excluidas := excluidas || ARRAY['Users', 'UserRoles'];
    END IF;

    -- Todas as tabelas do schema public (inclui tabelas criadas por migrations futuras).
    SELECT string_agg(format('%I.%I', schemaname, tablename), ', ' ORDER BY tablename)
      INTO tabelas
      FROM pg_tables
     WHERE schemaname = 'public'
       AND tablename <> ALL (excluidas);

    IF tabelas IS NULL THEN
        RAISE NOTICE 'Nenhuma tabela para limpar.';
        RETURN;
    END IF;

    -- Sem CASCADE de propósito: se alguma tabela fora da lista referenciar uma das limpas,
    -- o comando falha em vez de apagar dados de uma tabela que deveria ser mantida.
    EXECUTE 'TRUNCATE TABLE ' || tabelas || ' RESTART IDENTITY';
    RAISE NOTICE 'Tabelas limpas: %', tabelas;

    -- Mantendo os usuários, remove só os colegas fictícios da carga de demonstração (@prumo.demo).
    IF manter_usuarios THEN
        DELETE FROM "Users" WHERE "Id"::text LIKE 'de%';
    END IF;
END $$;

-- Conferência: quantidade de linhas que sobrou em cada tabela.
SELECT table_name AS tabela,
       (xpath('/row/c/text()',
              query_to_xml(format('SELECT count(*) AS c FROM %I.%I', table_schema, table_name), false, true, '')))[1]::text::int AS linhas
FROM information_schema.tables
WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
ORDER BY table_name;
