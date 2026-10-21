# Dados de demonstração

Scripts PostgreSQL para popular o banco a partir de um único usuário já cadastrado e apresentar todas as páginas do Prumo num **cenário positivo**: projetos no prazo e dentro do orçamento, retorno acima do previsto e equipes com ocupação adequada.

| Arquivo | O que faz |
|---|---|
| `01_seed_demo.sql` | Dá o perfil **Administrador** ao seu usuário e cria 3 portfólios, 11 projetos, critérios (pesos somando 10) e avaliações com score e ranking, orçamento e lançamentos, business case com VPL positivo e retornos, OKRs perto da meta, dependências sem conflito, roadmap em dia, 3 equipes com código de convite e alocadas aos projetos, integração Jira com histórico de sincronizações, issues e worklogs, notificações e histórico de relatórios. Pode ser rodado de novo quantas vezes quiser. |
| `02_remover_demo.sql` | Remove tudo que a carga criou (IDs que começam com `de`), incluindo o que foi criado dentro dos portfólios de demonstração. Não mexe no seu usuário. |
| `03_limpar_todas_tabelas.sql` | **Apaga os dados de todas as tabelas**, não só os da demonstração. Mantém só `__EFMigrationsHistory` e `DataProtectionKeys`. Com `manter_usuarios := true` (no início do script), mantém também os usuários reais e seus perfis. Faça backup antes (`pg_dump`). |
| `04_roteiros_avaliacao.sql` | Prepara os **roteiros de avaliação** (Formulários 1, 2 e 3): cria os times com os códigos `ROTEIRO1`, `ROTEIRO2` e `ROTEIRO3` e um gatilho que, quando o participante entra no time, dá os perfis do roteiro e prepara os dados dele. |
| `05_resetar_participantes.sql` | Remove os participantes dos roteiros e o que eles criaram, para repetir a sessão com as mesmas contas Google. |

```bash
psql "<connection string>" -f scripts/demo/01_seed_demo.sql
```

Depois de rodar, **saia e entre de novo** na aplicação: os perfis vão no token JWT. Com o perfil Administrador você não cai na tela de boas-vindas.

O banco precisa estar atualizado até a migration `AddProjectTeams`.

Como os números foram calibrados com as fórmulas da API:
- **Orçamento:** o custo realizado (horas × custo/hora + lançamentos) fica em cerca de 88% do proporcional ao prazo decorrido, então não há estouro nem risco de estouro.
- **Saúde:** nenhum projeto em risco, atrasado ou com tarefa vencida, então a saúde fica "Saudável".
- **Capacidade:** a demanda das issues abertas é cerca de 82% da capacidade de cada membro, então a ocupação fica "Adequada".
- **Dependências:** o pré-requisito sempre termina antes de quem depende dele.

Observações:
- Com mais de um usuário no banco, troque o `NULL` do `COALESCE(NULL::text, ...)` no começo do script pelo seu e-mail. Sem isso, o script usa o usuário cadastrado há mais tempo.
- As datas são relativas ao dia em que o script roda.
- A integração Jira é criada **pausada** e com um token fictício, para o job de sincronização não tentar usá-la. Se você já tiver uma integração Jira, ela é mantida.
- Os colegas fictícios (`@prumo.demo`) não conseguem fazer login. Eles existem para preencher membros, equipes e a tela de usuários.

## Roteiros de avaliação

Ordem: `01_seed_demo.sql` (dados do Roteiro 3) e depois `04_roteiros_avaliacao.sql`. Cada participante entra com a própria conta Google e, na tela de boas-vindas, usa **Entrar em uma equipe** com o código do roteiro:

| Código | Formulário | Perfis concedidos | Dados |
|---|---|---|---|
| `ROTEIRO1` | 1 — Criação e configuração do portfólio | ProductOwner + Desenvolvedor | Começa vazio: o participante cria portfólio, critérios e OKR. |
| `ROTEIRO2` | 2 — Gerenciamento e priorização de projetos | GerenteProjeto + ProductOwner + Desenvolvedor | Portfólio próprio já priorizado: critérios com soma 10, um OKR, 3 projetos avaliados no ranking, equipe com capacidade e issues para os indicadores operacionais. |
| `ROTEIRO3` | 3 — Análise estratégica e dashboards | GerenteProjeto + Desenvolvedor | Membro dos portfólios "Transformação Digital" e "Eficiência Operacional" da carga de demonstração. |

Observações:
- O perfil Desenvolvedor vem do próprio onboarding da API. O gatilho só acrescenta os demais perfis.
- Cada participante precisa ser um usuário **novo**, sem perfil. Para repetir com as mesmas contas, rode `05_resetar_participantes.sql`.
- A integração Jira (Formulário 2, Atividade 10) é única no sistema: todos os participantes editam a mesma configuração.
- No Formulário 2, as Atividades 10 e 11 ainda estão com "(COLOCAR PASSO A PASSO)" no PDF.
