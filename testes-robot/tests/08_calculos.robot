*** Settings ***
Documentation       Verificação dos cálculos (seção 4.9 c).
...                 Os indicadores da API para a massa de dados sintéticos (seção 4.3) são comparados com os
...                 valores esperados calculados de forma independente (oraculo/), com diferença máxima de 0,01.
...                 Cada comparação vira uma linha da planilha results/valores_esperados.xlsx (evidência).
...                 Os mesmos valores são conferidos na tela pela suíte 07 (Diretoria).
Resource            ../resources/api.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Test Tags           suite:calculos    api    calculo
Test Template       Indicador Deve Conferir Com A Planilha


*** Test Cases ***                                      INDICADOR               PORTFÓLIO
Burn Rate — Transformação Digital                       burnRate                ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF24    RF36    critico
Burn Rate — Eficiência Operacional                      burnRate                ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF24    RF36    critico
Saúde do portfólio — Transformação Digital              saude                   ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF35    critico
Saúde do portfólio — Eficiência Operacional             saude                   ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF35    critico
Alocação estratégica — Transformação Digital            alocacaoEstrategica     ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF37
Alocação estratégica — Eficiência Operacional           alocacaoEstrategica     ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF37
Qualidade da entrega — Transformação Digital            qualidade               ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF34    RF38
Qualidade da entrega — Eficiência Operacional           qualidade               ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF34    RF38
Capacidade e ocupação — Transformação Digital           capacidade              ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF32    RF39
Capacidade e ocupação — Eficiência Operacional          capacidade              ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF32    RF39
Lead time — Transformação Digital                       leadTime                ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF40
Lead time — Eficiência Operacional                      leadTime                ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF40
VPL esperado e realizado — Transformação Digital        vpl                     ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF25    RF41    critico
VPL esperado e realizado — Eficiência Operacional       vpl                     ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF25    RF41    critico
Alinhamento a OKRs — Transformação Digital              alinhamentoOkr          ${PORTFOLIO_VALIDACAO_1}
    [Tags]    RF17    RF42
Alinhamento a OKRs — Eficiência Operacional             alinhamentoOkr          ${PORTFOLIO_VALIDACAO_2}
    [Tags]    RF17    RF42

Score e ranking — Transformação Digital
    [Tags]    RF19    RF20    critico
    [Template]    Ranking Deve Conferir Com A Planilha
    ${PORTFOLIO_VALIDACAO_1}
Score e ranking — Eficiência Operacional
    [Tags]    RF19    RF20    critico
    [Template]    Ranking Deve Conferir Com A Planilha
    ${PORTFOLIO_VALIDACAO_2}

VPL de cada projeto com business case
    [Tags]    RF25
    [Template]    NONE
    FOR    ${nome}    IN    Portal do Cliente    App Mobile    Manutenção do ERP
        ${id}=    Id Do Projeto Pelo Nome    Validacao    ${PORTFOLIO_VALIDACAO_1}    ${nome}
        ${r}=    GET On Session    Validacao    /api/projetos/${id}/indicadores    expected_status=200
        ${e}=    Vpl Esperado Do Projeto    ${nome}
        Comparar Valor    vpl projeto    ${nome}.vplEsperado    ${e}[vplEsperado]    ${r.json()}[vpl][vplEsperado]
        Comparar Valor    vpl projeto    ${nome}.vplRealizado    ${e}[vplRealizado]    ${r.json()}[vpl][vplRealizado]
    END

Burn Rate de cada projeto
    [Tags]    RF24
    [Template]    NONE
    FOR    ${nome}    IN    Portal do Cliente    App Mobile    Migração para Nuvem    Chatbot de Atendimento
    ...    Manutenção do ERP    BI Comercial
        ${id}=    Id Do Projeto Pelo Nome    Validacao    ${PORTFOLIO_VALIDACAO_1}    ${nome}
        ${r}=    GET On Session    Validacao    /api/projetos/${id}/indicadores    expected_status=200
        ${e}=    Burn Rate Esperado Do Projeto    ${nome}
        FOR    ${campo}    IN    custoHoras    custoLancamentos    custoRealizado    burnRateMensal    projecaoCustoTotal
        ...    percentualConsumido    saldo    estouro    riscoEstouro
            Comparar Valor    burnRate projeto    ${nome}.${campo}    ${e}[${campo}]    ${r.json()}[burnRate][${campo}]
        END
    END


*** Keywords ***
Preparar Suite
    Entrar Na API Como    Validacao
    ${p}=    GET On Session    Validacao    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]
    ${dashboards}=    Create Dictionary
    FOR    ${id}    IN    ${PORTFOLIO_VALIDACAO_1}    ${PORTFOLIO_VALIDACAO_2}
        ${d}=    GET On Session    Validacao    /api/portfolios/${id}/dashboard    expected_status=200
        Set To Dictionary    ${dashboards}    ${id}=${d.json()}
    END
    Set Suite Variable    ${DASHBOARDS}    ${dashboards}

Indicador Deve Conferir Com A Planilha
    [Arguments]    ${indicador}    ${portfolio}
    ${resposta}=    Set Variable    ${DASHBOARDS}[${portfolio}][${indicador}]
    Should Be True    ${resposta}[disponivel]    ${indicador}: ${resposta}[motivo]
    Comparar Indicador    ${indicador}    ${resposta}    ${portfolio}

Ranking Deve Conferir Com A Planilha
    [Arguments]    ${portfolio}
    ${r}=    GET On Session    Validacao    /api/portfolios/${portfolio}/ranking    expected_status=200
    Comparar Ranking    ${r.json()}    ${portfolio}
