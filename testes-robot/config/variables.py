"""Variáveis da suíte, lidas do ambiente. Qualquer uma pode ser trocada com ``robot -v NOME:valor``."""
import os

_env = os.getenv

API_URL = _env("PRUMO_API_URL", "http://localhost:5150").rstrip("/")
FRONT_URL = _env("PRUMO_FRONT_URL", "http://localhost:4200").rstrip("/")

# Login de teste da API (seção 4.9 b): mesma chave configurada em AuthTeste:Chave.
AUTH_TESTE_CHAVE = _env("PRUMO_AUTH_TESTE_CHAVE", "")

# Contas simuladas, uma por persona (seção 4.3.2). Os padrões são os do SeedValidacao da API.
EMAILS = {
    "Desenvolvedor": _env("PRUMO_EMAIL_DESENVOLVEDOR", "desenvolvedor@prumo"),
    "QA": _env("PRUMO_EMAIL_QA", "qa@prumo"),
    "ProductOwner": _env("PRUMO_EMAIL_PRODUCTOWNER", "po@prumo"),
    "TechLead": _env("PRUMO_EMAIL_TECHLEAD", "techlead@prumo"),
    "GerenteProjeto": _env("PRUMO_EMAIL_GERENTEPROJETO", "gerente@prumo"),
    "Diretoria": _env("PRUMO_EMAIL_DIRETORIA", "diretoria@prumo"),
    "Validacao": _env("PRUMO_EMAIL_VALIDACAO", "validacao@prumo"),
}
EMAIL_ADMIN = _env("PRUMO_EMAIL_ADMIN", "admin@prumo.test")

# Portfólios da massa de validação (ids fixos do SeedValidacao).
PORTFOLIO_VALIDACAO_1 = "5eed0000-0000-4000-8000-000000000001"
PORTFOLIO_VALIDACAO_2 = "5eed0000-0000-4000-8000-000000000002"

# Opcional: chave JWT da API, só para os testes de token expirado.
JWT_KEY = _env("PRUMO_JWT_KEY", "")
JWT_ISSUER = _env("PRUMO_JWT_ISSUER", "prumo-api")
JWT_AUDIENCE = _env("PRUMO_JWT_AUDIENCE", "prumo-web")

# Navegador (seção 4.2): chrome, firefox ou edge.
BROWSER = _env("BROWSER", "chrome")
HEADLESS = _env("HEADLESS", "true").lower() != "false"
BROWSER_BINARY = _env("BROWSER_BINARY", "")      # ex.: Chromium fora do PATH
DRIVER_PATH = _env("DRIVER_PATH", "")            # ex.: chromedriver específico
LARGURA_JANELA = int(_env("LARGURA_JANELA", "1366"))
ALTURA_JANELA = int(_env("ALTURA_JANELA", "768"))

# Requisitos não funcionais (Tabela 21).
LIMITE_DASHBOARD_SEGUNDOS = float(_env("LIMITE_DASHBOARD_SEGUNDOS", "3"))
# Telas médias e grandes (seção 4.2).
RESOLUCOES = [(1024, 768), (1366, 768), (1920, 1080)]

# Jira Cloud de testes (seção 4.3.3). Sem estas variáveis, a sincronização real é pulada.
JIRA_URL = _env("PRUMO_JIRA_URL", "")
JIRA_EMAIL = _env("PRUMO_JIRA_EMAIL", "")
JIRA_TOKEN = _env("PRUMO_JIRA_TOKEN", "")
JIRA_PROJETO = _env("PRUMO_JIRA_PROJETO", "")     # chave do projeto de testes no Jira (ex.: PRUMOTEST)
JIRA_TIMEOUT_SINCRONIZACAO = _env("PRUMO_JIRA_TIMEOUT", "180s")
