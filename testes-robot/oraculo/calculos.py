"""Fórmulas dos indicadores (Cap. 3) aplicadas à massa de ``massa.py``.

Implementação independente da API: não importa nada do back-end e trabalha a partir da descrição da
massa de dados. Valores monetários e percentuais são arredondados para 2 casas (meio para cima),
como numa planilha; a comparação com a API admite diferença de até 0,01 (seção 4.9 c).
"""
from calendar import monthrange
from datetime import date, datetime, time, timedelta
from decimal import ROUND_HALF_UP, Decimal
from statistics import mean, median

from . import massa

INATIVOS = {"Cancelado", "Arquivado"}           # fora de "projetos ativos" (Seção 3.5)
AVALIAVEIS_SAUDE = {"Planejado", "EmAndamento", "EmRisco"}
DIAS_PERIODO = 90


def r2(valor):
    return float(Decimal(str(valor)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP))


def add_months(d, meses):
    """Mesmo dia N meses depois (último dia do mês quando ele não existe)."""
    total = d.month - 1 + meses
    ano, mes = d.year + total // 12, total % 12 + 1
    return date(ano, mes, min(d.day, monthrange(ano, mes)[1]))


def meses_inteiros(de, ate):
    meses = (ate.year - de.year) * 12 + ate.month - de.month
    if ate.day < de.day:
        meses -= 1
    return max(0, meses)


def percentil(valores, p):
    v = sorted(valores)
    pos = (len(v) - 1) * p / 100
    lo, hi = int(pos), min(int(pos) + 1, len(v) - 1)
    return v[lo] + (v[hi] - v[lo]) * (pos - lo)


