# Prumo — Testes automatizados (Robot Framework)

> Esta pasta fica dentro do repositório PrumoAPI. Rode os comandos abaixo a partir de `testes-robot/`.
> O workflow do CI está em `.github/workflows/robot-testes.yml`, na raiz do repositório.
> Relatórios da última execução: `evidencias/2026-10-27/` (abra `report.html` e `criterios_aceite.md`).

Implementação do **plano alternativo de validação** do TCC (Cap. 4, seção 4.9): a ferramenta é
avaliada sem participantes externos e sem dados pessoais, por suítes automatizadas que reproduzem
os roteiros das personas, conferem os cálculos com valores esperados calculados de forma
independente e medem os requisitos não funcionais.

Repositórios testados: [Prumo API](https://github.com/IvanNaka/PrumoAPI) (ASP.NET Core) e
[prumo_front](https://github.com/IvanNaka/prumo_front) (Angular).

## O que cada parte da seção 4.9 virou

| Seção 4.9 | Onde está |
|---|---|
| a) Suítes por persona (Tabela 20), SeleniumLibrary, tags com RF e UC | `tests/01` a `tests/07` |
| b) Autenticação de teste, só no ambiente de testes | `POST /api/auth/teste` na API (ver abaixo) + `resources/api.resource` |
| c) Verificação dos cálculos, tolerância 0,01, planilha de valores esperados | `oraculo/` + `tests/08_calculos.robot` + conferência na tela em `tests/07` |
| d) Integração com o Jira (importação, sem duplicar, intervalo, falha de autenticação) | `tests/06_qualidade_integracao.robot` |
| e) Compatibilidade, responsividade, desempenho, acessibilidade (axe-core) | `tests/09_nao_funcionais.robot` + matriz de navegadores no CI |
| f) Inspeção heurística | `docs/inspecao_heuristica.md` e `docs/inspecao_heuristica.csv` (manual) |
| Critérios de aceite (Tabela 21) e cobertura de requisitos | `scripts/criterios_aceite.py` |

### Suítes (Tabela 20)

| Suíte | Persona | Requisitos | Roteiro |
|---|---|---|---|
| `01_controle_acesso` | Todos os perfis | RF01–RF03, UC1 | Cap. 3 |
| `02_configuracao_portfolio` | Product Owner | RF04–RF09, RF14–RF17, UC2–UC6, UC8, UC9 | 4.4.1 |
| `03_gestao_projetos` | Gerente de Projetos | RF10–RF13, RF22–RF24, RF43, RF44, UC7 | 4.4.2 |
| `04_priorizacao` | Product Owner | RF18–RF21, UC10 | 4.4.2 |
| `05_operacao_tecnica` | Tech Lead | RF26–RF32, UC12, UC13 | 4.6.2 |
| `06_qualidade_integracao` | QA e Desenvolvedor | RF33, RF34, RF47, RF51, UC14–UC16 | 4.4.3 e 4.6.2 |
| `07_analise_estrategica` | Diretoria | RF35–RF42, RF45, RF46, UC11 | 4.4.3 e 4.6.3 |
| `08_calculos` | — | F1, F2, F4–F13 nos dois portfólios da massa | 4.9 c |
| `09_nao_funcionais` | — | Responsividade, desempenho, acessibilidade | 4.9 e |

Escopo: RF01–RF47 e RF51. RF48–RF50 (Azure DevOps, GitHub, Trello) estão como "versão futura" no
`CONFORMIDADE.md` da API e ficam fora da cobertura.

Tags úteis: `RFxx`, `UCxx`, `persona:<Perfil>`, `ui`, `api`, `calculo`, `permissoes`, `critico`,
`smoke`, `jira`, `lento`, `rnf:responsividade`, `rnf:desempenho`, `rnf:acessibilidade`.

## Pré-requisitos do ambiente de testes

A suíte roda contra um ambiente de **homologação** (seção 4.3.5) com:

1. **Massa de dados sintéticos** (seção 4.3): suba a API com `PRUMO_SEED_VALIDACAO=true`. O
   `SeedValidacao` cria as contas simuladas (uma por persona), os dois portfólios, 12 projetos,
   OKRs, orçamentos, business cases, dependências, equipes e issues/worklogs.
2. **Login de teste** (seção 4.9 b): o Google bloqueia login em navegadores automatizados, então a
   API expõe `POST /api/auth/teste`, que emite o JWT de uma conta já cadastrada. A rota só existe com
   `AuthTeste__Habilitado=true`, fora do ambiente `Production` e com o cabeçalho
   `X-Auth-Teste-Chave` igual a `AuthTeste__Chave` (16+ caracteres). Fora disso responde 404.
   O login real com Google continua sendo verificado manualmente pela equipe.
3. Opcional: `Admin__Email` (o administrador semeado), usado nos testes de gestão de usuários.

