# Controle de Conformidade

> Referência: `PLANO_CONFORMIDADE_PRUMO.md` × `BES_TCC_Prumo_3` (Cap. 3 e 4).
> Este arquivo existe (com o mesmo conteúdo) na raiz dos dois repositórios: **PrumoAPI** (back-end) e **prumo_front** (front-end).

## Mapa de nomes (nome no código → nome no documento)

Regra 4 do plano: os nomes já existentes (em inglês) foram mantidos; os tipos novos seguem o mesmo padrão em inglês e estão mapeados aqui. Os **valores** dos enums e o **contrato da API** (rotas e campos JSON) seguem exatamente o documento.

| Código | Documento |
|---|---|
| `User` | Usuario |
| `User.IsActive` | Usuario.Ativo |
| `UserRole` (UserId, Role) | UsuarioPerfil |
| `RoleName` (enum) | Role |
| `Portfolio.OwnerId` | Portfolio.ResponsavelId |
| `Portfolio.Goal` | Portfolio.Objetivo |
| `PortfolioMember` | PortfolioMembro |
| `PortfolioObjective` | PortfolioOkr |
| `PriorityCriteria` | CriterioPrioridade |
| `PriorityCriteria.ValueWeight` | CriterioPrioridade.Peso |
| `CriteriaType` (enum) | TipoCriterio |
| `Project` | Projeto |
| `Project.OwnerId` | Projeto.ResponsavelId |
| `Project.ApprovedBudget` | Projeto.OrcamentoAprovado |
| `Project.EvaluationStatus` | Projeto.StatusAvaliacao |
| `Project.CurrentScore` / `RankingPosition` / `LastPrioritizationDate` | ScoreAtual / PosicaoRanking / DataUltimaPriorizacao |
| `Project.CompletedAt` | Projeto.DataConclusao |
| `ProjectStatus` (enum) | ProjetoStatus |
| `EvaluationStatus` (enum) | AvaliacaoStatus |
| `Priority` (enum) | Prioridade |
| `StrategicCategory` (enum) | CategoriaEstrategica |
| `ProjectEvaluation` | AvaliacaoCriterio |
| `ProjectEvaluation.Score` | AvaliacaoCriterio.Nota |
| `Objective` | Okr |
| `KeyResult.Title` / `TargetValue` / `CurrentValue` | KeyResult.Descricao / Meta / ValorAtual |
| `ProjectObjective` | ProjetoOkr |
| `Budget` | Orcamento (1:1 com Projeto; `TotalAmount == ApprovedBudget`) |
| `BudgetExpense` | LancamentoFinanceiro |
| `BudgetExpenseCategory` (enum) | TipoLancamento |
| `BusinessCase` / `CashFlowForecast` / `RealizedReturn` | BusinessCase / FluxoCaixaPrevisto / RetornoRealizado |
| `ProjectDependency` (`ProjectId` → `DependsOnProjectId`) | DependenciaProjeto (ProjetoOrigemId → ProjetoDestinoId) |
| `Team` | Equipe |
| `TeamUser` | MembroEquipe |
| `ExternalIssue` / `ExternalWorklog` | Issue / Worklog |
| `ExternalIssueType` (enum) | TipoIssue |
| `Alert` | Notificacao |
| `AlertType` (enum) | TipoNotificacao |
| `AlertStatus` (enum) | NotificacaoStatus |
| `Integration` | IntegracaoConfig |
| `IntegrationStatus` (enum) | IntegracaoStatus |
| `IntegrationSyncLog` | SincronizacaoLog |
| `Report` | Relatorio |
| `BusinessRuleException` | RegraNegocioException |
| `ProjectStateMachine` / `PortfolioStateMachine` / `EvaluationStateMachine` / `IntegrationStateMachine` / `AlertStateMachine` | Máquinas de estado das Figs. 26–30 |
| `PrioritizationService` | PriorizacaoService |
| `NotificationService` / `NotificationRulesService` / `NotificationDispatcher` | NotificacaoService / RegrasNotificacaoService / NotificacaoDispatcher |
| `ScheduledSyncService` | SincronizacaoAgendadaService |
| `JiraIntegrationProvider` | JiraProvider |
| `ValidationSeeder` | SeedValidacao |
| `Startup.ConfigureServices` | `Program.cs` do plano (o projeto usa o padrão Program + Startup) |