class Cenario:
    """Massa de dados "materializada" para um dia de carga e um dia de execução."""

    def __init__(self, dia_carga: date, hoje: date, emails=None):
        self.carga = dia_carga
        self.hoje = hoje
        self.emails = {k: v[1] for k, v in massa.CONTAS.items()}
        self.emails.update(emails or {})
        self.equipes = massa.equipes(self.emails)
        self.projetos = {p.nome: p for p in massa.PROJETOS}
        self._montar_issues()

    # ---------- datas da massa ----------
    def d(self, dias):
        return self.carga + timedelta(days=dias)

    def dia_hora(self, dias):
        return datetime.combine(self.d(dias), time(12))

    def inicio(self, p):
        return self.d(p.inicio)

    def fim(self, p):
        return self.d(p.fim)

    def conclusao(self, p):
        return self.d(p.fim - massa.DIAS_ANTES_DO_FIM_CONCLUSAO) if p.status == "Concluido" else None

    def _montar_issues(self):
        self.issues = []
        dia_aberta = self.carga - timedelta(days=min(self.carga.day - 1, 2))
        for nome, (equipe, estimativa_aberta, muitas_atrasadas) in massa.ISSUES.items():
            membros = self.equipes[equipe][1]
            for i, (tipo, concluida, criada, concluida_em, prazo, prazo_atraso) in enumerate(massa.MODELO_ISSUES):
                email = membros[i % len(membros)][1]
                if concluida:
                    worklogs = [(self.d(concluida_em - w), h) for w, h in enumerate(massa.HORAS_CONCLUIDA)]
                else:
                    worklogs = [(dia_aberta, h) for h in massa.HORAS_ABERTA]
                self.issues.append({
                    "projeto": nome, "tipo": tipo, "concluida": concluida, "email": email,
                    "criada": self.dia_hora(criada),
                    "concluida_em": self.dia_hora(concluida_em) if concluida else None,
                    "prazo": self.d(prazo_atraso if muitas_atrasadas else prazo),
                    "estimativa": massa.ESTIMATIVA_CONCLUIDA if concluida else estimativa_aberta,
                    "realizado": sum(h for _, h in worklogs),
                    "worklogs": [(dia, h, email) for dia, h in worklogs],
                })

    def ativos(self, portfolio):
        return [p for p in massa.PROJETOS if p.portfolio == portfolio and p.status not in INATIVOS]

    def issues_de(self, projetos):
        nomes = {p.nome for p in projetos}
        return [i for i in self.issues if i["projeto"] in nomes]

    def periodo(self):
        de = self.hoje - timedelta(days=DIAS_PERIODO)
        return datetime.combine(de, time.min), datetime.combine(self.hoje, time.max)

    # ---------- F1 / F2: score e ranking ----------
    def score(self, p):
        if len(p.notas) < len(massa.CRITERIOS):
            return None
        soma = sum(peso * (6 - n if tipo == "Custo" else n) for (_, tipo, peso), n in zip(massa.CRITERIOS, p.notas))
        media = Decimal(soma) / Decimal(sum(peso for _, _, peso in massa.CRITERIOS))
        return r2((media - 1) / 4 * 100)

    def ranking(self, portfolio):
        ordem = {p.nome: i for i, p in enumerate(massa.PROJETOS)}
        candidatos = [p for p in self.ativos(portfolio) if self.score(p) is not None]
        candidatos.sort(key=lambda p: (-self.score(p), -massa.PRIORIDADE[p.prioridade], ordem[p.nome]))
        return [{"posicao": i + 1, "nome": p.nome, "score": self.score(p)} for i, p in enumerate(candidatos)]

    # ---------- F4: progresso de OKR ----------
    @staticmethod
    def progresso_kr(meta, atual):
        return r2(min(max(atual, 0) / meta, 1) * 100) if meta > 0 else 0

    def progresso_okr(self, chave):
        krs = massa.OKRS[chave][1]
        return r2(mean(self.progresso_kr(m, a) for _, m, a in krs))

    # ---------- F5: burn rate ----------
    def custo_hora(self):
        custos = {}
        for _, membros in self.equipes.values():
            for _, email, custo, _ in membros:
                custos.setdefault(email.lower(), []).append(custo)
        return {e: mean(v) for e, v in custos.items()}

    def burn_rate_projeto(self, p):
        custos = self.custo_hora()
        horas = [(h, e) for i in self.issues if i["projeto"] == p.nome for _, h, e in i["worklogs"]]
        custo_horas = sum(Decimal(str(h)) * Decimal(str(custos.get(e.lower(), 0))) for h, e in horas)
        lancamentos = Decimal(sum(p.lancamentos))
        custo = custo_horas + lancamentos
        ref = self.conclusao(p) or min(self.hoje, self.fim(p))
        meses = max(Decimal(1), Decimal((ref - self.inicio(p)).days) / Decimal(30))
        duracao = max(Decimal(1), Decimal((self.fim(p) - self.inicio(p)).days) / Decimal(30))
        mensal = Decimal(0) if self.hoje < self.inicio(p) else custo / meses
        orcamento = Decimal(p.orcamento)
        return {
            "nome": p.nome, "orcamentoAprovado": r2(orcamento), "custoHoras": r2(custo_horas),
            "custoLancamentos": r2(lancamentos), "custoRealizado": r2(custo),
            "burnRateMensal": r2(mensal), "projecaoCustoTotal": r2(mensal * duracao),
            "percentualConsumido": r2(custo / orcamento * 100), "saldo": r2(orcamento - custo),
            "estouro": custo > orcamento, "riscoEstouro": mensal * duracao > orcamento,
        }

    def burn_rate(self, portfolio):
        linhas = [self.burn_rate_projeto(p) for p in self.ativos(portfolio)]
        soma = lambda campo: sum(Decimal(str(l[campo])) for l in linhas)
        orcamento, custo = soma("orcamentoAprovado"), soma("custoRealizado")
        return {
            "orcamentoAprovado": r2(orcamento), "custoRealizado": r2(custo), "custoHoras": r2(soma("custoHoras")),
            "custoLancamentos": r2(soma("custoLancamentos")), "burnRateMensal": r2(soma("burnRateMensal")),
            "projecaoCustoTotal": r2(soma("projecaoCustoTotal")), "saldo": r2(orcamento - custo),
            "percentualConsumido": r2(custo / orcamento * 100), "projetos": linhas,
        }

    # ---------- F6: VPL ----------
    def vpl_projeto(self, p):
        if p.nome not in massa.BUSINESS_CASES:
            return None
        investimento, fluxo, meses = massa.BUSINESS_CASES[p.nome]
        i = (1 + massa.TAXA_ANUAL / 100) ** (1 / 12) - 1
        esperado = -investimento + sum(fluxo / (1 + i) ** t for t in range(1, meses + 1))
        retornos = []
        if p.nome in massa.RETORNOS:
            retornos = [(add_months(self.inicio(p), m), massa.RETORNOS[p.nome]) for m in (1, 2, 3)]
        custo = self.burn_rate_projeto(p)["custoRealizado"]
        realizado = -custo + sum(v / (1 + i) ** max(1, meses_inteiros(self.inicio(p), d)) for d, v in retornos)
        return {"nome": p.nome, "vplEsperado": r2(esperado), "vplRealizado": r2(realizado)}

    def vpl(self, portfolio):
        linhas = [v for v in (self.vpl_projeto(p) for p in self.ativos(portfolio)) if v]
        esperado = sum(Decimal(str(l["vplEsperado"])) for l in linhas)
        realizado = sum(Decimal(str(l["vplRealizado"])) for l in linhas)
        return {"vplEsperado": r2(esperado), "vplRealizado": r2(realizado), "diferenca": r2(realizado - esperado),
                "percentualAtingimento": r2(realizado / esperado * 100) if esperado else None, "projetos": linhas}

    # ---------- F7 / F8: lead time e qualidade ----------
    def concluidas_no_periodo(self, portfolio):
        de, ate = self.periodo()
        return [i for i in self.issues_de(self.ativos(portfolio))
                if i["concluida"] and de <= i["concluida_em"] <= ate]

    def lead_time(self, portfolio):
        dias = [(i["concluida_em"] - i["criada"]).total_seconds() / 86400 for i in self.concluidas_no_periodo(portfolio)]
        if not dias:
            return {"quantidade": 0}
        return {"quantidade": len(dias), "leadTimeMediano": r2(median(dias)), "leadTimeMedio": r2(mean(dias)),
                "p85": r2(percentil(dias, 85))}

    def qualidade(self, portfolio):
        concluidas = self.concluidas_no_periodo(portfolio)
        bugs = sum(1 for i in concluidas if i["tipo"] == "Bug")
        entregas = len(concluidas) - bugs
        if entregas == 0:
            return {"bugs": bugs, "entregas": 0}
        return {"bugs": bugs, "entregas": entregas, "razaoBugsPorEntrega": r2(Decimal(bugs) / entregas),
                "indiceQualidade": r2(Decimal(entregas) / (entregas + bugs) * 100),
                "atingiuMeta": Decimal(bugs) / entregas <= Decimal("0.20")}

    def qualidade_projeto(self, nome):
        de, ate = self.periodo()
        concluidas = [i for i in self.issues if i["projeto"] == nome and i["concluida"] and de <= i["concluida_em"] <= ate]
        bugs = sum(1 for i in concluidas if i["tipo"] == "Bug")
        entregas = len(concluidas) - bugs
        return {"bugs": bugs, "entregas": entregas,
                "razaoBugsPorEntrega": r2(Decimal(bugs) / entregas) if entregas else None,
                "indiceQualidade": r2(Decimal(entregas) / (entregas + bugs) * 100) if entregas else None}

    # ---------- F9: capacidade ----------
    def capacidade_membros(self, membros):
        emails = {e.lower() for _, e, _, _ in membros}
        capacidade = sum(c for _, _, _, c in membros)
        demanda = sum(max(i["estimativa"] - i["realizado"], 0) for i in self.issues
                      if not i["concluida"] and i["email"].lower() in emails)
        horas = sum(h for i in self.issues for dia, h, e in i["worklogs"]
                    if e.lower() in emails and (dia.year, dia.month) == (self.hoje.year, self.hoje.month))
        ocupacao = r2(Decimal(demanda) / capacidade * 100)
        return {"capacidade": capacidade, "demanda": r2(demanda), "horasMes": r2(horas), "ocupacao": ocupacao,
                "utilizacao": r2(Decimal(str(horas)) / capacidade * 100),
                "classificacao": "Subutilizada" if ocupacao < 70 else "Adequada" if ocupacao <= 100 else "Sobrecarregada"}

    def capacidade(self, portfolio):
        membros = [m for port, ms in self.equipes.values() if port == portfolio for m in ms]
        resultado = self.capacidade_membros(membros)
        resultado["mes"] = f"{self.hoje.year:04d}-{self.hoje.month:02d}"
        resultado["equipes"] = {nome: self.capacidade_membros(ms) for nome, (port, ms) in self.equipes.items() if port == portfolio}
        return resultado

    # ---------- F10 / F11: alocação estratégica e alinhamento a OKRs ----------
    def alocacao(self, portfolio):
        ativos = self.ativos(portfolio)
        total = Decimal(sum(p.orcamento for p in ativos))
        categorias = {}
        for cat in ("Run", "Grow", "Transform"):
            ps = [p for p in ativos if p.categoria == cat]
            orc = Decimal(sum(p.orcamento for p in ps))
            categorias[cat] = {"quantidade": len(ps), "orcamento": r2(orc), "percentualOrcamento": r2(orc / total * 100)}
        return {"orcamentoTotal": r2(total), "categorias": categorias}

    def alinhamento_okr(self, portfolio):
        ativos = self.ativos(portfolio)
        alinhados = [p for p in ativos if p.okrs]
        total = Decimal(sum(p.orcamento for p in ativos))
        return {
            "totalProjetos": len(ativos), "projetosAlinhados": len(alinhados),
            "percentualProjetosAlinhados": r2(Decimal(len(alinhados)) / len(ativos) * 100),
            "percentualOrcamentoAlinhado": r2(Decimal(sum(p.orcamento for p in alinhados)) / total * 100),
            "desalinhados": sorted(p.nome for p in ativos if not p.okrs),
            "progressoPorOkr": {massa.OKRS[k][0]: self.progresso_okr(k) for k in massa.OKRS_DO_PORTFOLIO[portfolio]},
        }

    # ---------- F12 / F13: saúde e dependências em risco ----------
    def dependencia_em_risco(self, origem, destino):
        if destino.status in {"EmRisco", "Suspenso", "Cancelado"}:
            return True
        return destino.status != "Concluido" and self.fim(destino) > self.fim(origem)

    def saude_projeto(self, p):
        issues = [i for i in self.issues if i["projeto"] == p.nome]
        atrasadas = sum(1 for i in issues if not i["concluida"] and i["prazo"] < self.hoje)
        flags = []
        if p.status == "EmRisco":
            flags.append("status")
        if self.burn_rate_projeto(p)["percentualConsumido"] > 100:
            flags.append("orcamento")
        if self.hoje > self.fim(p):
            flags.append("prazo")
        if issues and atrasadas / len(issues) > 0.20:
            flags.append("atrasadas")
        if any(o == p.nome and self.dependencia_em_risco(p, self.projetos[d]) for o, d in massa.DEPENDENCIAS):
            flags.append("dependencia")
        pontos = max(0, 100 - 25 * len(flags))
        return {"nome": p.nome, "pontuacaoSaude": pontos, "flags": len(flags), "atrasadas": atrasadas,
                "classificacao": self.classificar_saude(pontos)}

    @staticmethod
    def classificar_saude(pontos):
        return "Saudável" if pontos >= 75 else "Atenção" if pontos >= 50 else "Crítico"

    def saude(self, portfolio):
        linhas = [self.saude_projeto(p) for p in self.ativos(portfolio) if p.status in AVALIAVEIS_SAUDE]
        media = r2(Decimal(sum(l["pontuacaoSaude"] for l in linhas)) / len(linhas))
        contagem = {c: sum(1 for l in linhas if l["classificacao"] == c) for c in ("Saudável", "Atenção", "Crítico")}
        return {"saude": media, "classificacao": self.classificar_saude(media), "contagem": contagem, "projetos": linhas}

    def dependencias_em_risco(self, portfolio):
        return {f"{o} → {d}": self.dependencia_em_risco(self.projetos[o], self.projetos[d])
                for o, d in massa.DEPENDENCIAS if self.projetos[o].portfolio == portfolio}

    # ---------- RF45: notificações automáticas ----------
    def notificacoes(self):
        """Notificações geradas pelas regras (D14) no dia da carga: {persona: {(tipo, nome)}}."""
        esperadas = {}

        def notificar(personas, tipo, nome):
            for persona in personas:
                esperadas.setdefault(persona, set()).add((tipo, nome))

        ativos = [p for p in massa.PROJETOS if p.status not in INATIVOS]
        for p in ativos:
            dono = massa.RESPONSAVEL[p.portfolio]
            if p.status != "Concluido":
                if self.hoje > self.fim(p):
                    notificar([dono], "Atraso", p.nome)
                issues = [i for i in self.issues if i["projeto"] == p.nome]
                atrasadas = sum(1 for i in issues if not i["concluida"] and i["prazo"] < self.hoje)
                if issues and atrasadas / len(issues) > 0.20:
                    notificar([dono], "Atraso", p.nome)
                if p.status == "EmRisco":
                    notificar([dono], "Risco", p.nome)
            if not p.okrs:
                notificar(["ProductOwner"], "Desalinhamento", p.nome)
            burn = self.burn_rate_projeto(p)
            if burn["estouro"] or burn["riscoEstouro"]:
                notificar(["Diretoria", dono], "EstouroOrcamento", p.nome)
        for origem, destino in massa.DEPENDENCIAS:
            o = self.projetos[origem]
            if o.status != "Concluido" and self.dependencia_em_risco(o, self.projetos[destino]):
                notificar([massa.RESPONSAVEL[o.portfolio]], "Risco", origem)
        for nome, (_, membros) in self.equipes.items():
            if self.capacidade_membros(membros)["ocupacao"] > 100:
                notificar(["TechLead"], "Conflito", nome)
        return esperadas

    # ---------- tudo ----------
    def dashboard(self, portfolio):
        return {
            "burnRate": self.burn_rate(portfolio), "saude": self.saude(portfolio), "alocacaoEstrategica": self.alocacao(portfolio),
            "qualidade": self.qualidade(portfolio), "capacidade": self.capacidade(portfolio), "leadTime": self.lead_time(portfolio),
            "vpl": self.vpl(portfolio), "alinhamentoOkr": self.alinhamento_okr(portfolio),
        }


