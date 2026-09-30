# Prumo API — Documentação de Endpoints

Base URL local (ver `launchSettings.json`): `https://localhost:{porta}/api`

Autenticação: JWT Bearer (`Authorization: Bearer {token}`), obtido via `POST /api/auth/google`.
A maioria dos endpoints que dependem do usuário logado extrai o `userId` do claim `NameIdentifier`/`sub` do token.

Todos os IDs são `Guid` (formato string `"00000000-0000-0000-0000-000000000000"`).
Respostas de erro seguem os códigos HTTP padrão: `400 BadRequest`, `401 Unauthorized`, `404 NotFound`, `409 Conflict`.

---

## Sumário

- [Auth](#auth)
- [Onboarding](#onboarding-primeiro-acesso)
- [Roles](#roles)
- [Teams (Times)](#teams-times)
- [Users (Usuários)](#users-usuários)
- [Portfolios](#portfolios)
- [Projects](#projects)
- [Priority Criteria](#priority-criteria)
- [Project Dependency](#project-dependency)
- [Budgets (Orçamento)](#budgets-orçamento)
- [Integrations (Integrações)](#integrations-integrações)
- [Autorização por Role](#autorização-por-role)

---

## Auth

### `POST /api/auth/google`
Autentica (ou cria) um usuário via ID Token do Google e retorna um JWT.

**Body:**
```json
{
  "idToken": "string (Google ID Token)"
}
```

**200 OK:**
```json
{ "token": "jwt-string" }
```

**400 BadRequest:** `idToken` ausente/vazio.

> O JWT retornado inclui um claim de role por perfil do usuário. Um e-mail ainda não cadastrado é criado no primeiro login **sem perfil** (ou como `Desenvolvedor`, se o e-mail já for membro de alguma equipe). Usuário sem perfil só acessa `GET /api/auth/me` e as rotas de `/api/onboarding`; as demais respondem `403` com "Entre em uma equipe ou crie uma para acessar o Prumo.".

---

## Onboarding (primeiro acesso)

Somente para usuários logados **sem perfil**; quem já tem perfil recebe `409` ("Você já participa do Prumo."). As duas rotas devolvem um novo login (`{ token, expiraEm, usuario }`) já com o perfil concedido.

### `POST /api/onboarding/equipes`
Cria a equipe `{ "nome": "string" }`; o usuário vira membro e recebe o perfil `Administrador`. `409` se o nome já existir.

### `POST /api/onboarding/entrar`
Entra na equipe do código de convite `{ "codigo": "string" }` (sem diferenciar maiúsculas); o usuário vira membro e recebe o perfil `Desenvolvedor`. `404` "Código de convite inválido.".

### `POST /api/equipes/{id}/codigo-convite`
(Policy `EditarEquipes`.) Gera um novo código de convite; o anterior deixa de valer. O campo `codigoConvite` das equipes só é preenchido para `Administrador` e `TechLead`.

---

## Roles

Roles fixas do sistema (enum `RoleName`): **Admin, PO, Gerente, Diretoria, TechLead, ScrumMaster, QA, DEV**.
Elas são semeadas automaticamente no banco na inicialização da API (idempotente — não duplica se já existirem).

### `GET /api/roles`
Lista todas as roles disponíveis (para popular o seletor de `roleId` ao criar/editar usuários).
**Requer autenticação.**

**200 OK:**
```json
[
  { "id": "guid", "name": "Admin" },
  { "id": "guid", "name": "PO" },
  { "id": "guid", "name": "Gerente" },
  { "id": "guid", "name": "Diretoria" },
  { "id": "guid", "name": "TechLead" },
  { "id": "guid", "name": "ScrumMaster" },
  { "id": "guid", "name": "QA" },
  { "id": "guid", "name": "DEV" }
]
```
**401 Unauthorized**

---

## Teams (Times)

CRUD completo de times, incluindo gerenciamento de membros (`TeamUser`).
Todos os endpoints exigem autenticação (JWT). Criação/edição/exclusão de time e gerenciamento de membros exigem que o usuário tenha uma das roles: **Admin, Gerente, Diretoria, TechLead, ScrumMaster**.

### `GET /api/teams/{id}`
Retorna um time por ID, incluindo dono e membros.

**200 OK:**
```json
{
  "id": "guid",
  "portfolioId": "guid",
  "portfolioName": "string",
  "name": "string",
  "ownerUserId": "guid|null",
  "ownerUserName": "string|null",
  "createdDate": "2026-08-24T00:00:00Z",
  "members": [
    {
      "userId": "guid",
      "userName": "string",
      "userEmail": "string",
      "addedDate": "2026-08-24T00:00:00Z"
    }
  ]
}
```
**404 NotFound:** time não existe.

### `GET /api/teams`
Lista todos os times (com membros e dono).

**200 OK:** `TeamDto[]` (mesmo shape acima).

### `GET /api/teams/portfolio/{portfolioId}`
Lista os times de um portfólio específico.

**200 OK:** `TeamDto[]`

### `POST /api/teams`
Cria um novo time. **Requer role: Admin, Gerente, Diretoria, TechLead ou ScrumMaster.**

**Body:**
```json
{
  "portfolioId": "guid",
  "name": "string",
  "ownerUserId": "guid|null"
}
```

**201 Created:** `TeamDto` (Location aponta para `GET /api/teams/{id}`).
**400 BadRequest:** payload inválido, `name` vazio, portfólio ou dono inexistente.
**401 Unauthorized / 403 Forbidden:** usuário não autenticado ou sem role permitida.

### `PUT /api/teams/{id}`
Edita nome e/ou dono do time. **Requer role: Admin, Gerente, Diretoria, TechLead ou ScrumMaster.**

**Body:**
```json
{
  "id": "guid",
  "name": "string",
  "ownerUserId": "guid|null"
}
```

**204 NoContent:** sucesso.
**400 BadRequest:** `id` do body diferente do da URL, ou dono inexistente.
**404 NotFound:** time não existe.

### `DELETE /api/teams/{id}`
Remove um time (e seus vínculos de membros). **Requer role: Admin, Gerente, Diretoria, TechLead ou ScrumMaster.**

**204 NoContent:** sucesso.
**404 NotFound:** time não existe.

### `POST /api/teams/{id}/members`
Adiciona uma pessoa (usuário) ao time. **Requer role: Admin, Gerente, Diretoria, TechLead ou ScrumMaster.**

**Body:**
```json
{
  "userId": "guid"
}
```

**200 OK:** `TeamDto` atualizado (já com o novo membro na lista `members`).
**404 NotFound:** time não encontrado.
**409 Conflict:** usuário não existe ou já é membro do time.

### `DELETE /api/teams/{id}/members/{userId}`
Remove uma pessoa do time. **Requer role: Admin, Gerente, Diretoria, TechLead ou ScrumMaster.**

**200 OK:** `TeamDto` atualizado (sem o membro removido).
**404 NotFound:** time não encontrado ou usuário não é membro do time.

---

## Users (Usuários)

CRUD completo de usuários. Todos os endpoints exigem autenticação. Criação/edição/exclusão exigem role **Admin**.

### `GET /api/users/{id}`
Retorna um usuário por ID (com role).

**200 OK:**
```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "roleId": "guid",
  "roleName": "string|null",
  "createdDate": "2026-08-24T00:00:00Z"
}
```
**404 NotFound:** usuário não existe.

### `GET /api/users`
Lista todos os usuários (com role).

**200 OK:** `UserDto[]`

### `POST /api/users`
Cria um novo usuário. **Requer role: Admin.**

**Body:**
```json
{
  "name": "string",
  "email": "string",
  "password": "string",
  "roleId": "guid"
}
```

**201 Created:** `UserDto` (Location aponta para `GET /api/users/{id}`).
**400 BadRequest:** `name`, `email` ou `password` vazios.
**409 Conflict:** já existe usuário com o mesmo email.

> A senha é armazenada com hash PBKDF2 (salt aleatório + 100k iterações); nunca é retornada nas respostas.

### `PUT /api/users/{id}`
Edita um usuário. `password` é opcional — envie apenas se quiser trocá-la. **Requer role: Admin.**

**Body:**
```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "roleId": "guid",
  "password": "string|null"
}
```

**204 NoContent:** sucesso.
**400 BadRequest:** `id` do body diferente do da URL.
**404 NotFound:** usuário não existe.
**409 Conflict:** email já usado por outro usuário.

### `DELETE /api/users/{id}`
Remove um usuário. **Requer role: Admin.**

**204 NoContent:** sucesso.
**404 NotFound:** usuário não existe.

---

## Portfolios

### `GET /api/portfolios/{id}`
**200 OK:**
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "ownerName": "string",
  "ownerId": "guid",
  "createdAt": "2026-08-24T00:00:00Z"
}
```
**404 NotFound**

### `GET /api/portfolios`
Lista portfólios do usuário autenticado (via JWT). **Requer autenticação.**

**200 OK:** `PortfolioDto[]`
**401 Unauthorized:** claim de usuário ausente/inválida.

### `POST /api/portfolios`
Cria portfólio para o usuário autenticado. **Requer autenticação.**

**Body:**
```json
{ "name": "string", "description": "string" }
```
> `ownerId` é preenchido automaticamente a partir do token.

**201 Created:** `PortfolioDto`
**400 BadRequest:** payload nulo.
**401 Unauthorized**

### `PUT /api/portfolios/{id}`
**Body:**
```json
{ "id": "guid", "name": "string", "description": "string" }
```
**204 NoContent** · **400 BadRequest** (id mismatch) · **404 NotFound**

### `DELETE /api/portfolios/{id}`
**204 NoContent** · **404 NotFound**

---

## Projects

### `GET /api/projects/{id}`
**200 OK:**
```json
{
  "id": "guid",
  "portfolioId": "guid",
  "portfolioName": "string",
  "name": "string",
  "description": "string",
  "status": "string (enum ProjectStatus)",
  "ownerId": "guid",
  "ownerName": "string",
  "createdDate": "2026-08-24T00:00:00Z",
  "projectEvaluations": [
    {
      "id": "guid",
      "priorityCriteriaId": "guid",
      "priorityCriteriaName": "string",
      "userId": "guid",
      "value": 0,
      "weight": 0
    }
  ]
}
```
**404 NotFound**

### `GET /api/projects`
Lista projetos do usuário autenticado. **Requer autenticação.**

**200 OK:** `ProjectDto[]`
**401 Unauthorized**

### `GET /api/projects/portfolio/{portfolioId}`
**200 OK:** `ProjectDto[]`

### `POST /api/projects`
Cria projeto (dono = usuário autenticado). **Requer autenticação.**

**Body:**
```json
{
  "portfolioId": "guid",
  "name": "string",
  "description": "string",
  "criteriaScores": [
    { "priorityCriteriaId": "guid", "value": 0 }
  ]
}
```
**201 Created:** `ProjectDto`
**400 BadRequest** · **401 Unauthorized**

### `PUT /api/projects/{id}`
**Body:** entidade `Project` completa (`id` deve bater com a URL).
**204 NoContent** · **400 BadRequest**

### `DELETE /api/projects/{id}`
**204 NoContent** · **404 NotFound**

---

## Priority Criteria

### `GET /api/prioritycriteria/{id}`
**200 OK:**
```json
{
  "id": "guid",
  "name": "string",
  "valueWeight": 0,
  "portfolioId": "guid",
  "userId": "guid"
}
```
**404 NotFound**

### `GET /api/prioritycriteria`
**200 OK:** `PriorityCriteriaDto[]`

### `GET /api/prioritycriteria/portfolio/{portfolioId}`
**200 OK:** `PriorityCriteriaDto[]`

### `GET /api/prioritycriteria/user/{userId}`
**200 OK:** `PriorityCriteriaDto[]`

### `POST /api/prioritycriteria`
Cria critério (userId = usuário autenticado). **Requer autenticação.**

**Body:**
```json
{ "name": "string", "valueWeight": 0, "portfolioId": "guid" }
```
**201 Created:** `PriorityCriteriaDto`
**400 BadRequest** · **401 Unauthorized**

### `PUT /api/prioritycriteria/{id}`
**Body:**
```json
{ "id": "guid", "name": "string", "valueWeight": 0 }
```
**204 NoContent** · **400 BadRequest** · **404 NotFound**

### `DELETE /api/prioritycriteria/{id}`
**204 NoContent** · **404 NotFound**

---

## Project Dependency

### `GET /api/projectdependency/{id}`
**200 OK:**
```json
{
  "id": "guid",
  "reason": "string",
  "projectId": "guid",
  "projectName": "string",
  "dependsOnProjectId": "guid",
  "dependsOnProjectName": "string",
  "userId": "guid",
  "userName": "string",
  "portfolioId": "guid",
  "portfolioName": "string"
}
```
**404 NotFound**

### `GET /api/projectdependency/portfolio/{portfolioId}`
**200 OK:** `ProjectDependencyDto[]`

### `POST /api/projectdependency`
**Requer autenticação.**

**Body:**
```json
{
  "reason": "string",
  "projectId": "guid",
  "dependsOnProjectId": "guid",
  "portfolioId": "guid",
  "userId": "guid"
}
```
**201 Created:** `ProjectDependencyDto`
**400 BadRequest** · **401 Unauthorized**

### `DELETE /api/projectdependency/{id}`
**204 NoContent** · **404 NotFound**

---

## Budgets (Orçamento)

Gestão de orçamento por projeto (US10, RF22-RF25): cadastro do orçamento aprovado
(1:1 com `Project`), registro de custos/despesas e cálculo on-demand de
Burn Rate e VPL (Valor Presente Líquido).

`category` (`BudgetExpenseCategory`): `Custo` (0) ou `Despesa` (1) — serializado como
string (mesma convenção de `ProjectStatus`/`IntegrationType`), mas também aceita o
valor numérico na requisição.

### `GET /api/budgets/{id}`
**200 OK:**
```json
{
  "id": "guid",
  "projectId": "guid",
  "projectName": "string",
  "totalAmount": 150000.00,
  "currency": "BRL",
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-12-31T00:00:00Z",
  "discountRateMonthly": 1.5,
  "expectedReturn": 400000.00,
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": null
}
```
**404 NotFound**

### `GET /api/budgets/project/{projectId}`
Consulta o orçamento de um projeto (usado pelo front para decidir entre criar ou editar).

**200 OK:** `BudgetDto` (mesmo shape acima)
**404 NotFound:** projeto ainda não possui orçamento cadastrado.

### `POST /api/budgets`
Cadastra o orçamento aprovado de um projeto (1:1).

**Body:**
```json
{
  "projectId": "guid",
  "totalAmount": 150000.00,
  "currency": "BRL",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "discountRateMonthly": 1.5,
  "expectedReturn": 400000.00
}
```
**201 Created:** `BudgetDto`
**400 BadRequest:** `totalAmount <= 0`, `startDate >= endDate` ou `projectId` inexistente.
**409 Conflict:** o projeto já possui orçamento cadastrado.

### `PUT /api/budgets/{id}`
Atualiza um orçamento existente (mesmos campos de criação, exceto `projectId`).

**Body:**
```json
{
  "totalAmount": 160000.00,
  "currency": "BRL",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "discountRateMonthly": 1.5,
  "expectedReturn": 420000.00
}
```
**204 NoContent** · **400 BadRequest** (`totalAmount <= 0` ou datas inválidas) · **404 NotFound**

### `GET /api/budgets/{budgetId}/expenses`
Lista os custos e despesas registrados para o orçamento.

**200 OK:**
```json
[
  {
    "id": "guid",
    "budgetId": "guid",
    "description": "Licença de software",
    "category": "Custo",
    "amount": 3200.00,
    "date": "2026-02-10T00:00:00Z",
    "createdAt": "2026-02-10T00:00:00Z"
  }
]
```
**404 NotFound:** orçamento não existe.

### `POST /api/budgets/{budgetId}/expenses`
Registra um novo custo ou despesa contra o orçamento.

**Body:**
```json
{
  "description": "Licença de software",
  "category": 0,
  "amount": 3200.00,
  "date": "2026-02-10"
}
```
**201 Created:** `BudgetExpenseDto`
**400 BadRequest:** `amount <= 0` ou `description` vazia.
**404 NotFound:** `budgetId` não existe.

### `DELETE /api/budgets/expenses/{expenseId}`
Remove um lançamento de custo/despesa (correção de lançamentos).

**204 NoContent** · **404 NotFound**

### `GET /api/budgets/{budgetId}/metrics`
Calcula, sem cache, o Burn Rate e o VPL do orçamento a partir do `Budget` e de
seus `BudgetExpense` atuais.

**200 OK:**
```json
{
  "budgetId": "guid",
  "totalBudget": 150000.00,
  "totalSpent": 32000.00,
  "remainingBudget": 118000.00,
  "percentSpent": 21,
  "netPresentValue": 250000.00,
  "burnRateMonthly": 8000.00,
  "burnRatePercent": 5,
  "monthsElapsed": 4,
  "monthsRemaining": 8,
  "projectedDepletionDate": "2027-03-01T00:00:00Z",
  "isOverBudgetRisk": false
}
```
**404 NotFound:** orçamento não existe.

---

## Integrations (Integrações)

Interface genérica de integração com ferramentas externas de gestão de projetos (Jira, Azure DevOps, GitHub, Trello — RF47 a RF50), usada para trazer issues, worklogs e estimativas que alimentam os indicadores do dashboard (Lead Time, Burn Rate, Bugs vs Features etc.). Cada ferramenta é implementada por um provider que segue o mesmo contrato (`IIntegrationProvider`), permitindo adicionar novas ferramentas sem alterar o restante do sistema.

Todos os endpoints exigem autenticação; **configurar, editar, excluir e sincronizar manualmente exigem role `Admin`** (permissão administrativa, conforme especificado no caso de uso "Configurar Integração").

Tipos de integração (`type`): `Jira` (0), `AzureDevOps` (1), `GitHub` (2), `Trello` (3). **Atualmente há implementação de referência apenas para `Jira`** — as demais ferramentas usam o mesmo contrato e podem ser plugadas posteriormente; tentar configurar/sincronizar um tipo sem provider implementado retorna `400 BadRequest`.

Para o Jira, o campo `token` deve conter as credenciais no formato `email:apiToken` (par usado na autenticação Basic da API do Jira Cloud).

### `GET /api/integrations`
**Requer autenticação.**
**200 OK:**
```json
[
  {
    "id": "guid",
    "type": "Jira",
    "apiUrl": "string",
    "isActive": true,
    "syncIntervalMinutes": 60,
    "lastSyncedAt": "2026-01-01T00:00:00Z",
    "lastSyncStatus": "Success"
  }
]
```

### `GET /api/integrations/{id}`
**200 OK:** `IntegrationDto` (ver acima) · **404 NotFound**

### `POST /api/integrations`
**Requer role `Admin`.** Valida a conexão com a ferramenta externa antes de salvar (caso de uso "Configurar Integração").

**Body:**
```json
{
  "type": "Jira",
  "apiUrl": "https://minhaempresa.atlassian.net",
  "token": "email@empresa.com:apiTokenDoJira",
  "syncIntervalMinutes": 60
}
```
**201 Created:** `IntegrationDto`
**400 BadRequest** (payload inválido ou tipo sem provider implementado) · **401 Unauthorized** (credenciais rejeitadas pela ferramenta externa) · **403 Forbidden** (sem role `Admin`)

### `PUT /api/integrations/{id}`
**Requer role `Admin`.**

**Body:**
```json
{
  "apiUrl": "string",
  "token": "string (opcional — só enviar ao trocar a credencial)",
  "isActive": true,
  "syncIntervalMinutes": 60
}
```
**200 OK:** `IntegrationDto` · **404 NotFound**

### `DELETE /api/integrations/{id}`
**Requer role `Admin`.**
**204 NoContent** · **404 NotFound**

### `POST /api/integrations/{id}/sync`
**Requer role `Admin`.** Dispara manualmente a sincronização (busca issues, worklogs e estimativas na ferramenta externa e atualiza os dados locais) — chamada síncrona, retorna o resultado imediatamente.

A sincronização **automática** (RF51) não roda mais dentro da API: ela é feita por um projeto separado, **`Prumo.Functions`** (Azure Functions, isolated worker), com dois gatilhos que trabalham em conjunto:

1. **Timer Trigger** (`IntegrationSyncTimerFunction`) — roda diariamente (`0 0 3 * * *`, 03:00 UTC), consulta quais integrações ativas estão com sincronização vencida (`SyncIntervalMinutes` desde `lastSyncedAt`) e publica uma mensagem por integração na fila `integration-sync-queue` (Azure Storage Queue).
2. **Queue Trigger** (`IntegrationSyncQueueFunction`) — disparado a cada mensagem da fila, executa a sincronização real daquela integração (mesma lógica do `POST /{id}/sync`). Erros de "API indisponível" fazem a mensagem voltar para a fila (retry automático, até 5 tentativas conforme `host.json`); erros de "token expirado" não tentam de novo — a integração fica marcada com `lastSyncStatus: "AuthenticationFailed"` até ser reconfigurada.

Esse desenho decoupla o agendamento diário do processamento (que pode ser lento/instável por depender de APIs externas), e permite também enfileirar sincronizações avulsas (fora do horário diário) publicando diretamente na fila.

**200 OK:**
```json
{
  "integrationId": "guid",
  "syncedAtUtc": "2026-01-01T00:00:00Z",
  "projects": [{ "externalId": "string", "key": "string", "name": "string" }],
  "issues": [
    {
      "externalId": "PROJ-123",
      "key": "PROJ-123",
      "title": "string",
      "type": "Story",
      "status": "string",
      "projectExternalId": "string",
      "assigneeName": "string",
      "estimateHours": 8.0,
      "createdAt": "2026-01-01T00:00:00Z",
      "resolvedAt": null
    }
  ],
  "worklogs": [
    { "issueExternalId": "PROJ-123", "userName": "string", "hoursSpent": 2.5, "loggedAt": "2026-01-01T00:00:00Z" }
  ],
  "issuesImported": 10,
  "worklogsImported": 25
}
```
**401 Unauthorized** (token expirado/inválido na ferramenta externa — é necessário reconfigurar a integração) · **502 BadGateway** (API externa indisponível — o sistema tentará novamente no próximo ciclo automático) · **404 NotFound** (integração inexistente)

---



O JWT emitido em `POST /api/auth/google` carrega o claim de role do usuário. Os endpoints abaixo verificam a role via `[Authorize(Roles = "...")]`:

| Ação | Roles permitidas |
|---|---|
| Criar/editar/excluir Time | Admin, Gerente, Diretoria, TechLead, ScrumMaster |
| Adicionar/remover membro do Time | Admin, Gerente, Diretoria, TechLead, ScrumMaster |
| Criar/editar/excluir Usuário | Admin |
| Configurar/editar/excluir/sincronizar Integração | Admin |
| Consultas (GET) de Times, Usuários, Roles e Integrações | Qualquer usuário autenticado |

Sem o header `Authorization: Bearer {token}` válido → `401 Unauthorized`.
Com token válido mas role não autorizada para a ação → `403 Forbidden`.

Roles disponíveis: `Admin`, `PO`, `Gerente`, `Diretoria`, `TechLead`, `ScrumMaster`, `QA`, `DEV`.

---

## Fluxo sugerido no front para Times

1. `GET /api/portfolios` → escolher portfólio.
2. `GET /api/users` → listar usuários disponíveis para compor o time.
3. `POST /api/teams` → criar time vinculado ao portfólio (e opcionalmente definir dono).
4. `POST /api/teams/{id}/members` → adicionar cada pessoa ao time.
5. `DELETE /api/teams/{id}/members/{userId}` → remover pessoa do time.
6. `PUT /api/teams/{id}` → editar nome/dono do time.
7. `DELETE /api/teams/{id}` → excluir o time.
