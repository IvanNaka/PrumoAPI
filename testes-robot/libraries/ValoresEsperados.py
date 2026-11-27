"""Compara os cálculos da API e do dashboard com o oráculo independente (seção 4.9 c).

Cada comparação vira uma linha da planilha de evidências (results/valores_esperados.xlsx e .csv):
indicador, campo, valor esperado, valor obtido, diferença e resultado.
"""
import csv
import os
import sys
from datetime import date, datetime, timezone

from robot.api import logger
from robot.api.deco import keyword, library
from robot.libraries.BuiltIn import BuiltIn

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
from oraculo import massa  # noqa: E402
from oraculo.calculos import Cenario, burn_rate_basico, ranking_f2, score_f1  # noqa: E402

TOLERANCIA = 0.01

# Campos numéricos conferidos em cada indicador do dashboard (chave na resposta da API).
CAMPOS = {
    "burnRate": ["orcamentoAprovado", "custoHoras", "custoLancamentos", "custoRealizado", "burnRateMensal",
                 "projecaoCustoTotal", "saldo", "percentualConsumido"],
    "saude": ["saude"],
    "qualidade": ["bugs", "entregas", "razaoBugsPorEntrega", "indiceQualidade"],
    "capacidade": ["capacidade", "demanda", "horasMes", "ocupacao", "utilizacao"],
    "leadTime": ["quantidade", "leadTimeMediano", "leadTimeMedio", "p85"],
    "vpl": ["vplEsperado", "vplRealizado", "diferenca", "percentualAtingimento"],
    "alinhamentoOkr": ["totalProjetos", "projetosAlinhados", "percentualProjetosAlinhados", "percentualOrcamentoAlinhado"],
    "alocacaoEstrategica": ["orcamentoTotal"],
}


