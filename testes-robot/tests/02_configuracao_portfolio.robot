*** Settings ***
Documentation       Configuração do portfólio — persona Product Owner (Tabela 20; roteiro da seção 4.4.1).
...                 Criação e seleção de portfólio, critérios e pesos, OKRs e Key Results
...                 (RF04 a RF09, RF14 a RF17; UC2 a UC6, UC8 e UC9).
Resource            ../resources/ui.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Suite Teardown      Fechar Navegador
Test Tags           suite:configuracao-portfolio    persona:ProductOwner


*** Variables ***
${PORTFOLIO_ID}     ${EMPTY}
${OKR_ID}           ${EMPTY}


*** Test Cases ***
PO cria um portfólio com nome, descrição, objetivo e responsável
    [Tags]    RF04    UC2    ui    critico
    Abrir Tela    /portfolios
    Clicar No Botao    Novo portfólio
    Preencher    id:nome    ${NOME_PORTFOLIO}
    Preencher    id:descricao    Portfólio do roteiro 4.4.1 executado pelo Robot Framework.
    Preencher    id:objetivo    Validar a configuração do portfólio.
    Selecionar    id:responsavel    ${PO}[id]
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//h2[contains(@class,'portfolio-name') and normalize-space(.)='${NOME_PORTFOLIO}']
    ${id}=    Id Do Portfolio Pelo Nome    ${NOME_PORTFOLIO}
    Set Suite Variable    ${PORTFOLIO_ID}    ${id}

Portfólio sem nome não é cadastrado
    [Tags]    RF04    UC2    api
    ${corpo}=    Create Dictionary    nome=${EMPTY}    responsavelId=${PO}[id]
    ${resposta}=    POST On Session    ProductOwner    /api/portfolios    json=${corpo}    expected_status=400
    Validar Problem Details    ${resposta}    400    Preencha os campos obrigatórios.

PO visualiza os portfólios de que participa
    [Tags]    RF05    UC3    api    ui
    Precisa Do Portfolio
    ${lista}=    GET On Session    ProductOwner    /api/portfolios    expected_status=200
    ${ids}=    Evaluate    [p['id'] for p in $lista.json()]
    Should Contain    ${ids}    ${PORTFOLIO_ID}
    Should Contain    ${ids}    ${PORTFOLIO_VALIDACAO_1}
    Abrir Tela    /portfolios
    Page Should Contain    ${NOME_PORTFOLIO}
    Page Should Contain    Transformação Digital

PO seleciona o portfólio e vê a visão geral
    [Tags]    RF06    UC3    ui    critico
    Precisa Do Portfolio
    Abrir Tela    /portfolios
    Clicar    xpath=//button[contains(@class,'portfolio-card')][.//h2[normalize-space(.)='${NOME_PORTFOLIO}']]
    Url Deve Terminar Com    /portfolios/${PORTFOLIO_ID}/visao-geral
    Wait Until Page Contains    Validar a configuração do portfólio.
    ${ativo}=    Execute Javascript    return sessionStorage.getItem('portfolioAtivoId')
    Should Be Equal    ${ativo}    ${PORTFOLIO_ID}

PO cadastra critérios com tipo e peso
    [Tags]    RF07    UC4    ui    critico
    Precisa Do Portfolio
    Abrir Tela    /portfolios/${PORTFOLIO_ID}/criterios
    Titulo Da Pagina Deve Ser    Critérios de priorização
    Cadastrar Criterio Na Tela    Valor de negócio    Retorno esperado para o negócio.    4    Benefício
    Cadastrar Criterio Na Tela    Esforço    Tamanho do trabalho.    2    Custo
    Cadastrar Criterio Na Tela    Risco    Incerteza técnica e de negócio.    2    Custo
    Criterio Deve Estar Na Tabela    Valor de negócio    4    Benefício
    Criterio Deve Estar Na Tabela    Esforço    2    Custo
    Wait Until Page Contains    A soma dos pesos é 8

Regras do cadastro de critérios são aplicadas
    [Tags]    RF07    UC4    api
    Precisa Do Portfolio
    ${r}=    Criar Criterio Via API    ProductOwner    ${PORTFOLIO_ID}    Peso zero    0    status=400
    Validar Problem Details    ${r}    400    Peso inválido. Informe um valor maior que 0 e até 10.
    ${r}=    Criar Criterio Via API    ProductOwner    ${PORTFOLIO_ID}    ${EMPTY}    1    status=400
    Validar Problem Details    ${r}    400
    ${r}=    Criar Criterio Via API    ProductOwner    ${PORTFOLIO_ID}    Esforço    1    status=409
    Validar Problem Details    ${r}    409    Já existe um critério com este nome neste portfólio.
    ${r}=    Criar Criterio Via API    ProductOwner    ${PORTFOLIO_ID}    Excede a soma    5    status=400
    Validar Problem Details    ${r}    400    A soma dos pesos dos critérios não pode ultrapassar 10.

