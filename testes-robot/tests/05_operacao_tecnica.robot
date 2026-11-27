*** Settings ***
Documentation       Operação técnica — persona Tech Lead (Tabela 20; roteiros da seção 4.6.2).
...                 Dependências entre projetos, equipes, colaboradores e capacidade
...                 (RF26 a RF32; UC12 e UC13).
Resource            ../resources/ui.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Suite Teardown      Fechar Navegador
Test Tags           suite:operacao-tecnica    persona:TechLead


*** Variables ***
${EQUIPE_ID}        ${EMPTY}


*** Test Cases ***
Tech Lead cadastra uma dependência entre projetos
    [Tags]    RF26    UC12    ui    critico
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/dependencias
    Titulo Da Pagina Deve Ser    Dependências
    Clicar No Botao    Nova dependência
    Selecionar    id:origem    ${PROJETOS}[Integração]
    Selecionar    id:destino    ${PROJETOS}[Plataforma]
    Preencher    id:descricao    Integração usa a API da plataforma.
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//tr[contains(., 'Integração') and contains(., 'Plataforma')]

Tech Lead visualiza as dependências do portfólio
    [Tags]    RF27    UC12    ui    api
    ${lista}=    GET On Session    TechLead    /api/portfolios/${PORTFOLIO}[id]/dependencias    expected_status=200
    ${pares}=    Evaluate    [(d['projetoOrigemNome'], d['projetoDestinoNome']) for d in $lista.json()]
    Should Contain    ${pares}    ${{ ('Integração', 'Plataforma') }}
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/dependencias
    Wait Until Page Contains    1 dependência(s)

Projeto não pode depender de si mesmo
    [Tags]    RF26    UC12    api
    ${r}=    Criar Dependencia    Plataforma    Plataforma    expected=any
    Validar Problem Details    ${r}    ${r.status_code}    Um projeto não pode depender de si mesmo.

Dependência repetida é recusada
    [Tags]    RF26    UC12    api
    ${r}=    Criar Dependencia    Integração    Plataforma    expected=any
    Validar Problem Details    ${r}    ${r.status_code}    Esta dependência já existe.

Dependência circular é recusada (F14)
    [Documentation]    Integração → Plataforma e Plataforma → Relatórios já existem; Relatórios → Integração fecha o ciclo.
    [Tags]    RF26    RF28    UC12    api    critico
    Criar Dependencia    Plataforma    Relatórios
    ${r}=    Criar Dependencia    Relatórios    Integração    expected=any
    Validar Problem Details    ${r}    ${r.status_code}    Dependência circular não permitida.

Dependência de projeto suspenso fica em risco (F13)
    [Tags]    RF28    UC12    ui    api    critico
    Dependencia Deve Estar Em Risco    Integração    Plataforma    ${False}
    Alterar Status Do Projeto    GerenteProjeto    ${PROJETOS}[Plataforma]    Suspender
    Dependencia Deve Estar Em Risco    Integração    Plataforma    ${True}    Projeto dependente está suspenso
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/dependencias
    Wait Until Element Is Visible    xpath=//tr[contains(., 'Integração') and contains(., 'Plataforma')]//span[normalize-space(.)='Em risco']
    [Teardown]    Alterar Status Do Projeto    GerenteProjeto    ${PROJETOS}[Plataforma]    Retomar

Dependência de projeto que termina depois fica em risco (F13)
    [Documentation]    Plataforma termina em 200 dias e depende de Relatórios, que termina em 300.
    [Tags]    RF28    UC12    api
    Dependencia Deve Estar Em Risco    Plataforma    Relatórios    ${True}    Projeto dependente termina depois deste projeto

Riscos das dependências da massa de validação conferem com a planilha
    [Tags]    RF27    RF28    api    calculo
    Entrar Na API Como    Validacao
    ${p}=    GET On Session    Validacao    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]
    FOR    ${portfolio}    IN    ${PORTFOLIO_VALIDACAO_1}    ${PORTFOLIO_VALIDACAO_2}
        ${esperado}=    Dependencias Em Risco Esperadas    ${portfolio}
        ${lista}=    GET On Session    Validacao    /api/portfolios/${portfolio}/dependencias    expected_status=200
        FOR    ${d}    IN    @{lista.json()}
            ${chave}=    Set Variable    ${d}[projetoOrigemNome] → ${d}[projetoDestinoNome]
            Comparar Valor    dependencia    ${chave}    ${esperado}[${chave}]    ${d}[emRisco]
        END
    END

