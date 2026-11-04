*** Settings ***
Documentation       Análise estratégica — persona Diretoria (Tabela 20; roteiro da seção 4.4.3, Parte 3).
...                 Dashboard com os oito indicadores conferidos na tela contra a planilha de valores
...                 esperados, relatório executivo e notificações (RF35 a RF42, RF45 e RF46; UC11).
Resource            ../resources/ui.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Suite Teardown      Encerrar Suite
Test Tags           suite:analise-estrategica    persona:Diretoria


*** Test Cases ***
Diretoria acessa o dashboard com os oito indicadores
    [Tags]    RF35    RF36    RF37    RF38    RF39    RF40    RF41    RF42    UC11    ui    critico
    Abrir Dashboard
    FOR    ${titulo}    IN    Burn Rate    Saúde do portfólio    Alocação estratégica    Qualidade da entrega
    ...    Capacidade das equipes    Lead time    VPL esperado × realizado    Alinhamento a OKRs
        Wait Until Element Is Visible    xpath=//article//h2[normalize-space(.)='${titulo}']
        Page Should Not Contain Element    xpath=//article[.//h2[normalize-space(.)='${titulo}']]//p[contains(@class,'empty-state')]
    END

Gráficos do dashboard são exibidos
    [Tags]    RF35    RF37    RF41    UC11    ui
    Abrir Dashboard
    FOR    ${titulo}    IN    Saúde do portfólio    Alocação estratégica    VPL esperado × realizado
        Wait Until Element Is Visible    xpath=//article[.//h2[normalize-space(.)='${titulo}']]//canvas
    END

Burn Rate exibido confere com a planilha
    [Tags]    RF36    RF24    UC11    ui    calculo    critico
    Abrir Dashboard
    ${e}=    Valores Esperados    burnRate
    ${percentual}=    Valor Do Card    Burn Rate
    Comparar Valor    burnRate    percentualConsumido (tela)    ${e}[percentualConsumido]    ${percentual}    origem=UI
    ${texto}=    Texto Do Card    Burn Rate
    ${numeros}=    Numeros Brasileiros    ${texto}
    # "Realizado R$ X de R$ Y · burn mensal R$ Z · projeção R$ W"
    Should Contain    ${numeros}    ${e}[custoRealizado]
    Should Contain    ${numeros}    ${e}[orcamentoAprovado]
    Should Contain    ${numeros}    ${e}[burnRateMensal]
    Should Contain    ${numeros}    ${e}[projecaoCustoTotal]

Saúde do portfólio exibida confere com a planilha
    [Tags]    RF35    UC11    ui    calculo    critico
    Abrir Dashboard
    ${e}=    Valores Esperados    saude
    ${saude}=    Valor Do Card    Saúde do portfólio
    Comparar Valor    saude    saude (tela)    ${e}[saude]    ${saude}    origem=UI
    ${texto}=    Texto Do Card    Saúde do portfólio
    Should Contain    ${texto}    ${e}[classificacao]

Alocação estratégica exibida confere com a planilha
    [Tags]    RF37    UC11    ui    api    calculo
    Abrir Dashboard
    ${e}=    Valores Esperados    alocacaoEstrategica
    ${texto}=    Texto Do Card    Alocação estratégica
    ${total}=    Numero Brasileiro    ${texto}
    Comparar Valor    alocacaoEstrategica    orcamentoTotal (tela)    ${e}[orcamentoTotal]    ${total}    origem=UI
    ${r}=    GET On Session    Diretoria    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/indicadores/alocacaoEstrategica
    ...    expected_status=200
    Comparar Indicador    alocacaoEstrategica    ${r.json()}

Qualidade da entrega exibida confere com a planilha
    [Tags]    RF38    RF34    UC11    ui    calculo
    Abrir Dashboard
    ${e}=    Valores Esperados    qualidade
    ${indice}=    Valor Do Card    Qualidade da entrega
    Comparar Valor    qualidade    indiceQualidade (tela)    ${e}[indiceQualidade]    ${indice}    origem=UI
    ${texto}=    Texto Do Card    Qualidade da entrega
    Should Contain    ${texto}    ${{ 'Meta atingida' if $e['atingiuMeta'] else 'Meta não atingida' }}

