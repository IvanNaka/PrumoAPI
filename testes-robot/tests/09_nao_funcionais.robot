*** Settings ***
Documentation       Requisitos não funcionais (seção 4.9 e; Tabela 21).
...                 Compatibilidade: rode a suíte inteira com BROWSER=chrome, firefox e edge (ver README/CI).
...                 Responsividade nas resoluções médias e grandes (seção 4.2), desempenho do dashboard
...                 (até 3 s com a massa de teste) e da priorização, e acessibilidade com axe-core (WCAG 2.1 AA).
Resource            ../resources/ui.resource
Library             ../libraries/Acessibilidade.py
Library             ../libraries/Medicoes.py
Suite Setup         Preparar Suite
Suite Teardown      Fechar Navegador
Test Tags           suite:nao-funcionais


*** Variables ***
@{TELAS}            /portfolios    /portfolios/${PORTFOLIO_VALIDACAO_1}/visao-geral
...                 /portfolios/${PORTFOLIO_VALIDACAO_1}/dashboard    /portfolios/${PORTFOLIO_VALIDACAO_1}/priorizacao
...                 /portfolios/${PORTFOLIO_VALIDACAO_1}/projetos    /okrs    /equipes    /notificacoes


*** Test Cases ***
Telas principais se adaptam às resoluções médias e grandes
    [Documentation]    Sem rolagem horizontal da página e com o conteúdo principal visível.
    [Tags]    rnf:responsividade    ui
    FOR    ${largura}    ${altura}    IN    @{RESOLUCOES_PLANAS}
        Set Window Size    ${largura}    ${altura}
        FOR    ${tela}    IN    @{TELAS}
            Abrir Tela    ${tela}
            Wait Until Element Is Visible    css:.page-header h1
            Sem Rolagem Horizontal    ${tela} em ${largura}x${altura}
        END
    END
    [Teardown]    Set Window Size    ${LARGURA_JANELA}    ${ALTURA_JANELA}

Dashboard carrega em até 3 segundos com a massa de teste
    [Documentation]    Do carregamento da página até os oito indicadores visíveis (média de 3 medições).
    [Tags]    rnf:desempenho    ui    critico
    @{tempos}=    Create List
    FOR    ${i}    IN RANGE    3
        Go To    ${FRONT_URL}/portfolios
        Iniciar Cronometro    dashboard
        Go To    ${FRONT_URL}/portfolios/${PORTFOLIO_VALIDACAO_1}/dashboard
        Wait Until Element Is Visible    xpath=//article[.//h2[normalize-space(.)='Alinhamento a OKRs']]//*[contains(@class,'kpi-value')]
        Wait Until Element Is Visible    xpath=//article[.//h2[normalize-space(.)='Burn Rate']]//*[contains(@class,'kpi-value')]
        ${t}=    Parar Cronometro    dashboard
        Append To List    ${tempos}    ${t}
    END
    ${media}=    Evaluate    round(sum($tempos) / len($tempos), 3)
    Log    Medições: ${tempos}
    Registrar Medicao    Carregamento do dashboard (média de 3)    ${media}    ${LIMITE_DASHBOARD_SEGUNDOS}    ${BROWSER}

Tempo de resposta da API do dashboard
    [Tags]    rnf:desempenho    api
    ${r}=    GET On Session    Validacao    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/dashboard    expected_status=200
    Registrar Medicao    GET /dashboard (API)    ${r.elapsed.total_seconds()}    ${LIMITE_DASHBOARD_SEGUNDOS}

Tempo de execução da priorização com 10 projetos
    [Documentation]    Mede POST /priorizacao para um portfólio com 4 critérios e 10 projetos avaliados.
    [Tags]    rnf:desempenho    api    RF19
    ${po}=    Entrar Na API Como    ProductOwner
    ${gerente}=    Entrar Na API Como    GerenteProjeto
    ${nome}=    Nome Unico    Portfolio Desempenho
    ${portfolio}=    Criar Portfolio Via API    ProductOwner    ${nome}    ${po}[id]
    Adicionar Membro Ao Portfolio    ProductOwner    ${portfolio}[id]    ${gerente}[id]
    @{criterios}=    Create List
    FOR    ${c}    ${peso}    ${tipo}    IN    Valor    4    Beneficio    Esforço    2    Custo    Risco    2    Custo
    ...    Alinhamento    2    Beneficio
        ${r}=    Criar Criterio Via API    ProductOwner    ${portfolio}[id]    ${c}    ${peso}    ${tipo}
        Append To List    ${criterios}    ${r.json()}[id]
    END
    FOR    ${i}    IN RANGE    10
        ${r}=    Criar Projeto Via API    GerenteProjeto    ${portfolio}[id]    Projeto ${i}    ${gerente}[id]
        ${notas}=    Evaluate    {c: ($i + k) % 5 + 1 for k, c in enumerate($criterios)}
        Avaliar Projeto    ProductOwner    ${r.json()}[id]    ${notas}
    END
    ${r}=    POST On Session    ProductOwner    /api/portfolios/${portfolio}[id]/priorizacao    expected_status=200
    Registrar Medicao    POST /priorizacao (10 projetos)    ${r.elapsed.total_seconds()}

Telas principais sem violações críticas ou graves de acessibilidade
    [Documentation]    axe-core com as regras WCAG 2.0/2.1 A e AA; o detalhe de cada tela fica em
    ...    results/acessibilidade/*.json. Violações moderadas e menores aparecem no log como informação.
    [Tags]    rnf:acessibilidade    ui    critico
    @{falhas}=    Create List
    Abrir Tela    /login
    Analisar Tela    login    ${falhas}
    Entrar Na Interface Como    Validacao    /portfolios
    Ativar Portfolio    ${PORTFOLIO_VALIDACAO_1}
    FOR    ${tela}    IN    @{TELAS}
        Abrir Tela    ${tela}
        Wait Until Element Is Visible    css:.page-header h1
        ${nome}=    Evaluate    $tela.strip('/').replace('/', '_').replace($PORTFOLIO_VALIDACAO_1, 'p1')
        Analisar Tela    ${nome}    ${falhas}
    END
    Should Be Empty    ${falhas}    Telas com violações críticas/graves: ${falhas}


*** Keywords ***
Preparar Suite
    Entrar Na API Como    Validacao
    @{planas}=    Evaluate    [x for r in $RESOLUCOES for x in r]
    Set Suite Variable    @{RESOLUCOES_PLANAS}    @{planas}
    Abrir Navegador
    Entrar Na Interface Como    Validacao    /portfolios
    Ativar Portfolio    ${PORTFOLIO_VALIDACAO_1}

Sem Rolagem Horizontal
    [Arguments]    ${contexto}
    ${excesso}=    Execute Javascript
    ...    return document.documentElement.scrollWidth - document.documentElement.clientWidth;
    Should Be True    ${excesso} <= 1    ${contexto}: a página rola ${excesso}px na horizontal.

Analisar Tela
    [Arguments]    ${nome}    ${falhas}
    ${violacoes}=    Analisar Acessibilidade    ${nome}
    ${status}    ${erro}=    Run Keyword And Ignore Error    Nao Deve Haver Violacoes Criticas Ou Graves    ${violacoes}
    IF    $status == 'FAIL'
        Log    ${nome}: ${erro}    WARN
        Append To List    ${falhas}    ${nome}
    END