PO exclui um critério
    [Tags]    RF09    UC6    ui
    Precisa Do Portfolio
    Criar Criterio Via API    ProductOwner    ${PORTFOLIO_ID}    Temporário    2
    Abrir Tela    /portfolios/${PORTFOLIO_ID}/criterios
    Clicar    xpath=//tr[td[normalize-space(.)='Temporário']]//button[normalize-space(.)='Excluir']
    Wait Until Page Contains    Deseja excluir o critério Temporário?
    Clicar    xpath=//*[@role='dialog' or contains(@class,'modal')]//button[normalize-space(.)='Excluir']
    Wait Until Page Does Not Contain Element    xpath=//td[normalize-space(.)='Temporário']

PO edita um critério cadastrado
    [Tags]    RF08    UC5    ui
    Precisa Do Portfolio
    Cadastrar Criterio Na Tela    Alinhamento    Aderência aos OKRs.    1    Benefício
    Clicar    xpath=//tr[td[normalize-space(.)='Alinhamento']]//button[normalize-space(.)='Editar']
    Wait Until Element Is Visible    id:nome
    Preencher    id:nome    Alinhamento estratégico
    Preencher    id:peso    2
    Clicar No Botao    Salvar
    Criterio Deve Estar Na Tabela    Alinhamento estratégico    2    Benefício
    Page Should Not Contain    A soma dos pesos é

PO ajusta os pesos e a soma deve ser exatamente 10
    [Tags]    RF07    RF08    UC5    api
    Precisa Do Portfolio
    ${criterios}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}/criterios    expected_status=200
    ${errados}=    Evaluate    [{'criterioId': c['id'], 'peso': 1} for c in $criterios.json()]
    ${r}=    PUT On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}/criterios/pesos    json=${errados}
    ...    expected_status=400
    Validar Problem Details    ${r}    400    A soma dos pesos dos critérios deve ser exatamente 10.
    ${certos}=    Evaluate    [{'criterioId': c['id'], 'peso': 2.5} for c in $criterios.json()]
    ${r}=    PUT On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}/criterios/pesos    json=${certos}
    ...    expected_status=200
    ${soma}=    Evaluate    sum(c['peso'] for c in $r.json())
    Should Be Equal As Numbers    ${soma}    10
    ${originais}=    Evaluate
    ...    [{'criterioId': c['id'], 'peso': {'Valor de negócio': 4}.get(c['nome'], 2)} for c in $criterios.json()]
    PUT On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}/criterios/pesos    json=${originais}
    ...    expected_status=200

PO cadastra um OKR com Key Results
    [Tags]    RF14    RF15    UC8    ui    critico
    Abrir Tela    /okrs
    Titulo Da Pagina Deve Ser    OKRs
    Clicar No Botao    Novo OKR
    Preencher    id:titulo    ${TITULO_OKR}
    Preencher    id:descricao    OKR do roteiro 4.4.1.
    Preencher    id:kr-desc-0    Clientes ativos no novo canal
    Preencher    id:kr-meta-0    200
    Preencher    id:kr-atual-0    50
    Clicar No Botao    Adicionar KR
    Preencher    id:kr-desc-1    Tempo médio de resposta (inverso)
    Preencher    id:kr-meta-1    10
    Preencher    id:kr-atual-1    10
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//h2[normalize-space(.)='${TITULO_OKR}']
    ${okr}=    Okr Pelo Titulo    ${TITULO_OKR}
    Set Suite Variable    ${OKR_ID}    ${okr}[id]
    Length Should Be    ${okr}[keyResults]    2

OKR sem Key Result é recusado
    [Tags]    RF14    RF15    UC8    api
    ${vazia}=    Create List
    ${corpo}=    Create Dictionary    titulo=OKR sem KR    keyResults=${vazia}
    ${r}=    POST On Session    ProductOwner    /api/okrs    json=${corpo}    expected_status=400
    Validar Problem Details    ${r}    400    Um objetivo precisa de pelo menos um Key Result.

Progresso do OKR segue a fórmula F4
    [Documentation]    progressoKR = min(atual/meta, 1) × 100; progressoOKR = média dos KRs.
    ...    50/200 = 25% e 10/10 = 100% → 62,5%. Depois de atualizar o 1º KR para 150: 75% → 87,5%.
    [Tags]    RF17    UC8    ui    api    calculo
    Skip If    not $OKR_ID    Depende do cadastro do OKR.
    ${okr}=    Okr Pelo Titulo    ${TITULO_OKR}
    Comparar Valor    okr    progresso inicial    ${62.5}    ${okr}[progresso]
    Abrir Tela    /okrs
    Clicar    xpath=//article[.//h2[normalize-space(.)='${TITULO_OKR}']]//div[@role='button']
    Clicar    xpath=//tr[td[normalize-space(.)='Clientes ativos no novo canal']]//button[normalize-space(.)='Atualizar valor']
    ${campo}=    Set Variable    xpath=//tr[td[normalize-space(.)='Clientes ativos no novo canal']]//input
    Wait Until Element Is Visible    ${campo}
    Clear Element Text    ${campo}
    Preencher    ${campo}    150
    Clicar    xpath=//tr[td[normalize-space(.)='Clientes ativos no novo canal']]//button[normalize-space(.)='Salvar']
    Wait Until Element Contains    xpath=//article[.//h2[normalize-space(.)='${TITULO_OKR}']]//div[contains(@class,'okr-progress')]
    ...    87,5
    ${okr}=    Okr Pelo Titulo    ${TITULO_OKR}
    Comparar Valor    okr    progresso após atualização    ${87.5}    ${okr}[progresso]