Capacidade das equipes exibida confere com a planilha
    [Tags]    RF39    RF32    UC11    ui    calculo
    Abrir Dashboard
    ${e}=    Valores Esperados    capacidade
    ${texto}=    Texto Do Card    Capacidade das equipes
    Should Contain    ${texto}    Mês ${e}[mes]
    Should Contain    ${texto}    ${e}[classificacao]
    ${numeros}=    Numeros Brasileiros    ${texto}
    Should Contain    ${numeros}    ${e}[ocupacao]

Lead time exibido confere com a planilha
    [Tags]    RF40    UC11    ui    calculo
    Abrir Dashboard
    ${e}=    Valores Esperados    leadTime
    ${mediana}=    Valor Do Card    Lead time
    Comparar Valor    leadTime    leadTimeMediano (tela)    ${e}[leadTimeMediano]    ${mediana}    origem=UI
    ${texto}=    Texto Do Card    Lead time
    ${numeros}=    Numeros Brasileiros    ${texto}
    Should Contain    ${numeros}    ${e}[leadTimeMedio]
    Should Contain    ${numeros}    ${e}[p85]

VPL esperado e realizado exibidos conferem com a planilha
    [Tags]    RF41    RF25    UC11    ui    calculo    critico
    Abrir Dashboard
    ${e}=    Valores Esperados    vpl
    ${texto}=    Texto Do Card    VPL esperado × realizado
    ${numeros}=    Numeros Brasileiros    ${texto}
    Comparar Valor    vpl    vplEsperado (tela)    ${e}[vplEsperado]    ${numeros}[0]    origem=UI
    Comparar Valor    vpl    vplRealizado (tela)    ${e}[vplRealizado]    ${numeros}[1]    origem=UI

Alinhamento a OKRs exibido confere com a planilha
    [Tags]    RF42    RF17    UC11    ui    calculo
    Abrir Dashboard
    ${e}=    Valores Esperados    alinhamentoOkr
    ${projetos}=    Valor Do Card    Alinhamento a OKRs    indice=1
    ${orcamento}=    Valor Do Card    Alinhamento a OKRs    indice=2
    Comparar Valor    alinhamentoOkr    percentualProjetosAlinhados (tela)    ${e}[percentualProjetosAlinhados]    ${projetos}    origem=UI
    Comparar Valor    alinhamentoOkr    percentualOrcamentoAlinhado (tela)    ${e}[percentualOrcamentoAlinhado]    ${orcamento}    origem=UI
    ${texto}=    Texto Do Card    Alinhamento a OKRs
    FOR    ${nome}    IN    @{e}[desalinhados]
        Should Contain    ${texto}    ${nome}
    END

Projetos com maior prioridade aparecem no topo do ranking
    [Tags]    RF20    UC11    ui    calculo
    Abrir Dashboard
    ${ranking}=    Ranking Esperado
    ${top5}=    Get Slice From List    ${ranking}    0    5
    FOR    ${item}    IN    @{top5}
        ${linha}=    Set Variable    xpath=//div[contains(@class,'card')][h2[normalize-space(.)='Top 5 do ranking']]//tbody/tr[${item}[posicao]]
        Element Should Contain    ${linha}/td[2]    ${item}[nome]
        ${score}=    Get Text    ${linha}/td[3]
        ${score}=    Numero Brasileiro    ${score}
        Comparar Valor    ranking    top5 ${item}[posicao]º (tela)    ${item}[score]    ${score}    origem=UI
    END

Dashboard filtrado por período recalcula os indicadores de fluxo
    [Documentation]    Lead time e qualidade dependem do período (De/Até); fora da janela não há issues concluídas.
    [Tags]    RF40    RF38    UC11    api
    ${r}=    GET On Session    Diretoria    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/dashboard
    ...    params=de=2000-01-01&ate=2000-12-31    expected_status=200
    Should Not Be True    ${r.json()}[leadTime][disponivel]
    Should Be Equal    ${r.json()}[leadTime][motivo]    Dados insuficientes para calcular este indicador.

