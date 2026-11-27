*** Settings ***
Documentation       Plano alternativo de validação do Prumo (Cap. 4, seção 4.9).
...                 Suítes por persona (Tabela 20), cálculos conferidos com a planilha de valores esperados
...                 (seção 4.9 c), integração com o Jira (4.9 d) e requisitos não funcionais (4.9 e).
Library             ../libraries/ValoresEsperados.py
Suite Teardown      Salvar Planilha De Valores Esperados
