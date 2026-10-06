# Dados de demonstração

Scripts PostgreSQL para popular o banco a partir de um único usuário já cadastrado e apresentar todas as páginas do Prumo num **cenário positivo**: projetos no prazo e dentro do orçamento, retorno acima do previsto e equipes com ocupação adequada.

| Arquivo | O que faz |
|---|---|
| `01_seed_demo.sql` | Dá o perfil **Administrador** ao seu usuário e cria 3 portfólios, 11 projetos, critérios (pesos somando 10) e avaliações com score e ranking, orçamento e lançamentos, business case com VPL positivo e retornos, OKRs perto da meta, dependências sem conflito, roadmap em dia, 3 equipes com código de convite e alocadas aos projetos, integração Jira com histórico de sincronizações, issues e worklogs, notificações e histórico de relatórios. Pode ser rodado de novo quantas vezes quiser. |
| `02_remover_demo.sql` | Remove tudo que a carga criou (IDs que começam com `de`), incluindo o que foi criado dentro dos portfólios de demonstração. Não mexe no seu usuário. |

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