Progresso dos OKRs da massa de validação confere com a planilha
    [Tags]    RF17    api    calculo
    ${okrs}=    GET On Session    ProductOwner    /api/okrs    expected_status=200
    FOR    ${titulo}    IN    Aumentar a satisfação do cliente nos canais digitais    Reduzir o custo operacional
    ...    Garantir a continuidade dos sistemas críticos
        ${okr}=    Item Com    ${okrs.json()}    titulo    ${titulo}
        ${esperado}=    Progresso Okr Esperado    ${titulo}
        Comparar Valor    okr    ${titulo}    ${esperado}    ${okr}[progresso]
    END

PO associa o OKR ao portfólio
    [Tags]    RF16    UC9    ui    critico
    Precisa Do Portfolio
    Skip If    not $OKR_ID    Depende do cadastro do OKR.
    Abrir Tela    /portfolios/${PORTFOLIO_ID}/visao-geral
    ${seletor}=    Set Variable    xpath=//*[.//h2[normalize-space(.)='OKRs do portfólio']]//select
    Wait Until Element Is Visible    ${seletor}
    Select From List By Label    ${seletor}    ${TITULO_OKR}
    Clicar No Botao    Associar OKR
    Wait Until Element Is Visible    xpath=//*[.//h2[normalize-space(.)='OKRs do portfólio']]//*[normalize-space(.)='${TITULO_OKR}']
    ${associados}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}/okrs    expected_status=200
    ${ids}=    Evaluate    [o['id'] for o in $associados.json()]
    Should Contain    ${ids}    ${OKR_ID}

Sistema apresenta corretamente os dados cadastrados
    [Tags]    RF04    RF06    RF07    UC3    api
    Precisa Do Portfolio
    ${p}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}    expected_status=200
    Should Be Equal    ${p.json()}[nome]    ${NOME_PORTFOLIO}
    Should Be Equal    ${p.json()}[responsavelId]    ${PO}[id]
    Should Be Equal As Integers    ${p.json()}[quantidadeCriterios]    4
    ${criterios}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO_ID}/criterios    expected_status=200
    ${resumo}=    Evaluate    sorted((c['nome'], c['tipo'], c['peso']) for c in $criterios.json())
    ${esperado}=    Evaluate
    ...    sorted([('Alinhamento estratégico','Beneficio',2), ('Esforço','Custo',2), ('Risco','Custo',2), ('Valor de negócio','Beneficio',4)])
    Should Be Equal    ${resumo}    ${esperado}


*** Keywords ***
Preparar Suite
    ${po}=    Entrar Na API Como    ProductOwner
    Set Suite Variable    ${PO}    ${po}
    ${nome}=    Nome Unico    Portfolio PO
    Set Suite Variable    ${NOME_PORTFOLIO}    ${nome}
    ${titulo}=    Nome Unico    OKR Expandir canais digitais
    Set Suite Variable    ${TITULO_OKR}    ${titulo}
    Preparar Oraculo Da Massa
    Abrir Navegador
    Entrar Na Interface Como    ProductOwner    /portfolios

Preparar Oraculo Da Massa
    ${p}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]

Precisa Do Portfolio
    Skip If    not $PORTFOLIO_ID    Depende do cadastro do portfólio pela interface.

Id Do Portfolio Pelo Nome
    [Arguments]    ${nome}
    ${lista}=    GET On Session    ProductOwner    /api/portfolios    expected_status=200
    ${portfolio}=    Item Com    ${lista.json()}    nome    ${nome}
    RETURN    ${portfolio}[id]

Okr Pelo Titulo
    [Arguments]    ${titulo}
    ${okrs}=    GET On Session    ProductOwner    /api/okrs    expected_status=200
    ${okr}=    Item Com    ${okrs.json()}    titulo    ${titulo}
    RETURN    ${okr}

Cadastrar Criterio Na Tela
    [Arguments]    ${nome}    ${descricao}    ${peso}    ${tipo}
    Abrir Tela    /portfolios/${PORTFOLIO_ID}/criterios
    Clicar No Botao    Novo critério
    Wait Until Element Is Visible    id:nome
    Preencher    id:nome    ${nome}
    Preencher    id:descricao    ${descricao}
    Preencher    id:peso    ${peso}
    Marcar Opcao    ${tipo}
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//td[normalize-space(.)='${nome}']

Criterio Deve Estar Na Tabela
    [Arguments]    ${nome}    ${peso}    ${tipo}
    ${linha}=    Set Variable    xpath=//tr[td[normalize-space(.)='${nome}']]
    Wait Until Element Is Visible    ${linha}
    Element Should Contain    ${linha}/td[3]    ${peso}
    Element Should Contain    ${linha}/td[4]    ${tipo}
