"""Análise automática de acessibilidade com axe-core (seção 4.9 e; referência WCAG 2.1 nível AA)."""
import json
import os

from robot.api import logger
from robot.api.deco import keyword, library
from robot.libraries.BuiltIn import BuiltIn

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AXE = os.getenv("AXE_CORE_PATH", os.path.join(RAIZ, "node_modules", "axe-core", "axe.min.js"))
REGRAS_WCAG = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]
IMPACTOS_BLOQUEANTES = ("critical", "serious")   # "crítico ou grave" (Tabela 21)


@library(scope="GLOBAL")
class Acessibilidade:
    @keyword
    def analisar_acessibilidade(self, nome_tela):
        """Roda o axe-core na página aberta, salva o resultado em JSON e devolve as violações."""
        if not os.path.exists(AXE):
            raise AssertionError(f"axe-core não encontrado em {AXE}. Rode 'npm install' na raiz do repositório.")
        driver = BuiltIn().get_library_instance("SeleniumLibrary").driver
        with open(AXE, encoding="utf-8") as f:
            driver.execute_script(f.read())
        resultado = driver.execute_async_script(
            "const pronto = arguments[arguments.length - 1];"
            "axe.run(document, {runOnly: {type: 'tag', values: arguments[0]}})"
            ".then(r => pronto({violations: r.violations, passes: r.passes.length}))"
            ".catch(e => pronto({erro: String(e)}));",
            REGRAS_WCAG,
        )
        if "erro" in resultado:
            raise AssertionError(f"axe-core falhou: {resultado['erro']}")
        violacoes = [
            {"regra": v["id"], "impacto": v["impact"], "descricao": v["help"], "ajuda": v["helpUrl"],
             "elementos": [n["target"] for n in v["nodes"]][:10], "quantidade": len(v["nodes"])}
            for v in resultado["violations"]
        ]
        pasta = os.path.join(BuiltIn().get_variable_value("${OUTPUT DIR}"), "acessibilidade")
        os.makedirs(pasta, exist_ok=True)
        with open(os.path.join(pasta, f"{nome_tela}.json"), "w", encoding="utf-8") as f:
            json.dump({"tela": nome_tela, "regrasAprovadas": resultado["passes"], "violacoes": violacoes},
                      f, ensure_ascii=False, indent=2)
        for v in violacoes:
            nivel = "WARN" if v["impacto"] in IMPACTOS_BLOQUEANTES else "INFO"
            logger.write(f"[{nome_tela}] {v['impacto']}: {v['regra']} — {v['descricao']} ({v['quantidade']} elemento(s))", nivel)
        return violacoes

    @keyword
    def nao_deve_haver_violacoes_criticas_ou_graves(self, violacoes):
        bloqueantes = [v for v in violacoes if v["impacto"] in IMPACTOS_BLOQUEANTES]
        if bloqueantes:
            linhas = [f"- {v['impacto']}: {v['regra']} ({v['quantidade']}): {v['descricao']} — {v['elementos'][:3]}"
                      for v in bloqueantes]
            raise AssertionError(f"{len(bloqueantes)} violação(ões) crítica(s)/grave(s) do axe-core:\n" + "\n".join(linhas))