Tech Lead cadastra uma equipe
    [Tags]    RF29    UC13    ui    critico
    Abrir Tela    /equipes
    Titulo Da Pagina Deve Ser    Equipes
    Clicar No Botao    Nova equipe
    Preencher    id:nomeEquipe    ${NOME_EQUIPE}
    Selecionar    id:portfolioEquipe    ${PORTFOLIO}[id]
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//h2[normalize-space(.)='${NOME_EQUIPE}']
    ${equipes}=    GET On Session    TechLead    /api/equipes    expected_status=200
    ${equipe}=    Item Com    ${equipes.json()}    nome    ${NOME_EQUIPE}
    Set Suite Variable    ${EQUIPE_ID}    ${equipe}[id]

Tech Lead cadastra colaboradores com custo e capacidade mensal
    [Tags]    RF30    RF31    UC13    ui    critico
    Precisa Da Equipe
    Abrir Tela    /equipes
    Adicionar Membro Na Tela    Bianca Backend    ${EMAIL_BIANCA}    120    160
    Adicionar Membro Na Tela    Caio Frontend    ${EMAIL_CAIO}    100    40
    ${equipe}=    GET On Session    TechLead    /api/equipes/${EQUIPE_ID}    expected_status=200
    ${capacidades}=    Evaluate    sorted(m['capacidadeMensalHoras'] for m in $equipe.json()['membros'])
    Should Be Equal    ${capacidades}    ${{ [40, 160] }}

Capacidade mensal fora de 1 a 300 horas é recusada
    [Tags]    RF31    UC13    api
    Precisa Da Equipe
    FOR    ${horas}    IN    0    301
        ${corpo}=    Create Dictionary    nome=Inválido    email=invalido.${horas}@prumo.test    custoHora=${10}
        ...    capacidadeMensalHoras=${horas}
        ${r}=    POST On Session    TechLead    /api/equipes/${EQUIPE_ID}/membros    json=${corpo}    expected_status=400
    END

Ocupação da equipe nova segue a fórmula F9
    [Documentation]    Sem issues: demanda 0 e ocupação 0% (Subutilizada); capacidade = 160 + 40 = 200 h.
    [Tags]    RF32    UC13    api    ui    calculo
    Precisa Da Equipe
    ${r}=    GET On Session    TechLead    /api/equipes/${EQUIPE_ID}/capacidade    expected_status=200
    Comparar Valor    capacidade    equipe nova.capacidade    ${200}    ${r.json()}[capacidade]
    Comparar Valor    capacidade    equipe nova.ocupacao    ${0}    ${r.json()}[ocupacao]
    Comparar Valor    capacidade    equipe nova.classificacao    Subutilizada    ${r.json()}[classificacao]
    Abrir Tela    /equipes/${EQUIPE_ID}/capacidade
    Wait Until Page Contains    200 h

Capacidade das equipes da massa de validação confere com a planilha
    [Documentation]    Squad Digital: 400 h de capacidade e demanda das issues abertas dos membros (F9).
    [Tags]    RF32    UC13    api    ui    calculo    critico
    Entrar Na API Como    Validacao
    ${p}=    GET On Session    Validacao    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]
    ${equipes}=    GET On Session    Validacao    /api/equipes    expected_status=200
    FOR    ${nome}    IN    Squad Digital    Squad Operações
        ${equipe}=    Item Com    ${equipes.json()}    nome    ${nome}
        ${r}=    GET On Session    Validacao    /api/equipes/${equipe}[id]/capacidade    expected_status=200
        ${esperado}=    Capacidade Esperada Da Equipe    ${nome}
        FOR    ${campo}    IN    capacidade    demanda    horasMes    ocupacao    utilizacao    classificacao
            Comparar Valor    capacidade    ${nome}.${campo}    ${esperado}[${campo}]    ${r.json()}[${campo}]
        END
    END
    ${digital}=    Item Com    ${equipes.json()}    nome    Squad Digital
    ${esperado}=    Capacidade Esperada Da Equipe    Squad Digital
    Entrar Na Interface Como    Validacao    /equipes/${digital}[id]/capacidade
    ${ocupacao}=    Get Text    xpath=//div[contains(@class,'kpi')][span[normalize-space(.)='Ocupação']]/span[contains(@class,'kpi-value')]
    ${ocupacao}=    Numero Brasileiro    ${ocupacao}
    Comparar Valor    capacidade    Squad Digital.ocupacao (tela)    ${esperado}[ocupacao]    ${ocupacao}    origem=UI
    [Teardown]    Entrar Na Interface Como    TechLead    /portfolios


