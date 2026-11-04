"""Cobertura de requisitos e critérios de aceite do plano alternativo (Tabela 21).

Uso:  python scripts/criterios_aceite.py results/output.xml [docs/inspecao_heuristica.csv]

Gera, na pasta do output.xml:
  - cobertura_requisitos.csv  (RF/UC → casos de teste e resultado)
  - criterios_aceite.md       (Tabela 21 preenchida com os resultados da execução)
"""
import csv
import os
import re
import sys
from collections import defaultdict

from robot.api import ExecutionResult, ResultVisitor

# Escopo (CONFORMIDADE.md): RF48 a RF50 são "futuro" (Azure DevOps, GitHub, Trello).
RF_ESCOPO = [f"RF{n:02d}" for n in list(range(1, 48)) + [51]]
UC_ESCOPO = [f"UC{n}" for n in range(1, 17)]


class Coletor(ResultVisitor):
    def __init__(self):
        self.testes = []

    def visit_test(self, test):
        self.testes.append({"nome": test.name, "suite": test.parent.name, "status": test.status,
                            "tags": [str(t) for t in test.tags], "mensagem": test.message})


def ler_heuristica(caminho):
    if not caminho or not os.path.exists(caminho):
        return None
    with open(caminho, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f, delimiter=";"))


def ler_planilha(pasta):
    caminho = os.path.join(pasta, "valores_esperados.csv")
    if not os.path.exists(caminho):
        return None
    with open(caminho, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f, delimiter=";"))


def situacao(testes):
    executados = [t for t in testes if t["status"] in ("PASS", "FAIL")]
    if not executados:
        return "NÃO EXECUTADO", 0, 0
    aprovados = sum(1 for t in executados if t["status"] == "PASS")
    return ("ATENDIDO" if aprovados == len(executados) else "NÃO ATENDIDO"), aprovados, len(executados)


