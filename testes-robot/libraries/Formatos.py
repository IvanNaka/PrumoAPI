"""Utilitários da interface: opções dos navegadores e leitura de números no formato brasileiro."""
import re

from robot.api.deco import keyword, library
from selenium import webdriver

_MOEDA_NEGATIVA = re.compile(r"-\s*R\$\s*")
_NUMERO = re.compile(r"-?\d{1,3}(?:\.\d{3})+(?:,\d+)?|-?\d+(?:,\d+)?")


@library(scope="GLOBAL")
class Formatos:
    @keyword
    def opcoes_do_navegador(self, navegador, headless=True, binario="", largura=1366, altura=768):
        """Opções do Selenium para chrome, firefox ou edge (seção 4.2)."""
        nome = navegador.lower().replace("headless", "")
        headless = str(headless).lower() not in ("false", "0", "no")
        if nome in ("chrome", "gc", "googlechrome", "chromium"):
            opcoes = webdriver.ChromeOptions()
        elif nome in ("edge", "msedge"):
            opcoes = webdriver.EdgeOptions()
        elif nome in ("firefox", "ff"):
            opcoes = webdriver.FirefoxOptions()
            if headless:
                opcoes.add_argument("-headless")
            opcoes.add_argument(f"--width={largura}")
            opcoes.add_argument(f"--height={altura}")
            if binario:
                opcoes.binary_location = binario
            return opcoes
        else:
            raise ValueError(f"Navegador não suportado: {navegador}. Use chrome, firefox ou edge.")
        if headless:
            opcoes.add_argument("--headless=new")
        for argumento in ("--no-sandbox", "--disable-dev-shm-usage", f"--window-size={largura},{altura}", "--lang=pt-BR"):
            opcoes.add_argument(argumento)
        if binario:
            opcoes.binary_location = binario
        return opcoes

    @keyword
    def numero_brasileiro(self, texto):
        """Primeiro número de ``texto`` (ex.: "R$ 1.300.000,00" -> 1300000.0; "36,68%" -> 36.68)."""
        encontrado = _NUMERO.search(_normalizar(texto))
        if not encontrado:
            raise AssertionError(f"Nenhum número em: {texto!r}")
        return float(encontrado.group(0).replace(".", "").replace(",", "."))

    @keyword
    def numeros_brasileiros(self, texto):
        """Todos os números de ``texto``, na ordem."""
        return [float(n.replace(".", "").replace(",", ".")) for n in _NUMERO.findall(_normalizar(texto))]


def _normalizar(texto):
    texto = str(texto).replace("\u2212", "-").replace("\xa0", " ")
    return _MOEDA_NEGATIVA.sub("-", texto)