## Inventário (Fase 0)

Levantamento feito no commit `848e93a` (back-end) e `12611b9` (front-end), antes de qualquer alteração.

### Enums
| Enum (spec 3.1) | Nome no código | Status | Diferença |
|---|---|---|---|
| Role | `RoleName` (+ entidade `Role`) | DIVERGENTE | Valores `Admin, PO, Gerente, Diretoria, TechLead, ScrumMaster, QA, DEV`; `ScrumMaster` sobra; nomes diferentes; usuário tem **um** perfil só (`User.RoleId`) |
| ProjetoStatus | `ProjectStatus` | DIVERGENTE | Só `Active, Paused, Completed` |
| PortfolioStatus | — | AUSENTE | |
| AvaliacaoStatus | — | AUSENTE | |
| Prioridade | — | AUSENTE | |
| CategoriaEstrategica | — | AUSENTE | |
| TipoCriterio | — | AUSENTE | |
| TipoNotificacao | `AlertType` | DIVERGENTE | Valores `Warning, Critical, Info` |
| NotificacaoStatus | — | AUSENTE | `Alert` usa `IsResolved: bool` |
| IntegracaoStatus | — | AUSENTE | `Integration.LastSyncStatus` é string livre |
| TipoIssue | `ExternalIssueType` | DIVERGENTE | Tem o valor extra `Other` |
| TipoLancamento | `BudgetExpenseCategory` | OK | `Custo, Despesa`, já gravado como string |

Enums gravados como inteiro no banco: `Alert.Type`, `Integration.Type`. Os demais já usavam `HasConversion<string>()`.

### Entidades
| Entidade (spec 3.2) | Nome no código | Status | Campos faltando | Campos sobrando | Tipos/regras diferentes |
|---|---|---|---|---|---|
| Usuario | `User` | DIVERGENTE | Ativo (login), Perfis (N) | `PasswordHash`, `RoleId` | e-mail sem índice único; nome 200 |
| UsuarioPerfil | `Role` (tabela) + `User.RoleId` | DIVERGENTE | relação N:N | — | um perfil por usuário |
| Portfolio | `Portfolio` | DIVERGENTE | Objetivo, Status, Membros, Okrs | `Teams`, `RoadmapItems` (navegação) | nome 300 sem unicidade; descrição 500 |
| CriterioPrioridade | `PriorityCriteria` | DIVERGENTE | Descricao, Tipo | `UserId` (criador) | peso numeric(18,2) em "%" com soma 100 (front); nome opcional (200); sem índice único |
| Projeto | `Project` | DIVERGENTE | DataInicio, DataFim, OrcamentoAprovado, CategoriaEstrategica, Prioridade, StatusAvaliacao, ScoreAtual, PosicaoRanking, DataUltimaPriorizacao, JiraProjectKey, DataConclusao | `ProjectMembers`, `Alerts` | nome 300 |
| Orcamento (Fig. 9) | `Budget` | DIVERGENTE | — | `Currency`, `StartDate`, `EndDate`, `DiscountRateMonthly`, `ExpectedReturn` | valor não amarrado ao projeto |
| AvaliacaoCriterio | `ProjectEvaluation` | DIVERGENTE | DataAvaliacao | — | `Value` decimal livre (spec: nota int 1–5); sem índice único (Projeto, Critério) |
| Okr | `Objective` | DIVERGENTE | KeyResults (navegação) | — | datas obrigatórias (spec: opcionais); título 500 |
| KeyResult | `KeyResult` | DIVERGENTE | — | — | descrição 500 (spec 300); sem validação Meta > 0 |
| ProjetoOkr | `ProjectObjective` | OK | — | — | chave composta já existe |
| PortfolioOkr | — | AUSENTE | | | |
| LancamentoFinanceiro | `BudgetExpense` | DIVERGENTE | ProjetoId | `BudgetId` | data sem validação de futuro; descrição 500 |
| BusinessCase / FluxoCaixaPrevisto / RetornoRealizado | — | AUSENTE | | | VPL calculado em `BudgetService` com fórmula própria |
| DependenciaProjeto | `ProjectDependency` | DIVERGENTE | — | `UserId`, `PortfolioId` | sem índice único; sem validação de ciclo/auto-dependência; `Reason` obrigatório (spec: opcional, 500) |
| Equipe | `Team` | DIVERGENTE | — | `InviteCode`, `OwnerUserId`, `CapacityEntries` | `PortfolioId` obrigatório (spec: opcional); nome sem unicidade |
| MembroEquipe | `TeamUser` | DIVERGENTE | Id, Nome, Email, CustoHora, CapacidadeMensalHoras | — | chave (TeamId, UserId); `UserId` obrigatório |
| Issue / Worklog | `ExternalData` (JSON bruto) | DIVERGENTE | tabelas estruturadas | `RawDataJson` | nada é consultável para os indicadores |
| Notificacao | `Alert` | DIVERGENTE | UsuarioId, EntidadeTipo/Id, Status, datas | `ProjectId`, `IsResolved` | tipo com valores errados |
| IntegracaoConfig | `Integration` | DIVERGENTE | Email, Status, TentativasFalhas, ProximaTentativa | `LastSyncStatus` | token gravado em texto puro |
| SincronizacaoLog | — | AUSENTE | | | |
| Relatorio | — | AUSENTE | | | |

