# Dados de demonstração

Scripts PostgreSQL para popular o banco a partir de um único usuário já cadastrado, para apresentar todas as páginas do Prumo.

| Arquivo | O que faz |
|---|---|
| `01_seed_demo.sql` | Dá o perfil **Administrador** ao seu usuário e cria 3 portfólios, 8 projetos, critérios e avaliações (com score e ranking já calculados), orçamento e lançamentos, business case e retornos, OKRs, dependências, roadmap, equipes, integração Jira com histórico, issues e worklogs, notificações e histórico de relatórios. Pode ser rodado de novo quantas vezes quiser. |
| `02_remover_demo.sql` | Remove tudo que a carga criou (IDs que começam com `de`), incluindo o que foi criado dentro dos portfólios de demonstração. Não mexe no seu usuário. |

```bash
psql "<connection string>" -f scripts/demo/01_seed_demo.sql
```

Depois de rodar, **saia e entre de novo** na aplicação: os perfis vão no token JWT.

Observações:
- Com mais de um usuário no banco, informe o seu e-mail no `COALESCE(NULL::text, ...)` do começo do script. Sem isso, o script usa o usuário cadastrado há mais tempo.
- As datas são relativas ao dia em que o script roda, então os indicadores do mês corrente sempre têm dados.
- A integração Jira é criada **pausada** e com um token fictício, para o job de sincronização não tentar usá-la. Se você já tiver uma integração Jira, ela é mantida.
- Os colegas fictícios (`@prumo.demo`) não conseguem fazer login. Eles existem para preencher membros, equipes e a tela de usuários.