*** Keywords ***
Preparar Suite
    ${tl}=    Entrar Na API Como    TechLead
    ${po}=    Entrar Na API Como    ProductOwner
    ${gerente}=    Entrar Na API Como    GerenteProjeto
    ${nome}=    Nome Unico    Portfolio Tech Lead
    ${portfolio}=    Criar Portfolio Via API    ProductOwner    ${nome}    ${po}[id]
    Set Suite Variable    ${PORTFOLIO}    ${portfolio}
    Adicionar Membro Ao Portfolio    ProductOwner    ${portfolio}[id]    ${gerente}[id]
    Adicionar Membro Ao Portfolio    ProductOwner    ${portfolio}[id]    ${tl}[id]
    &{projetos}=    Create Dictionary
    FOR    ${nome}    ${fim}    IN    Integração    250    Plataforma    200    Relatórios    300
        ${r}=    Criar Projeto Via API    GerenteProjeto    ${portfolio}[id]    ${nome}    ${gerente}[id]    fim=${fim}
        Levar Ate Em Andamento    ${r.json()}[id]
        Set To Dictionary    ${projetos}    ${nome}=${r.json()}[id]
    END
    Set Suite Variable    &{PROJETOS}    &{projetos}
    ${equipe}=    Nome Unico    Squad RF
    Set Suite Variable    ${NOME_EQUIPE}    ${equipe}
    ${sufixo}=    Evaluate    __import__('uuid').uuid4().hex[:6]
    Set Suite Variable    ${EMAIL_BIANCA}    bianca.${sufixo}@prumo.test
    Set Suite Variable    ${EMAIL_CAIO}    caio.${sufixo}@prumo.test
    Abrir Navegador
    Entrar Na Interface Como    TechLead    /portfolios
    Ativar Portfolio    ${portfolio}[id]

Levar Ate Em Andamento
    [Arguments]    ${id}
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${id}
    IF    $projeto['status'] == 'Rascunho'    Alterar Status Do Projeto    GerenteProjeto    ${id}    Aprovar
    Alterar Status Do Projeto    GerenteProjeto    ${id}    Iniciar

Criar Dependencia
    [Arguments]    ${origem}    ${destino}    ${expected}=201
    ${corpo}=    Create Dictionary    projetoOrigemId=${PROJETOS}[${origem}]    projetoDestinoId=${PROJETOS}[${destino}]
    ${r}=    POST On Session    TechLead    /api/dependencias    json=${corpo}    expected_status=${expected}
    IF    $expected == 'any'    Should Be True    400 <= ${r.status_code} < 500    ${r.text}
    RETURN    ${r}

Dependencia Deve Estar Em Risco
    [Arguments]    ${origem}    ${destino}    ${em_risco}    ${motivo}=${None}
    ${lista}=    GET On Session    TechLead    /api/portfolios/${PORTFOLIO}[id]/dependencias    expected_status=200
    ${dep}=    Item Com Campos    ${lista.json()}    projetoOrigemNome=${origem}    projetoDestinoNome=${destino}
    Comparar Valor    dependencia    ${origem} → ${destino}.emRisco    ${em_risco}    ${dep}[emRisco]
    IF    $motivo    Should Be Equal    ${dep}[motivo]    ${motivo}

Precisa Da Equipe
    Skip If    not $EQUIPE_ID    Depende do cadastro da equipe pela interface.

Adicionar Membro Na Tela
    [Arguments]    ${nome}    ${email}    ${custo}    ${capacidade}
    Clicar    xpath=//article[.//h2[normalize-space(.)='${NOME_EQUIPE}']]//button[normalize-space(.)='Adicionar membro']
    Preencher    id:nomeMembro    ${nome}
    Preencher    id:emailMembro    ${email}
    Preencher    id:custo    ${custo}
    Preencher    id:capacidade    ${capacidade}
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//td[normalize-space(.)='${email}']
