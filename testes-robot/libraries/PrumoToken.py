"""Gera JWTs no mesmo formato que o AuthService da Prumo API emite.

O login real é feito pelo Google, que não dá para automatizar. Como a API valida apenas a
assinatura (HS256 com Jwt:Key), emissor, audiência e validade, a suíte assina o próprio token.
"""
import uuid
from datetime import datetime, timedelta, timezone

import jwt
from robot.api.deco import keyword, library


@library(scope="GLOBAL")
class PrumoToken:
    @keyword
    def gerar_token_prumo(self, key, issuer, audience, user_id="", email="", name="",
                          roles="Administrador", horas=1):
        """Retorna um JWT assinado. ``roles`` é uma lista separada por vírgula (pode ser vazia)."""
        if not key:
            raise ValueError("Informe PRUMO_JWT_KEY (a mesma Jwt:Key da API) ou PRUMO_TOKEN.")
        claims = {
            "sub": user_id or str(uuid.uuid4()),
            "email": email,
            "name": name,
            "iss": issuer,
            "aud": audience,
            "exp": datetime.now(timezone.utc) + timedelta(hours=float(horas)),
        }
        lista = [r.strip() for r in str(roles).split(",") if r.strip()]
        if lista:
            claims["role"] = lista if len(lista) > 1 else lista[0]
        return jwt.encode(claims, key, algorithm="HS256")

    @keyword
    def gerar_token_expirado(self, key, issuer, audience):
        claims = {
            "sub": str(uuid.uuid4()),
            "iss": issuer,
            "aud": audience,
            "exp": datetime.now(timezone.utc) - timedelta(hours=1),
            "role": "Administrador",
        }
        return jwt.encode(claims, key, algorithm="HS256")

    @keyword
    def gerar_nome_unico(self, prefixo="RF"):
        return f"{prefixo} {uuid.uuid4().hex[:8]}"
