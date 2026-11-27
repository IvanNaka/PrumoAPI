"""Massa de dados sintéticos da seção 4.3 — a mesma carregada pelo SeedValidacao da API.

As datas são deslocamentos (em dias) a partir do dia em que a massa foi carregada (``dia_carga``).
Este arquivo é a "entrada da planilha": os valores esperados saem de ``calculos.py``.
"""
from dataclasses import dataclass, field

PORTFOLIO_1 = "5eed0000-0000-4000-8000-000000000001"
PORTFOLIO_2 = "5eed0000-0000-4000-8000-000000000002"
PORTFOLIOS = {PORTFOLIO_1: "Transformação Digital", PORTFOLIO_2: "Eficiência Operacional"}

# Contas simuladas (seção 4.3.2). E-mails padrão; podem ser trocados por variáveis de ambiente.
CONTAS = {
    "Desenvolvedor": ("Ana Desenvolvedora", "desenvolvedor@prumo"),
    "QA": ("Paulo QA", "qa@prumo"),
    "ProductOwner": ("Priscila Product Owner", "po@prumo"),
    "TechLead": ("Tiago Tech Lead", "techlead@prumo"),
    "GerenteProjeto": ("Gabriela Gerente", "gerente@prumo"),
    "Diretoria": ("Daniel Diretor", "diretoria@prumo"),
    "Validacao": ("Usuário de Validação", "validacao@prumo"),
}

# Critérios de cada portfólio: (nome, tipo, peso). Custo = "quanto menor, melhor".
CRITERIOS = [
    ("Valor de negócio", "Beneficio", 4),
    ("Esforço", "Custo", 2),
    ("Risco", "Custo", 2),
    ("Alinhamento estratégico", "Beneficio", 2),
]

PRIORIDADE = {"Baixa": 1, "Media": 2, "Alta": 3, "Critica": 4}


@dataclass
class Projeto:
    portfolio: str
    nome: str
    categoria: str
    status: str
    prioridade: str
    orcamento: float
    inicio: int
    fim: int
    notas: list = field(default_factory=list)
    lancamentos: list = field(default_factory=list)
    okrs: list = field(default_factory=list)


P1, P2 = PORTFOLIO_1, PORTFOLIO_2
# Ordem = ordem de cadastro (desempate final do ranking).
PROJETOS = [
    Projeto(P1, "Portal do Cliente", "Grow", "EmAndamento", "Alta", 400_000, -120, 120, [5, 3, 2, 5], [45_000, 30_000], ["cliente"]),
    Projeto(P1, "App Mobile", "Transform", "EmRisco", "Critica", 300_000, -90, 60, [5, 4, 4, 4], [60_000, 25_000], ["cliente"]),
    Projeto(P1, "Migração para Nuvem", "Transform", "EmAndamento", "Alta", 250_000, -200, -10, [4, 4, 3, 4], [90_000, 40_000], ["custos", "continuidade"]),
    Projeto(P1, "Chatbot de Atendimento", "Grow", "EmAndamento", "Media", 80_000, -60, 90, [3, 2, 2, 3], [50_000, 32_000], ["cliente"]),
    Projeto(P1, "Manutenção do ERP", "Run", "EmAndamento", "Media", 150_000, -300, 65, [3, 2, 1, 2], [35_000, 20_000], ["continuidade"]),
    Projeto(P1, "BI Comercial", "Grow", "Planejado", "Baixa", 120_000, 10, 200, [4, 3], [3_000], []),
    Projeto(P2, "Automação de Faturamento", "Grow", "EmAndamento", "Alta", 200_000, -100, 150, [5, 2, 2, 4], [40_000, 15_000], ["custos"]),
    Projeto(P2, "Renovação do Parque de TI", "Run", "Suspenso", "Media", 180_000, -150, 100, [3, 3, 2, 2], [70_000], ["continuidade"]),
    Projeto(P2, "Gestão de Contratos", "Run", "Concluido", "Media", 90_000, -300, -30, [3, 2, 1, 3], [55_000, 20_000], ["custos"]),
    Projeto(P2, "Portal de Fornecedores", "Transform", "Planejado", "Alta", 220_000, 20, 300, [4, 4, 3, 5], [5_000], ["custos"]),
    Projeto(P2, "Segurança da Informação", "Run", "EmAndamento", "Critica", 160_000, -80, 180, [5, 3, 2, 4], [30_000, 12_000], ["continuidade"]),
    Projeto(P2, "Data Lake", "Transform", "Rascunho", "Media", 350_000, 30, 400, [4], [2_000], ["custos"]),
]
POR_NOME = {p.nome: p for p in PROJETOS}