### Endpoints
| Endpoint (spec 3.7) | Rota no código | Status | Diferença (verbo, corpo, resposta, perfil) |
|---|---|---|---|
| POST /auth/google | POST /api/Auth/google | DIVERGENTE | cria usuário automaticamente (spec: 403 se não cadastrado); resposta só `{token}`; sem RN01–RN03 |
| GET /auth/me | — | AUSENTE | |
| GET/POST/PUT /usuarios, PATCH /usuarios/{id}/ativo | /api/Users (CRUD) | DIVERGENTE | pede senha; sem policy (aberto); DELETE físico |
| GET /portfolios | GET /api/Portfolios | DIVERGENTE | filtra só por dono (spec: D11) |
| GET /portfolios/{id} | GET /api/Portfolios/{id} | DIVERGENTE | sem checagem de membro; sem contagens |
| POST /portfolios | POST /api/Portfolios | DIVERGENTE | sem objetivo/responsável; sem policy |
| PUT /portfolios/{id} | PUT /api/Portfolios/{id} | DIVERGENTE | sem policy/RN23 |
| /portfolios/{id}/membros | — | AUSENTE | |
| /portfolios/{id}/acoes/* | — | AUSENTE | |
| /portfolios/{id}/okrs | — | AUSENTE | |
| GET/POST /portfolios/{id}/criterios, PUT/DELETE /criterios/{id} | /api/PriorityCriteria/... | DIVERGENTE | rotas diferentes; sem RN07–RN11 |
| GET/POST /portfolios/{id}/projetos | GET /api/Projects/portfolio/{id}, POST /api/Projects | DIVERGENTE | sem filtros; sem validações |
| GET /projetos/{id} | GET /api/Projects/{id} | DIVERGENTE | sem detalhe completo |
| PUT /projetos/{id} | PUT /api/Projects/{id} | DIVERGENTE | recebe a entidade inteira |
| POST /projetos/{id}/status | — | AUSENTE | |
| PUT /projetos/{id}/avaliacoes | — (serviço sem controller) | AUSENTE | |
| POST /portfolios/{id}/priorizacao, GET /ranking, POST /avaliacao/{acao} | — | AUSENTE | |
| /okrs, /key-results, /projetos/{id}/okrs | — | AUSENTE | tabelas existem, sem API |
| /projetos/{id}/lancamentos, /lancamentos/{id} | /api/Budgets/{budgetId}/expenses | DIVERGENTE | ligado ao orçamento, não ao projeto; sem RN28 |
| /projetos/{id}/business-case, /retornos | — | AUSENTE | |
| GET /portfolios/{id}/dependencias, POST/DELETE /dependencias | /api/ProjectDependency/... | DIVERGENTE | sem F13/F14, RN19–RN21 |
| /equipes, /equipes/{id}/membros | /api/Teams/... | DIVERGENTE | membros são usuários; sem custo/capacidade |
| GET /equipes/{id}/capacidade | /api/TeamCapacities (lançamento manual) | DIVERGENTE | capacidade digitada, não calculada (F9) |
| /portfolios/{id}/dashboard, /indicadores/{nome}, /projetos/{id}/indicadores | — | AUSENTE | `/api/Budgets/{id}/metrics` calcula burn rate/VPL com fórmulas próprias |
| /notificacoes... | — | AUSENTE | |
| /integracoes/jira... | /api/Integrations (CRUD genérico + /sync) | DIVERGENTE | teste só na criação; sem status/logs; sync síncrona |
| /portfolios/{id}/relatorios/... | — | AUSENTE | |

### Telas (front-end)
| Tela (spec 4.x) | Componente/rota no código | Status | Diferença |
|---|---|---|---|
| /login (T02) | `Login` (/login) | DIVERGENTE | botão Google já existe; token em `localStorage`; sem mensagens RN01–RN03 |
| /admin/usuarios (T04) | — | AUSENTE | |
| /portfolios (T05) | `Portfolios` | DIVERGENTE | mistura lista + critérios; sem objetivo/responsável/status; filtra por times no front |
| /portfolios/{id}/visao-geral (T06) | — | AUSENTE | sem portfólio ativo / cabeçalho |
| /portfolios/{id}/criterios (T07) | dentro de `Portfolios` | DIVERGENTE | pesos em % com soma 100; sem descrição/tipo |
| /portfolios/{id}/projetos (T08) | `Projetos`, `CriarProjeto` | DIVERGENTE | campos da spec ausentes |
| /projetos/{id} (T20) | `DetalhesProjeto`, `OrcamentoProjeto` | DIVERGENTE | sem abas da spec |
| /portfolios/{id}/priorizacao (T09) | — | AUSENTE | |
| /okrs (T10) | `Okrs` (placeholder, fora do menu) | AUSENTE | |
| /portfolios/{id}/dependencias (T14) | `Dependencias` | DIVERGENTE | sem risco (F13) |
| /equipes + capacidade (T15) | `Times`, `CapacidadeEquipe` | DIVERGENTE | membros = usuários; capacidade manual |
| /integracoes/jira (T16) | `Integracoes` | DIVERGENTE | CRUD genérico; sem status/logs |
| /portfolios/{id}/dashboard (T19) | `Dashboard` | DIVERGENTE | **dados fictícios** (mock) no componente |
| /notificacoes + sino (T21) | — | AUSENTE | |
| /portfolios/{id}/relatorios (T22) | `Relatorios` (placeholder) | AUSENTE | |

### Itens EXTRA
| Item | Onde | Decisão (remover / esconder / manter e documentar) |
|---|---|---|
| Roadmap (`RoadmapItem`, `RoadmapsController`, tela `/roadmap`) | back + front | **Esconder** (D08): removido do menu e das rotas; código mantido |
| Senha (`PasswordHash`, `PasswordHasher`, senha em `CreateUserDto`) | back | **Remover** (D01) |
| Perfil `ScrumMaster` | `RoleName` | **Remover** (D02); usuários migrados para `TechLead` (ver dúvidas) |
| Cadastro automático de usuário no 1º login Google | `AuthService` | **Remover** (RN03) |
| Código de convite de equipe (`Team.InviteCode`, `POST /teams/join`) e guarda "precisa ter time" | back + front | **Esconder** da interface (T23); a regra de acesso passa a ser a de membro do portfólio (D11) |
| Capacidade manual (`TeamCapacityEntry`, `/api/TeamCapacities`) | back + front | **Remover**: substituída pelo cálculo F9 |
| Dados externos brutos (`ExternalData`) | back | **Remover**: substituído por `ExternalIssue`/`ExternalWorklog` |
| `CriteriaValue`, `ProjectMember` | Domain | `CriteriaValue` removida (sem uso); `ProjectMember` mantida sem API (ver dúvidas) |
| Azure Functions (`Prumo.Functions`) para sincronização | back | **Remover**: substituída pelo `BackgroundService` exigido em T17 (evita sincronização em dobro) |
| Tela "Configurações" (placeholder) | front | **Esconder** (sem RF) |
| Pacote `recharts` | front | Biblioteca React, inutilizável em Angular → removida; instalados `chart.js` + `ng2-charts` (T19) |

## Progresso das tarefas
| Tarefa | Status (PENDENTE/EM ANDAMENTO/CONCLUÍDA) | Observações |
|---|---|---|
| Fase 0 | CONCLUÍDA | inventário acima |
| T01 | CONCLUÍDA | 12 enums conforme 3.1, todos gravados como texto (convenção global no DbContext); migration `T01_Enums` converte dados antigos; `enums.ts`/`rotulos.ts` no front; teste `EnumsTests` |
| T02 | CONCLUÍDA | Login só Google (`POST /api/auth/google`, `GET /api/auth/me`), RN01–RN03 em ProblemDetails, JWT com claim `role` por perfil, `UserRoles` (D03), sem senha; migration `T02_RemoverSenha` (copia perfil antigo); seed do admin (`Admin:Email`); front: GSI no `index.html`, sessão no `sessionStorage`, interceptor e `authGuard`. Testes: `AuthTests` (6 casos) e `auth.service.spec`. Guarda de 'time obrigatório' removida (item EXTRA). |
| T03 | CONCLUÍDA | 13 policies da 3.6 (`Policies.Register`), RN27 via `IAuthorizationMiddlewareResultHandler`, `IPortfolioAccessService` (RN06/RN23; Admin e Diretoria veem tudo), `PortfolioMember` + `Portfolio.Status` (migration `T03_PortfolioMembros` inclui responsável e membros das equipes); ProblemDetails para toda exceção; front: `roleGuard`, diretiva `*temPerfil`, `permissoes.ts` e menu filtrado por perfil. Testes: `AccessControlTests`, `tem-perfil.directive.spec`. As policies de cada endpoint novo são aplicadas na tarefa que o cria. |
| T04 | CONCLUÍDA | `/api/usuarios` (GET/POST/PUT, PATCH `/ativo`) com policy GerirUsuarios; validações (e-mail válido e único — 409, ao menos 1 perfil, não desativar a si mesmo — 409); endpoint auxiliar `GET /api/usuarios/ativos` (qualquer autenticado) para os selects de responsável/membros; tela `/admin/usuarios` (tabela, novo/editar com 7 perfis, toggle ativo). Testes: `UsersTests` (6 casos, inclui 'desativado não loga'). |
| T05 | CONCLUÍDA | Portfolio + Objetivo (`Goal`), `PortfolioObjective`; `PortfolioStateMachine` (tabela 3.4.2); transições automáticas nos serviços de critérios/projetos; D11 no GET; ações `/acoes/{aprovar\|reavaliar\|encerrar}` (GovernarPortfolio); membros (responsável/Admin); RN23; nome único entre não encerrados (409). Front: `/portfolios` (cards, RN05), formulário com responsável, página do portfólio com ações por status/perfil, confirmação de encerrar e aviso 'somente leitura'. Testes: `PortfolioTests` (7). O fluxo completo com priorização é validado em T09. |
| T06 | PENDENTE | |
| T07 | PENDENTE | |
| T08 | PENDENTE | |
| T09 | PENDENTE | |
| T10 | PENDENTE | |
| T11 | PENDENTE | |
| T12 | PENDENTE | |
| T13 | PENDENTE | |
| T14 | PENDENTE | |
| T15 | PENDENTE | |
| T16 | PENDENTE | |
| T17 | PENDENTE | |
| T18 | PENDENTE | |
| T19 | PENDENTE | |
| T20 | PENDENTE | |
| T21 | PENDENTE | |
| T22 | PENDENTE | |
| T23 | PENDENTE | |
| T24 | PENDENTE | |
| T25 | PENDENTE | |
| T26 | PENDENTE | |
| T27 | PENDENTE | |

## Dúvidas em aberto
- **ScrumMaster (T01):** o perfil não existe no documento (D02). Na migration `T01_Enums` os usuários com esse perfil foram migrados para **TechLead** (perfil mais próximo: ambos podiam gerir equipes). Confirmar com a equipe.
- **Alertas antigos (T01):** a tabela `Alerts` (tipos Warning/Critical/Info) não era alimentada por nenhum código; os registros existentes foram descartados na migration `T01_Enums`.
- **Quem cria o portfólio (T05):** a spec só manda incluir o responsável como membro. O usuário que cadastra (PO/Gerente) também é incluído como membro, senão perderia o acesso ao portfólio que acabou de criar quando indica outra pessoa como responsável.
- **Critério cadastrado com projetos já existentes (T05):** se o portfólio passa para Configurado e já tem projetos, ele segue direto para EmAnalise (a condição 'há projeto cadastrado' já é verdadeira).