def main(saida, heuristica_csv=None):
    resultado = ExecutionResult(saida)
    coletor = Coletor()
    resultado.visit(coletor)
    testes = coletor.testes
    pasta = os.path.dirname(os.path.abspath(saida))

    por_req = defaultdict(list)
    for t in testes:
        for tag in t["tags"]:
            if re.fullmatch(r"(RF\d{2}|UC\d{1,2})", tag):
                por_req[tag].append(t)

    with open(os.path.join(pasta, "cobertura_requisitos.csv"), "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["requisito", "casos_de_teste", "aprovados", "falhos", "pulados", "testes"])
        for req in RF_ESCOPO + UC_ESCOPO:
            ts = por_req.get(req, [])
            w.writerow([req, len(ts), sum(t["status"] == "PASS" for t in ts), sum(t["status"] == "FAIL" for t in ts),
                        sum(t["status"] == "SKIP" for t in ts), " | ".join(t["nome"] for t in ts)])

    cobertos = [r for r in RF_ESCOPO if por_req.get(r)]
    sem_teste = [r for r in RF_ESCOPO if not por_req.get(r)]
    executados = [t for t in testes if t["status"] in ("PASS", "FAIL")]
    aprovados = [t for t in executados if t["status"] == "PASS"]
    pulados = [t for t in testes if t["status"] == "SKIP"]
    # Desempenho e acessibilidade têm linhas próprias na Tabela 21.
    funcionais = [t for t in executados if not any(tag.startswith("rnf:") for tag in t["tags"])]
    funcionais_ok = [t for t in funcionais if t["status"] == "PASS"]
    taxa = 100 * len(funcionais_ok) / len(funcionais) if funcionais else 0
    criticos_falhos = [t for t in funcionais if t["status"] == "FAIL" and "critico" in t["tags"]]
    todas_criticas = [t for t in testes if t["status"] == "FAIL" and "critico" in t["tags"]]

    linhas = []

    def criterio(dimensao, regra, status, evidencia):
        linhas.append((dimensao, regra, status, evidencia))

    criterio("Cobertura de requisitos", "100% dos RF do escopo com ao menos um caso de teste",
             "ATENDIDO" if not sem_teste else "NÃO ATENDIDO",
             f"{len(cobertos)}/{len(RF_ESCOPO)} RF cobertos" + (f"; sem teste: {', '.join(sem_teste)}" if sem_teste else ""))
    criterio("Testes funcionais", "≥ 95% aprovados e nenhuma falha crítica em aberto",
             "ATENDIDO" if taxa >= 95 and not criticos_falhos else "NÃO ATENDIDO",
             f"{len(funcionais_ok)}/{len(funcionais)} aprovados ({taxa:.1f}%); {len(criticos_falhos)} falha(s) crítica(s); "
             f"{sum(1 for t in pulados if not any(g.startswith('rnf:') for g in t['tags']))} pulado(s)")

    calc = [t for t in testes if "calculo" in t["tags"]]
    status, ok, n = situacao(calc)
    planilha = ler_planilha(pasta)
    extra = ""
    if planilha is not None:
        divergentes = [l for l in planilha if l["resultado"] != "OK"]
        extra = f"; planilha: {len(planilha) - len(divergentes)}/{len(planilha)} valores iguais (tolerância 0,01)"
        if divergentes:
            status = "NÃO ATENDIDO"
    criterio("Correção dos cálculos", "100% dos indicadores e do ranking iguais aos esperados (±0,01)", status,
             f"{ok}/{n} casos de cálculo aprovados{extra}")

    status, ok, n = situacao([t for t in testes if "permissoes" in t["tags"] or "suite:controle-acesso" in t["tags"]])
    criterio("Controle de acesso", "100% das permissões e restrições por perfil verificadas", status,
             f"{ok}/{n} casos de controle de acesso aprovados")

    status, ok, n = situacao([t for t in testes if "jira" in t["tags"]])
    criterio("Integração com o Jira", "Sincronização completa e sem duplicados em duas execuções", status,
             f"{ok}/{n} casos com o Jira de testes" + ("" if n else " (defina PRUMO_JIRA_* para executar)"))

    status, ok, n = situacao([t for t in testes if "rnf:desempenho" in t["tags"] and "ui" in t["tags"]])
    medicoes = os.path.join(pasta, "desempenho.csv")
    criterio("Desempenho", "Dashboard carregado em até 3 segundos com a massa de teste", status,
             f"{ok}/{n} medições dentro do limite" + (" (ver desempenho.csv)" if os.path.exists(medicoes) else ""))

    status, ok, n = situacao([t for t in testes if "rnf:acessibilidade" in t["tags"]])
    criterio("Acessibilidade", "Nenhuma violação crítica ou grave do axe-core nas telas principais", status,
             f"{ok}/{n} análises sem violações críticas/graves (ver acessibilidade/*.json)")

    heuristica = ler_heuristica(heuristica_csv)
    if heuristica is None:
        criterio("Usabilidade (inspeção)", "Nenhum problema de severidade 3 ou 4 sem correção", "NÃO EXECUTADO",
                 "lista consolidada da inspeção heurística não informada")
    else:
        graves = [h for h in heuristica if str(h.get("severidade", "")).strip() in ("3", "4")]
        abertos = [h for h in graves if h.get("status", "").strip().lower() != "corrigido"]
        criterio("Usabilidade (inspeção)", "Nenhum problema de severidade 3 ou 4 sem correção",
                 "ATENDIDO" if heuristica and not abertos else ("NÃO EXECUTADO" if not heuristica else "NÃO ATENDIDO"),
                 f"{len(heuristica)} problema(s) registrados; {len(graves)} de severidade 3/4; {len(abertos)} sem correção")

    with open(os.path.join(pasta, "criterios_aceite.md"), "w", encoding="utf-8") as f:
        f.write("# Critérios de aceite do plano alternativo (Tabela 21)\n\n")
        f.write(f"Execução: `{os.path.basename(saida)}` — {len(testes)} casos de teste "
                f"({len(aprovados)} aprovados, {len(executados) - len(aprovados)} falhos, {len(pulados)} pulados).\n\n")
        f.write("| Dimensão | Critério de aceite | Resultado | Evidência |\n|---|---|---|---|\n")
        for d, r, s, e in linhas:
            f.write(f"| {d} | {r} | **{s}** | {e} |\n")
        if todas_criticas:
            f.write("\n## Falhas críticas\n\n")
            for t in todas_criticas:
                f.write(f"- **{t['suite']} › {t['nome']}**: {t['mensagem'].splitlines()[0] if t['mensagem'] else ''}\n")
        f.write("\nCobertura detalhada por requisito: `cobertura_requisitos.csv`.\n")

    for d, r, s, e in linhas:
        print(f"{s:14} {d}: {e}")
    return 0


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    sys.exit(main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None))