def burn_rate_basico(orcamento, inicio: date, fim: date, hoje: date, custo, concluido_em: date = None):
    """F5 para um projeto qualquer (sem a massa): custo realizado já somado (lançamentos + horas)."""
    custo, orcamento = Decimal(str(custo)), Decimal(str(orcamento))
    ref = concluido_em or min(hoje, fim)
    meses = max(Decimal(1), Decimal((ref - inicio).days) / Decimal(30))
    duracao = max(Decimal(1), Decimal((fim - inicio).days) / Decimal(30))
    mensal = Decimal(0) if hoje < inicio else custo / meses
    return {"custoRealizado": r2(custo), "burnRateMensal": r2(mensal), "projecaoCustoTotal": r2(mensal * duracao),
            "percentualConsumido": r2(custo / orcamento * 100), "saldo": r2(orcamento - custo),
            "estouro": custo > orcamento, "riscoEstouro": mensal * duracao > orcamento}


def score_f1(criterios, notas):
    """F1 para critérios [(peso, tipo)] e notas [1..5] na mesma ordem."""
    soma = sum(Decimal(str(peso)) * (6 - n if tipo == "Custo" else n) for (peso, tipo), n in zip(criterios, notas))
    media = soma / sum(Decimal(str(peso)) for peso, _ in criterios)
    return r2((media - 1) / 4 * 100)


def ranking_f2(projetos):
    """F2 para [(nome, score, prioridade, ordem de cadastro)]: score ↓, prioridade ↓, cadastro ↑."""
    ordenados = sorted(projetos, key=lambda p: (-p[1], -massa.PRIORIDADE[p[2]], p[3]))
    return [{"posicao": i + 1, "nome": p[0], "score": p[1]} for i, p in enumerate(ordenados)]