Exemplo para subir a API local:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=prumo;Username=postgres;Password=postgres"
export Jwt__Key="uma-chave-local-com-mais-de-32-caracteres"
export Admin__Email="admin@prumo.test" PRUMO_SEED_VALIDACAO=true
export AuthTeste__Habilitado=true AuthTeste__Chave="chave-local-do-login-de-teste"
dotnet ef database update -p Prumo.Infrastructure -s Prumo.API
dotnet run --project Prumo.API --urls http://localhost:5150
```

E o front apontando para ela (`apiUrl: 'http://localhost:5150/api'` em
`src/environments/environment.ts`), com `npm start`.

> Os testes criam portfólios, projetos, equipes e usuários com nomes únicos e **não alteram** os
> dois portfólios da massa, cujos indicadores são conferidos. A suíte 06 reconfigura a integração
> com o Jira do ambiente. Nunca rode contra produção.

## Instalação

Python 3.10+, Node.js 18+ (axe-core) e o navegador desejado (Chrome, Firefox ou Edge). O Selenium
Manager baixa o driver automaticamente.

```bash
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
npm ci
```

## Variáveis

| Variável | Padrão | Descrição |
|---|---|---|
| `PRUMO_API_URL` | `http://localhost:5150` | API (sem `/api`) |
| `PRUMO_FRONT_URL` | `http://localhost:4200` | Front |
| `PRUMO_AUTH_TESTE_CHAVE` | — | Mesma `AuthTeste:Chave` da API (**obrigatória**) |
| `PRUMO_EMAIL_<PERFIL>` | e-mails do `SeedValidacao` | Contas simuladas (`DESENVOLVEDOR`, `QA`, `PRODUCTOWNER`, `TECHLEAD`, `GERENTEPROJETO`, `DIRETORIA`, `VALIDACAO`) |
| `PRUMO_EMAIL_ADMIN` | `admin@prumo.test` | Administrador (`Admin:Email` da API) |
| `PRUMO_JWT_KEY` | — | Opcional: `Jwt:Key`, só para o teste de token expirado |
| `PRUMO_JIRA_URL`, `PRUMO_JIRA_EMAIL`, `PRUMO_JIRA_TOKEN`, `PRUMO_JIRA_PROJETO` | — | Projeto de testes no Jira Cloud (seção 4.3.3); sem elas, a sincronização real é pulada |
| `BROWSER` | `chrome` | `chrome`, `firefox` ou `edge` |
| `HEADLESS` | `true` | `false` para ver o navegador |
| `BROWSER_BINARY`, `DRIVER_PATH` | — | Navegador ou driver fora do padrão |
| `LIMITE_DASHBOARD_SEGUNDOS` | `3` | Critério de desempenho (Tabela 21) |

## Execução

```bash
export PRUMO_AUTH_TESTE_CHAVE="chave-local-do-login-de-teste"
robot -d results tests                          # tudo
robot -d results --include persona:Diretoria tests
robot -d results --include calculo tests        # só os cálculos
BROWSER=firefox robot -d results/firefox tests  # compatibilidade
python scripts/criterios_aceite.py results/output.xml docs/inspecao_heuristica.csv
```

## Evidências geradas em `results/`

| Arquivo | Conteúdo |
|---|---|
| `report.html`, `log.html`, `output.xml` | Relatórios do Robot Framework, com capturas de tela das falhas |
| `valores_esperados.xlsx` / `.csv` | Planilha de valores esperados × obtidos (API e tela), diferença e resultado |
| `desempenho.csv` | Tempos medidos (dashboard na tela e na API, priorização) |
| `acessibilidade/*.json` | Violações do axe-core por tela (WCAG 2.1 A/AA) |
| `cobertura_requisitos.csv` | RF/UC × casos de teste e resultado |
| `criterios_aceite.md` | Tabela 21 preenchida com o resultado da execução |

## Como os valores esperados são calculados

`oraculo/massa.py` descreve a massa da seção 4.3 (é a "entrada da planilha") e
`oraculo/calculos.py` implementa as fórmulas do Cap. 3 sem importar nada da API. Como a massa usa
datas relativas ao dia em que foi carregada, o oráculo recebe esse dia (a `dataCriacao` do
portfólio de validação) e o dia da execução. A comparação aceita diferença de até 0,01.

Se o Jira real for sincronizado com um projeto da massa, os indicadores de fluxo mudam. Por isso a
suíte 06 liga o Jira a um projeto próprio, criado pelos testes.

## CI (GitHub Actions)

- Push e PR: `robot --dryrun` (sintaxe) e uma checagem do oráculo.
- **Actions → Robot Framework → Run workflow**: executa no ambiente de homologação em Chrome,
  Firefox e Edge (um de cada vez), publica a Tabela 21 no resumo da execução e as evidências como
  artefatos. Configure em *Settings → Secrets and variables → Actions*:
  - Variables: `PRUMO_API_URL`, `PRUMO_FRONT_URL`, `PRUMO_JIRA_URL`, `PRUMO_JIRA_EMAIL`, `PRUMO_JIRA_PROJETO`
  - Secrets: `PRUMO_AUTH_TESTE_CHAVE`, `PRUMO_JIRA_TOKEN`, `PRUMO_JWT_KEY` (opcional)

O Safari fica com verificação manual, como prevê a seção 4.9 e.
