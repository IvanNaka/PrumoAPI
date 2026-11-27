# Critérios de aceite do plano alternativo (Tabela 21)

Execução: `output.xml` — 128 casos de teste (125 aprovados, 2 falhos, 1 pulados).

| Dimensão | Critério de aceite | Resultado | Evidência |
|---|---|---|---|
| Cobertura de requisitos | 100% dos RF do escopo com ao menos um caso de teste | **ATENDIDO** | 48/48 RF cobertos |
| Testes funcionais | ≥ 95% aprovados e nenhuma falha crítica em aberto | **ATENDIDO** | 121/122 aprovados (99.2%); 0 falha(s) crítica(s); 1 pulado(s) |
| Correção dos cálculos | 100% dos indicadores e do ranking iguais aos esperados (±0,01) | **ATENDIDO** | 45/45 casos de cálculo aprovados; planilha: 364/364 valores iguais (tolerância 0,01) |
| Controle de acesso | 100% das permissões e restrições por perfil verificadas | **ATENDIDO** | 25/25 casos de controle de acesso aprovados |
| Integração com o Jira | Sincronização completa e sem duplicados em duas execuções | **NÃO ATENDIDO** | 4/5 casos com o Jira de testes |
| Desempenho | Dashboard carregado em até 3 segundos com a massa de teste | **ATENDIDO** | 1/1 medições dentro do limite (ver desempenho.csv) |
| Acessibilidade | Nenhuma violação crítica ou grave do axe-core nas telas principais | **NÃO ATENDIDO** | 0/1 análises sem violações críticas/graves (ver acessibilidade/*.json) |
| Usabilidade (inspeção) | Nenhum problema de severidade 3 ou 4 sem correção | **NÃO EXECUTADO** | 0 problema(s) registrados; 0 de severidade 3/4; 0 sem correção |

## Falhas críticas

- **09 Nao Funcionais › Telas principais sem violações críticas ou graves de acessibilidade**: Telas com violações críticas/graves: ['portfolios', 'portfolios_p1_visao-geral', 'portfolios_p1_dashboard', 'portfolios_p1_priorizacao', 'portfolios_p1_projetos', 'okrs', 'equipes', 'notificacoes']

Cobertura detalhada por requisito: `cobertura_requisitos.csv`.
