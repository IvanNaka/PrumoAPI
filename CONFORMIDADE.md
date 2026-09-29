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
| T05 | CONCLUÍDA | Portfolio + Objetivo (`Goal`), `PortfolioObjective`; `PortfolioStateMachine` (tabela 3.4.2); transições automáticas nos serviços de critérios/projetos/priorização; D11 no GET; ações `/acoes/{aprovar\|reavaliar\|encerrar}` (GovernarPortfolio); membros (responsável/Admin); RN23; nome único entre não encerrados (409). Front: `/portfolios` (cards, RN05), formulário com responsável, página do portfólio com ações por status/perfil, confirmação de encerrar e aviso 'somente leitura'. Testes: `PortfolioTests` (7); o fluxo completo Criado→…→Encerrado passando pela priorização está em `PrioritizationTests.FluxoCompleto`. |
| T06 | CONCLUÍDA | Front: `PortfolioContextService` (`ativo`/`ativo$`, `portfolioAtivoId` no sessionStorage, restaurado após F5), clique no card faz GET /portfolios/{id} e abre `/portfolios/{id}/visao-geral` (projetos + critérios, UC3 passo 5), cabeçalho com portfólio ativo e 'Trocar', `portfolioGuard` nas rotas dependentes, RN06 limpa o portfólio ativo (guard e interceptor), menu com seção do portfólio ativo. Back: `GET /portfolios/{id}/criterios` e `/projetos` (membro). Testes: `portfolio.guard.spec` (F5 e redirecionamento), `PortfolioOverviewTests`. |
| T07 | CONCLUÍDA | Critério com descrição, tipo (Beneficio/Custo), peso numeric(5,2) em (0,10] e índice único (Portfólio, Nome); rotas `GET/POST /portfolios/{id}/criterios`, `PUT/DELETE /criterios/{id}` (EditarCriterios); RN07–RN11; critério novo leva projetos Priorizado/Aprovado a Reavaliado; peso/tipo alterado chama F3 (`PrioritizationService.RecalculateIfNeededAsync`). Como F3 depende da avaliação, esta tarefa também trouxe `Project.EvaluationStatus/CurrentScore/RankingPosition/LastPrioritizationDate/Priority`, a nota inteira 1–5 (`ProjectEvaluation.Score`, único por projeto+critério) e a `EvaluationStateMachine`. Migration `T07_Criterios` converte pesos (÷10) e notas (0–10 → 1–5). Front: tela `/portfolios/{id}/criterios`. Testes: `CriteriaTests` (12). |
| T08 | CONCLUÍDA | Projeto com DataInicio/DataFim (date), OrcamentoAprovado, CategoriaEstrategica, JiraProjectKey, DataConclusao; `ProjectStateMachine` (Fig. 26 + Cancelar, RN22); `GET /portfolios/{id}/projetos?status=&categoria=`, `GET /projetos/{id}` (detalhe com ações permitidas e notas por critério), `POST /portfolios/{id}/projetos`, `PUT /projetos/{id}` (bloqueado em Cancelado/Arquivado), `POST /projetos/{id}/status` (Finalizar preenche DataConclusao; chama F3). Migration `T08_Projetos` preenche datas/orçamento a partir do Budget. Front: lista com filtros, formulário (validador fim ≥ início, BRL, categorias com descrição) e detalhe com botões do `ACOES_POR_STATUS` + confirmação de Cancelar. Testes: `ProjectTests` (12). |
| T09 | CONCLUÍDA | `PrioritizationService`: `PUT /projetos/{id}/avaliacoes` (upsert, RN17, NaoAvaliado→Avaliando, chama F3), `POST /portfolios/{id}/priorizacao` (RN15, RN16, F1+F2, grava Score/Posição/DataUltimaPriorizacao, Figura 27), `GET /portfolios/{id}/ranking` ({ranking, naoAvaliados com critérios faltantes}), `POST /projetos/{id}/avaliacao/{aprovar\|rejeitar}` (RN29; aprovar leva Rascunho→Planejado). Endpoint auxiliar `GET /portfolios/{id}/avaliacoes` (matriz da aba Avaliar). A tabela `AvaliacaoProjeto` antiga já era por critério — foi migrada em T07 (sem descarte). Front: `/portfolios/{id}/priorizacao` (abas Avaliar e Ranking, legenda 1–5, salvar por linha, barra de score, aprovar/rejeitar, não avaliados, data da última priorização). Testes: `PrioritizationCalculatorTests` (caso do plano = 80,00; empate Alta > Média) e `PrioritizationTests` (fluxo completo da Figura 27, RF21, RN15–RN17, RN29, Reavaliado→Priorizado). |
| T10 | CONCLUÍDA | Okr (título 200, datas opcionais) e KeyResult (descrição 300, meta > 0, valor ≥ 0) com navegações; `GET /okrs` (progresso F4 por OKR e KR), `POST /okrs` (RN13, transação única), `PUT /okrs/{id}` (substitui a lista de KRs, mantém ≥ 1), `PUT /key-results/{id}` ({valorAtual}); migration `T10_Okrs`. Front: `/okrs` (barras de progresso, expandir KRs, FormArray começando com 1 KR, 'Atualizar valor'). Testes: `OkrProgressTests` (caso 75%), `OkrTests` (4), `okrs.spec`. |
| T11 | CONCLUÍDA | `POST/DELETE /projetos/{id}/okrs/{okrId}` e `GET/POST/DELETE /portfolios/{id}/okrs[/{okrId}]` (EditarOkrs na escrita), RN14 (404), associação repetida idempotente (204 sem duplicar); `GET /projetos/{id}` passa a trazer os OKRs com progresso. Front: aba 'OKRs' no detalhe do projeto e seção 'OKRs do portfólio' na visão geral (componente `OkrAssociacoes`). A verificação 'projeto sem OKR aparece como desalinhado' é feita em T19 (F11). Testes: `OkrAssociationTests` (3). |
| T12 | CONCLUÍDA | `BudgetExpense` (LancamentoFinanceiro) passa a apontar para o projeto (ProjectId), data `date`, descrição 300; `Budget` mantido 1:1 com `TotalAmount == ApprovedBudget` (sincronizado ao criar/editar projeto) e sem colunas de valor consumido/burn rate/VPL; `GET/POST /projetos/{id}/lancamentos` (VerFinanceiro/EditarFinanceiro), `DELETE /lancamentos/{id}`, RN28; `BurnRateCalculator` (F5 exato, recebe `hoje`; `CalcularProjeto`/`CalcularPortfolio`) e `GET /projetos/{id}/indicadores` com `burnRate`. O custo de horas usa worklogs (T17) × custo/hora dos membros (T15) via `IndicatorDataLoader`. `BudgetsController`/`BudgetService` antigos (fórmulas próprias) removidos. Front: aba 'Orçamento' (6 cartões, alertas vermelho/amarelo, tabela e 'Novo lançamento'). Testes: `BurnRateCalculatorTests` (caso do plano: 10.000/30%/90.000) e `FinanceTests`. |
| T13 | CONCLUÍDA | Entidades `BusinessCase` (1 por projeto; investimento ≥ 0; taxa anual 0–100%), `CashFlowForecast` (mês ≥ 1 único) e `RealizedReturn`; `GET/PUT /projetos/{id}/business-case` (PUT substitui os fluxos), `GET/POST /projetos/{id}/retornos`; `VplCalculator` (F6) e `vpl` em `/projetos/{id}/indicadores` (sem business case → disponivel=false). Migration `T13_BusinessCase`. Front: aba 'Business case' (cartões VPL, tabela editável de fluxos, retornos). Testes: `VplCalculatorTests` (caso do plano = 182,24) e `BusinessCaseTests`. Também corrigido: filhos com Id pré-gerado são adicionados pelo DbSet (evita DbUpdateConcurrencyException) e o `ApiFactory` assina tokens com a chave efetiva da API. |
| T14 | CONCLUÍDA | `ProjectDependency` com descrição opcional (500) e índice único (origem, destino); `POST /dependencias` (EditarDependencias; RN20, RN21, F14 → RN19; origem e destino no mesmo portfólio), `DELETE /dependencias/{id}`, `GET /portfolios/{id}/dependencias` com `emRisco`/`motivo` (F13); `GET /projetos/{id}` traz as dependências (auxiliar: `GET /projetos/{id}/dependencias`). Migration `T14_Dependencias` remove auto-dependências e duplicadas. Front: `/portfolios/{id}/dependencias` (tabela Origem → Destino com status e risco) e aba 'Dependências' no projeto (duas listas, badge 'Em risco' com o motivo no tooltip). Grafo visual não feito (não há biblioteca de grafos). Testes: `DependencyTests` (F13/F14), `DependencyApiTests` (A→B, B→C, C→A = RN19; suspender B deixa A→B em risco). |
| T15 | CONCLUÍDA | `Team` (nome 100 único, PortfolioId opcional) e `TeamUser` como MembroEquipe (Id próprio, UserId opcional, nome, e-mail, custo/hora ≥ 0, capacidade 1–300 h); `/api/equipes[/{id}]` e `/api/equipes/{id}/membros[/{membroId}]` (EditarEquipes na escrita), `GET /equipes/{id}/capacidade?mes=AAAA-MM` (F9 por equipe e por membro; sem membros → disponivel=false/RN18). `CapacityCalculator` recebe o mês. Removidos: capacidade manual (`TeamCapacityEntry`) e código de convite. Migration `T15_Equipes` converte os membros antigos (nome/e-mail do usuário, capacidade do último lançamento ou 160 h, custo 0). Front: `/equipes` e `/equipes/{id}/capacidade` (seletor de mês, barras coloridas pela classificação). Testes: `CapacityCalculatorTests` (160 h/200 h → 125% Sobrecarregada), `TeamTests`, `capacidade.spec`. |
| T16 | CONCLUÍDA | Integration refeita (Url, Email, token criptografado via Data Protection com chaves no banco, intervalo 15–1440, Status da máquina de estados 3.4, tentativas/próxima tentativa, logs). /api/integracoes/jira GET/PUT/testar/logs; PUT testa automaticamente. IIntegrationProvider + JiraProvider (GET /rest/api/3/myself). Prumo.Functions e ExternalData removidos. Tela /integracoes/jira reescrita. |
| T17 | CONCLUÍDA | Entidades Issue/Worklog (tabelas Issues/Worklogs, IdExterno único, Origem), JiraProvider com /search/jql (nextPageToken) e fallback /search (startAt), worklogs paginados; IntegrationSyncService (upsert, estados 3.4.4, RN25 5 min × tentativas, RN26 401→ErroConexao, log sempre); ScheduledSyncService (PeriodicTimer 1 min + SemaphoreSlim + fila manual); POST /sincronizar 202; indicadores F5/F9 passam a usar worklogs/issues. Front: Sincronizar agora com polling de 3 s e resumo do último log. |
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
- **Conversão de dados legados (T07):** pesos antigos eram percentuais (soma 100) e foram divididos por 10 (peso 0 virou 1); notas antigas (0 a 10) viraram 1 + nota×0,4 arredondado (0→1, 5→3, 10→5); critérios sem nome/duplicados receberam nome/sufixo. Confirmar se há dados reais que precisem de outra regra.
- **Ranking após critério novo (T07/F3):** projetos em Reavaliado sem todas as notas saem do ranking (ScoreAtual e PosicaoRanking nulos) até receberem as notas do novo critério.
- **Projetos antigos sem datas/categoria (T08):** a migration `T08_Projetos` usou as datas do Budget quando existiam; sem ele, início = data de criação e término = início + 6 meses; categoria padrão Run e orçamento 0. Revisar esses projetos na tela.
- **Burn rate do portfólio (F5):** a spec manda somar custo e orçamento e 'aplicar as mesmas fórmulas'; como os projetos têm prazos diferentes, o burn rate mensal e a projeção do portfólio são a soma dos valores de cada projeto (percentual, saldo e estouro usam os totais).
- **Dependência entre portfólios (T14):** a spec não diz se origem e destino podem ser de portfólios diferentes; como a listagem é por portfólio, o cadastro exige que os dois projetos sejam do mesmo portfólio.
- **Custo/hora de membro em mais de uma equipe (F5):** se o mesmo e-mail aparece em equipes com custos diferentes, o burn rate usa a média dos custos.
- **Membros migrados das equipes antigas (T15):** entraram com custo/hora 0 — o TechLead precisa preencher o custo para o F5 considerar as horas.
- **Divisão do Sincronizar (T17):** o `IIntegrationProvider` só lê dados do Jira (`GetIssuesAsync`, `GetWorklogsAsync`); o upsert, os estados, as novas tentativas e o `SincronizacaoLog` ficam em `IntegrationSyncService` (Application), para que outras ferramentas (D09) reaproveitem a mesma lógica.
- **Campo `updated` (T17):** incluído na lista de `fields` da busca, pois é o que indica se a issue mudou (e, portanto, se os worklogs precisam ser buscados de novo) e alimenta `UltimaAtualizacao`.
- **Nomes de tipo em português (T17):** além de Story/Task/Bug/Feature/Sub-task, o mapeamento aceita História, Tarefa e Subtarefa (Jira em pt-BR) e qualquer tipo com `subtask=true` vira Task. Epic e demais tipos são ignorados.
- **Próxima janela depois de 3 falhas (RN25, T17):** falhas 1 e 2 agendam nova tentativa em 5 e 10 minutos; a partir da 3ª, `ProximaTentativa = agora + IntervaloSincronizacaoMinutos`. HTTP 429 também é tratado como indisponibilidade.
- **Chave de projeto inválida (T17):** se o Jira responder 400/403/404 para um projeto, ele é pulado, a mensagem vai para `MensagemErro` do log e a sincronização dos demais continua (resultado: sucesso).
- **Worklogs apagados no Jira (T17):** ao buscar de novo os worklogs de uma issue alterada, os que não vêm mais do Jira são removidos do Prumo; `HorasRealizadas` é recalculada.
- **Sincronização interrompida (T17):** na subida da API, se a integração ficou em `Sincronizando` (processo encerrado no meio), ela volta para `FalhaSincronizacao` com nova tentativa imediata.
- **Aviso RN26 (T17):** até T21 (entidade Notificação) o aviso aos Administradores é registrado no log da aplicação (`IAdminNotifier`); T21 troca a implementação para gerar notificações.