@library(scope="GLOBAL")
class ValoresEsperados:
    def __init__(self):
        self.linhas = []
        self.cenario = None

    # ---------- preparação ----------
    @keyword
    def preparar_oraculo(self, dia_carga, hoje=None, **emails):
        """``dia_carga``: data (AAAA-MM-DD ou ISO) em que a massa foi carregada — a ``dataCriacao``
        do portfólio de validação. ``hoje``: dia da execução (padrão: hoje em UTC)."""
        carga = datetime.fromisoformat(str(dia_carga).replace("Z", "+00:00")).date() if "T" in str(dia_carga) \
            else date.fromisoformat(str(dia_carga))
        dia = date.fromisoformat(hoje) if hoje else datetime.now(timezone.utc).date()
        self.cenario = Cenario(carga, dia, {k: v for k, v in emails.items() if v})
        logger.info(f"Oráculo: massa carregada em {carga}, execução em {dia}.")
        return self.cenario

    @keyword
    def valores_esperados(self, indicador, portfolio=massa.PORTFOLIO_1):
        return getattr(self.cenario, "dashboard")(portfolio)[indicador]

    @keyword
    def ranking_esperado(self, portfolio=massa.PORTFOLIO_1):
        return self.cenario.ranking(portfolio)

    @keyword
    def score_esperado(self, nome_projeto):
        return self.cenario.score(massa.POR_NOME[nome_projeto])

    @keyword
    def progresso_okr_esperado(self, titulo):
        chave = next(k for k, (t, _) in massa.OKRS.items() if t == titulo)
        return self.cenario.progresso_okr(chave)

    @keyword
    def burn_rate_esperado_do_projeto(self, nome_projeto):
        return self.cenario.burn_rate_projeto(massa.POR_NOME[nome_projeto])

    @keyword
    def score_esperado_para(self, criterios, notas):
        """F1. ``criterios``: lista de (peso, tipo); ``notas``: lista de inteiros na mesma ordem."""
        return score_f1([(float(p), t) for p, t in criterios], [int(n) for n in notas])

    @keyword
    def ranking_esperado_para(self, projetos):
        """F2. ``projetos``: lista de (nome, score, prioridade, ordem de cadastro)."""
        return ranking_f2([(n, float(s), p, int(o)) for n, s, p, o in projetos])

    @keyword
    def burn_rate_esperado(self, orcamento, inicio, fim, custo, hoje=None):
        """F5 de um projeto criado pelos testes (datas AAAA-MM-DD)."""
        dia = date.fromisoformat(hoje) if hoje else datetime.now(timezone.utc).date()
        return burn_rate_basico(orcamento, date.fromisoformat(inicio), date.fromisoformat(fim), dia, custo)

    @keyword
    def vpl_esperado_do_projeto(self, nome_projeto):
        return self.cenario.vpl_projeto(massa.POR_NOME[nome_projeto])

    @keyword
    def qualidade_esperada_do_projeto(self, nome_projeto):
        return self.cenario.qualidade_projeto(nome_projeto)

    @keyword
    def capacidade_esperada_da_equipe(self, nome_equipe):
        return self.cenario.capacidade_membros(self.cenario.equipes[nome_equipe][1])

    @keyword
    def notificacoes_esperadas(self, persona):
        """Notificações que a persona recebeu no dia da carga da massa: lista de (tipo, nome)."""
        na_carga = Cenario(self.cenario.carga, self.cenario.carga, self.cenario.emails)
        return sorted(na_carga.notificacoes().get(persona, set()))

    @keyword
    def dependencias_em_risco_esperadas(self, portfolio=massa.PORTFOLIO_1):
        return self.cenario.dependencias_em_risco(portfolio)

    # ---------- comparação ----------
    @keyword
    def comparar_valor(self, indicador, campo, esperado, obtido, origem="API"):
        """Falha se |esperado - obtido| > 0,01 (ou se forem diferentes, para textos e booleanos)."""
        if isinstance(esperado, (int, float)) and not isinstance(esperado, bool) and obtido is not None:
            diferenca = abs(float(esperado) - float(obtido))
            ok = diferenca <= TOLERANCIA + 1e-9
        else:
            diferenca = None
            ok = esperado == obtido
        self.linhas.append({
            "origem": origem, "indicador": indicador, "campo": campo, "esperado": esperado, "obtido": obtido,
            "diferenca": None if diferenca is None else round(diferenca, 4), "resultado": "OK" if ok else "DIVERGENTE",
        })
        if not ok:
            raise AssertionError(f"{origem} {indicador}.{campo}: esperado {esperado}, obtido {obtido} (tolerância {TOLERANCIA})")

    @keyword
    def comparar_indicador(self, indicador, resposta, portfolio=massa.PORTFOLIO_1, origem="API"):
        """Confere os campos de ``CAMPOS[indicador]`` e os detalhes (categorias, contagens, OKRs)."""
        esperado = self.valores_esperados(indicador, portfolio)
        erros = []
        for campo in CAMPOS[indicador]:
            erros += self._tentar(indicador, campo, esperado.get(campo), resposta.get(campo), origem)
        if indicador == "alocacaoEstrategica":
            obtidas = {c["categoria"]: c for c in resposta["categorias"]}
            for cat, valores in esperado["categorias"].items():
                for campo, valor in valores.items():
                    erros += self._tentar(indicador, f"{cat}.{campo}", valor, obtidas[cat][campo], origem)
        if indicador == "saude":
            for classe, n in esperado["contagem"].items():
                erros += self._tentar(indicador, f"contagem.{classe}", n, resposta["contagem"][classe], origem)
            erros += self._tentar(indicador, "classificacao", esperado["classificacao"], resposta["classificacao"], origem)
            obtidos = {p["nome"]: p for p in resposta["projetos"]}
            for p in esperado["projetos"]:
                erros += self._tentar(indicador, f"{p['nome']}.pontuacaoSaude", p["pontuacaoSaude"],
                                      obtidos.get(p["nome"], {}).get("pontuacaoSaude"), origem)
        if indicador == "capacidade":
            erros += self._tentar(indicador, "classificacao", esperado["classificacao"], resposta["classificacao"], origem)
            erros += self._tentar(indicador, "mes", esperado["mes"], resposta["mes"], origem)
        if indicador == "alinhamentoOkr":
            obtidos = {o["titulo"]: o["progresso"] for o in resposta["progressoPorOkr"]}
            for titulo, progresso in esperado["progressoPorOkr"].items():
                erros += self._tentar(indicador, f"progresso.{titulo}", progresso, obtidos.get(titulo), origem)
            erros += self._tentar(indicador, "desalinhados", esperado["desalinhados"],
                                  sorted(p["nome"] for p in resposta["listaDesalinhados"]), origem)
        if indicador == "burnRate":
            obtidos = {p["projetoNome"]: p for p in resposta["projetos"]}
            for p in esperado["projetos"]:
                for campo in ("custoRealizado", "burnRateMensal", "percentualConsumido"):
                    erros += self._tentar(indicador, f"{p['nome']}.{campo}", p[campo], obtidos[p["nome"]][campo], origem)
        if erros:
            raise AssertionError(f"{len(erros)} divergência(s) em {indicador}:\n" + "\n".join(erros))

    @keyword
    def comparar_ranking(self, resposta, portfolio=massa.PORTFOLIO_1):
        esperado = self.ranking_esperado(portfolio)
        obtido = resposta["ranking"]
        erros = self._tentar("ranking", "quantidade", len(esperado), len(obtido), "API")
        for e, o in zip(esperado, obtido):
            erros += self._tentar("ranking", f"{e['posicao']}º.nome", e["nome"], o["nome"], "API")
            erros += self._tentar("ranking", f"{e['posicao']}º.score", e["score"], o["score"], "API")
        if erros:
            raise AssertionError("Ranking divergente:\n" + "\n".join(erros))

    def _tentar(self, indicador, campo, esperado, obtido, origem):
        try:
            self.comparar_valor(indicador, campo, esperado, obtido, origem)
            return []
        except AssertionError as erro:
            return [str(erro)]

    # ---------- evidência ----------
    @keyword
    def salvar_planilha_de_valores_esperados(self, pasta=None):
        pasta = pasta or BuiltIn().get_variable_value("${OUTPUT DIR}")
        os.makedirs(pasta, exist_ok=True)
        campos = ["origem", "indicador", "campo", "esperado", "obtido", "diferenca", "resultado"]
        caminho_csv = os.path.join(pasta, "valores_esperados.csv")
        with open(caminho_csv, "w", newline="", encoding="utf-8-sig") as f:
            escritor = csv.DictWriter(f, fieldnames=campos, delimiter=";")
            escritor.writeheader()
            escritor.writerows(self.linhas)
        try:
            from openpyxl import Workbook
            from openpyxl.styles import Font, PatternFill
        except ImportError:
            logger.warn("openpyxl não instalado: planilha salva só em CSV.")
            return caminho_csv
        wb = Workbook()
        ws = wb.active
        ws.title = "Comparação"
        ws.append(["Origem", "Indicador", "Campo", "Esperado", "Obtido", "Diferença", "Resultado"])
        for c in ws[1]:
            c.font = Font(bold=True)
        for linha in self.linhas:
            ws.append([linha[c] if not isinstance(linha[c], list) else ", ".join(linha[c]) for c in campos])
            if linha["resultado"] != "OK":
                for c in ws[ws.max_row]:
                    c.fill = PatternFill("solid", fgColor="F8D7DA")
        ws.append([])
        ws.append([f"Massa carregada em {self.cenario.carga}; execução em {self.cenario.hoje}; tolerância {TOLERANCIA}"])
        for coluna, largura in zip("ABCDEFG", (8, 22, 48, 16, 16, 10, 12)):
            ws.column_dimensions[coluna].width = largura
        caminho = os.path.join(pasta, "valores_esperados.xlsx")
        wb.save(caminho)
        logger.info(f"Planilha de valores esperados: {caminho}", also_console=True)
        return caminho
