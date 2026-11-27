# Inspeção heurística de usabilidade (seção 4.9 f)

Substitui em parte a avaliação de usabilidade feita pelos estudantes. **Cada um dos três autores**
percorre as telas sozinho e registra os problemas encontrados. Depois, as três listas são
consolidadas em `inspecao_heuristica.csv`, que alimenta o critério "Usabilidade (inspeção)" da
Tabela 21 (`scripts/criterios_aceite.py`).

## Telas a inspecionar

Login · Boas-vindas · Portfólios · Visão geral · Critérios · Priorização (Avaliar e Ranking) ·
Projetos · Detalhe do projeto (todas as abas) · Dependências · OKRs · Equipes · Capacidade ·
Dashboard · Relatórios · Notificações · Integração Jira · Usuários.

## Heurísticas de Nielsen (NIELSEN; MACK, 1994)

| Código | Heurística |
|---|---|
| H1 | Visibilidade do status do sistema |
| H2 | Correspondência entre o sistema e o mundo real |
| H3 | Controle e liberdade do usuário |
| H4 | Consistência e padrões |
| H5 | Prevenção de erros |
| H6 | Reconhecimento em vez de memorização |
| H7 | Flexibilidade e eficiência de uso |
| H8 | Estética e design minimalista |
| H9 | Ajudar o usuário a reconhecer, diagnosticar e corrigir erros |
| H10 | Ajuda e documentação |

## Severidade (0 a 4)

| Grau | Significado |
|---|---|
| 0 | Não é um problema de usabilidade |
| 1 | Cosmético: corrigir se houver tempo |
| 2 | Menor: correção de baixa prioridade |
| 3 | Maior: correção importante, alta prioridade |
| 4 | Catástrofe: corrigir antes da entrega |

Problemas de severidade **3 ou 4** precisam estar com `status = Corrigido` na versão final.

## Como preencher `inspecao_heuristica.csv`

Separador `;`, uma linha por problema consolidado:

| Coluna | Conteúdo |
|---|---|
| `id` | P01, P02, … |
| `avaliador` | Iniciais dos autores que encontraram o problema (ex.: `IN, AB`) |
| `tela` | Tela onde ocorre |
| `problema` | Descrição objetiva |
| `heuristica` | H1 a H10 |
| `severidade` | 0 a 4 (média dos avaliadores, arredondada para cima) |
| `status` | `Aberto`, `Corrigido` ou `Não será corrigido` |
| `correcao` | Commit, PR ou descrição da correção |

Exemplo:

```
id;avaliador;tela;problema;heuristica;severidade;status;correcao
P01;IN;Priorização;Seletores de nota sem rótulo para leitores de tela;H4;3;Corrigido;prumo_front#123
```

> Limitação (seção 4.9.7): por ser feita pelos próprios desenvolvedores, a inspeção está sujeita a viés.