Diretoria gera o relatório executivo em PDF e Excel
    [Tags]    RF43    RF44    UC11    api    critico
    ${pdf}=    GET On Session    Diretoria    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/relatorios/executivo
    ...    params=formato=pdf    expected_status=200
    Should Be True    $pdf.content.startswith(b'%PDF')
    ${excel}=    GET On Session    Diretoria    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/relatorios/executivo
    ...    params=formato=excel    expected_status=200
    Should Be True    $excel.content.startswith(b'PK')
    ${texto}=    Evaluate
    ...    ' '.join(str(c) for ws in openpyxl.load_workbook(io.BytesIO($excel.content)).worksheets for r in ws.iter_rows(values_only=True) for c in r if c is not None)
    ...    modules=openpyxl,io
    Should Contain    ${texto}    Transformação Digital

Relatório executivo pela tela
    [Tags]    RF43    RF44    UC11    ui
    Abrir Tela    /portfolios/${PORTFOLIO_VALIDACAO_1}/relatorios
    Titulo Da Pagina Deve Ser    Relatórios
    Clicar    xpath=//article[.//h2[normalize-space(.)='Relatório executivo']]//button[contains(normalize-space(.),'PDF')]
    Wait Until Element Is Enabled    xpath=//article[.//h2[normalize-space(.)='Relatório executivo']]//button[contains(normalize-space(.),'PDF')]
    ...    timeout=30s
    Wait Until Page Contains    Executivo

Notificações automáticas chegam a quem deve recebê-las
    [Documentation]    Regras do RF45 aplicadas à massa no dia da carga: atraso e risco ao responsável, desalinhamento
    ...    ao PO, estouro de orçamento à Diretoria e conflito de capacidade ao Tech Lead.
    [Tags]    RF45    api    calculo    critico
    @{tipos}=    Create List
    FOR    ${persona}    IN    Diretoria    GerenteProjeto    ProductOwner    TechLead
        Entrar Na API Como    ${persona}
        ${recebidas}=    GET On Session    ${persona}    /api/notificacoes    expected_status=200
        ${esperadas}=    Notificacoes Esperadas    ${persona}
        FOR    ${tipo}    ${nome}    IN    @{{ [x for par in $esperadas for x in par] }}
            ${achou}=    Existe Item    ${recebidas.json()}    tipo    ${tipo}    mensagem    ${nome}
            Comparar Valor    notificacao    ${persona}: ${tipo} — ${nome}    ${True}    ${achou}
            Append To List    ${tipos}    ${tipo}
        END
    END
    FOR    ${tipo}    IN    Atraso    Risco    Conflito    Desalinhamento    EstouroOrcamento
        Should Contain    ${tipos}    ${tipo}
    END

Diretoria consulta a central de notificações e marca uma como lida
    [Tags]    RF46    ui    api
    ${antes}=    GET On Session    Diretoria    /api/notificacoes/nao-lidas/contagem    expected_status=200
    Skip If    ${antes.json()}[quantidade] == 0    Não há notificações não lidas (recarregue a massa de validação).
    Abrir Tela    /notificacoes
    Titulo Da Pagina Deve Ser    Notificações
    Wait Until Element Is Visible    css:article.notification.unread
    Clicar    xpath=(//article[contains(@class,'unread')]//button[normalize-space(.)='Marcar como lida'])[1]
    Wait Until Keyword Succeeds    10s    500ms    Nao Lidas Deve Ser    ${{ $antes.json()['quantidade'] - 1 }}


*** Keywords ***
Preparar Suite
    Entrar Na API Como    Diretoria
    ${p}=    GET On Session    Diretoria    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]
    Abrir Navegador
    Entrar Na Interface Como    Diretoria    /portfolios
    Ativar Portfolio    ${PORTFOLIO_VALIDACAO_1}

Encerrar Suite
    Fechar Navegador

Abrir Dashboard
    ${url}=    Get Location
    IF    not $url.endswith('/dashboard')
        Abrir Tela    /portfolios/${PORTFOLIO_VALIDACAO_1}/dashboard
        Wait Until Element Is Visible    xpath=//article//h2[normalize-space(.)='Burn Rate']
    END

Nao Lidas Deve Ser
    [Arguments]    ${quantidade}
    ${r}=    GET On Session    Diretoria    /api/notificacoes/nao-lidas/contagem    expected_status=200
    Should Be Equal As Integers    ${r.json()}[quantidade]    ${quantidade}