# Projeto concluído: DataConclusao = fim - 5 dias.
DIAS_ANTES_DO_FIM_CONCLUSAO = 5

# OKRs: chave -> (título, [(KR, meta, atual)])
OKRS = {
    "cliente": ("Aumentar a satisfação do cliente nos canais digitais",
                [("NPS dos canais digitais", 70, 52), ("Atendimentos resolvidos no autoatendimento (%)", 60, 35)]),
    "custos": ("Reduzir o custo operacional",
               [("Economia anual (R$ mil)", 1500, 600), ("Processos automatizados", 20, 9)]),
    "continuidade": ("Garantir a continuidade dos sistemas críticos",
                     [("Disponibilidade dos sistemas (%)", 99.9, 99.5), ("Sistemas com plano de contingência", 12, 7)]),
}
OKRS_DO_PORTFOLIO = {P1: ["cliente", "custos", "continuidade"], P2: ["custos", "continuidade"]}

# Business case: projeto -> (investimento, fluxo mensal, meses); taxa anual 12%.
TAXA_ANUAL = 12
BUSINESS_CASES = {
    "Portal do Cliente": (400_000, 45_000, 18),
    "App Mobile": (300_000, 40_000, 12),
    "Manutenção do ERP": (150_000, 20_000, 12),
    "Automação de Faturamento": (200_000, 30_000, 12),
    "Portal de Fornecedores": (220_000, 25_000, 18),
}
# Retornos realizados: projeto -> valor, nos meses 1, 2 e 3 após o início.
RETORNOS = {"Portal do Cliente": 38_000, "App Mobile": 15_000, "Manutenção do ERP": 22_000, "Automação de Faturamento": 31_000}

# Dependências: (origem, destino) — origem depende do destino.
DEPENDENCIAS = [
    ("Portal do Cliente", "App Mobile"),
    ("BI Comercial", "Portal do Cliente"),
    ("Portal de Fornecedores", "Automação de Faturamento"),
]

# Equipes: nome -> (portfólio, [(chave da conta ou None, nome, e-mail, custo/hora, capacidade mensal)])
def equipes(emails):
    return {
        "Squad Digital": (P1, [
            ("Desenvolvedor", emails["Desenvolvedor"], 120, 160),
            ("QA", emails["QA"], 100, 160),
            ("Carla Souza", "carla.souza@prumo.dev", 130, 80),
        ]),
        "Squad Operações": (P2, [
            ("TechLead", emails["TechLead"], 150, 160),
            ("Bruno Lima", "bruno.lima@prumo.dev", 110, 160),
            ("Diego Rocha", "diego.rocha@prumo.dev", 90, 160),
        ]),
    }

# Modelo de issues (tipo, concluída, criada, concluída em, prazo normal, prazo se "muitas atrasadas").
MODELO_ISSUES = [
    ("Story", True, -80, -70, -65, -65),
    ("Story", True, -60, -45, -40, -40),
    ("Task", True, -50, -47, -45, -45),
    ("Feature", True, -70, -20, -15, -15),
    ("Bug", True, -30, -28, -25, -25),
    ("Task", False, -40, None, -3, -5),
    ("Story", False, -20, None, 20, -2),
    ("Bug", False, -10, None, 15, -1),
]
HORAS_CONCLUIDA = [8, 6]   # worklogs de issue concluída (no dia da conclusão e no anterior)
HORAS_ABERTA = [4]         # worklog de issue aberta (até 2 dias antes da carga, no mesmo mês)
ESTIMATIVA_CONCLUIDA = 16

# Projetos com issues: projeto -> (equipe, estimativa das abertas, muitas atrasadas?)
ISSUES = {
    "Portal do Cliente": ("Squad Digital", 40, False),
    "App Mobile": ("Squad Digital", 40, True),
    "Migração para Nuvem": ("Squad Digital", 40, False),
    "Chatbot de Atendimento": ("Squad Digital", 40, False),
    "Manutenção do ERP": ("Squad Digital", 40, False),
    "Automação de Faturamento": ("Squad Operações", 60, False),
    "Segurança da Informação": ("Squad Operações", 60, False),
}

# Responsáveis: portfólio 1 e seus projetos -> Gerente; portfólio 2 e seus projetos -> PO.
RESPONSAVEL = {PORTFOLIO_1: "GerenteProjeto", PORTFOLIO_2: "ProductOwner"}
