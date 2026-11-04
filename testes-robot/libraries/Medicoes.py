"""Registro das medições de desempenho (seção 4.9 e) em results/desempenho.csv."""
import csv
import os
import time
from datetime import datetime, timezone

from robot.api import logger
from robot.api.deco import keyword, library
from robot.libraries.BuiltIn import BuiltIn


@library(scope="GLOBAL")
class Medicoes:
    def __init__(self):
        self._inicio = {}

    @keyword
    def iniciar_cronometro(self, nome):
        self._inicio[nome] = time.perf_counter()

    @keyword
    def parar_cronometro(self, nome):
        return round(time.perf_counter() - self._inicio.pop(nome), 3)

    @keyword
    def registrar_medicao(self, medicao, segundos, limite=None, navegador=""):
        """Grava a medição; com ``limite``, falha se ``segundos`` passar dele."""
        segundos = float(segundos)
        ok = limite is None or segundos <= float(limite)
        caminho = os.path.join(BuiltIn().get_variable_value("${OUTPUT DIR}"), "desempenho.csv")
        novo = not os.path.exists(caminho)
        with open(caminho, "a", newline="", encoding="utf-8-sig") as f:
            escritor = csv.writer(f, delimiter=";")
            if novo:
                escritor.writerow(["data", "medicao", "navegador", "segundos", "limite", "resultado"])
            escritor.writerow([datetime.now(timezone.utc).isoformat(timespec="seconds"), medicao, navegador,
                               f"{segundos:.3f}".replace(".", ","), "" if limite is None else str(limite).replace(".", ","),
                               "OK" if ok else "ACIMA DO LIMITE"])
        logger.info(f"{medicao}: {segundos:.3f} s" + ("" if limite is None else f" (limite {limite} s)"), also_console=True)
        if not ok:
            raise AssertionError(f"{medicao}: {segundos:.3f} s, acima do limite de {limite} s.")
