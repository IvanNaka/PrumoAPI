*** Settings ***
Documentation       Priorização — persona Product Owner (Tabela 20; roteiro da seção 4.4.2).
...                 Avaliação por critério, cálculo do score (F1), ranking (F2) e recálculo automático (F3)
...                 (RF18 a RF21; UC10). Os valores esperados vêm do oráculo independente.
Resource            ../resources/ui.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Suite Teardown      Fechar Navegador
Test Tags           suite:priorizacao    persona:ProductOwner


*** Variables ***
# Critérios do roteiro: (nome, peso, tipo)
@{CRITERIOS}        Valor de negócio    Esforço    Risco    Alinhamento estratégico
@{PESOS}            4    2    2    2
@{TIPOS}            Beneficio    Custo    Custo    Beneficio
# Projetos: nome, prioridade e notas (na ordem dos critérios)
@{ALFA}             5    2    1    5
@{BETA}             4    3    3    4
@{GAMA}             3    1    1    3
@{DELTA}            4    3    3    4


*** Test Cases ***
Priorizar sem critérios é recusado
    [Tags]    RF19    UC10    api
    ${nome}=    Nome Unico    Portfolio sem criterios
    ${vazio}=    Criar Portfolio Via API    ProductOwner    ${nome}    ${PO}[id]
    ${r}=    POST On Session    ProductOwner    /api/portfolios/${vazio}[id]/priorizacao    expected_status=409
    Validar Problem Details    ${r}    409    Cadastre ao menos um critério antes de priorizar.

Priorizar sem projetos é recusado
    [Tags]    RF19    UC10    api
    ${nome}=    Nome Unico    Portfolio sem projetos
    ${vazio}=    Criar Portfolio Via API    ProductOwner    ${nome}    ${PO}[id]
    Criar Criterio Via API    ProductOwner    ${vazio}[id]    Valor    10
    ${r}=    POST On Session    ProductOwner    /api/portfolios/${vazio}[id]/priorizacao    expected_status=409
    Validar Problem Details    ${r}    409    Não há projetos cadastrados para priorização.

Nota fora da escala de 1 a 5 é recusada
    [Tags]    RF18    UC10    api
    ${notas}=    Create Dictionary    ${CRITERIO_IDS}[0]=6
    ${r}=    Avaliar Projeto    ProductOwner    ${PROJETOS}[Alfa]    ${notas}    status=400
    Validar Problem Details    ${r}    400    A nota deve estar entre 1 e 5.

PO avalia um projeto pela matriz de avaliação
    [Tags]    RF18    UC10    ui    critico
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/priorizacao
    Titulo Da Pagina Deve Ser    Priorização
    FOR    ${indice}    ${nota}    IN ENUMERATE    @{ALFA}
        ${coluna}=    Evaluate    ${indice} + 2
        Selecionar    xpath=//tr[td//a[normalize-space(.)='Alfa']]/td[${coluna}]//select    ${nota}
    END
    Clicar    xpath=//tr[td//a[normalize-space(.)='Alfa']]//button[normalize-space(.)='Salvar avaliações']
    Wait Until Keyword Succeeds    10s    500ms    Avaliacao Do Projeto Deve Ter    Alfa    4
    ${avaliacoes}=    Notas Gravadas    Alfa
    Should Be Equal    ${avaliacoes}    ${{ [int(n) for n in $ALFA] }}

PO avalia os demais projetos
    [Tags]    RF18    UC10    api
    Avaliar Com Notas    Beta    ${BETA}
    Avaliar Com Notas    Gama    ${GAMA}
    Avaliar Com Notas    Delta    ${DELTA}

PO executa a priorização e o score segue a fórmula F1
    [Tags]    RF19    UC10    ui    calculo    critico
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/priorizacao
    Clicar No Botao    Executar priorização
    Wait Until Page Does Not Contain    Calculando...
    Ranking Deve Conferir Com O Oraculo    @{PESOS}

Ranking ordena por score, prioridade e data de cadastro (F2)
    [Documentation]    Beta e Delta empatam em score; Beta (Média) fica à frente de Delta (Baixa).
    [Tags]    RF20    UC10    ui    calculo    critico
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/priorizacao
    Clicar No Botao    Ranking
    ${esperado}=    Ranking Esperado Do Cenario    @{PESOS}
    FOR    ${item}    IN    @{esperado}
        ${linha}=    Set Variable    xpath=//tr[td//a[normalize-space(.)='${item}[nome]']]
        Wait Until Element Is Visible    ${linha}
        Element Text Should Be    ${linha}/td[1]    ${item}[posicao]º
        ${score}=    Get Text    ${linha}/td[3]
        ${score}=    Numero Brasileiro    ${score}
        Comparar Valor    ranking tela    ${item}[nome]    ${item}[score]    ${score}    origem=UI
    END

Alterar os pesos recalcula o ranking automaticamente (F3)
    [Documentation]    Sem nova execução da priorização: Gama passa à frente de Alfa (empate em 90, prioridade Crítica).
    [Tags]    RF21    UC10    api    calculo    critico
    @{novos}=    Create List    1    4    4    1
    Ajustar Pesos    @{novos}
    Ranking Deve Conferir Com O Oraculo    @{novos}
    ${ranking}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO}[id]/ranking    expected_status=200
    Should Be Equal    ${ranking.json()}[ranking][0][nome]    Gama

Alterar uma nota recalcula o ranking automaticamente (F3)
    [Tags]    RF21    UC10    api    calculo
    @{novas}=    Create List    5    3    3    4
    Set Suite Variable    @{DELTA}    @{novas}
    Avaliar Com Notas    Delta    ${DELTA}
    Ranking Deve Conferir Com O Oraculo    1    4    4    1

Ranking da massa de validação confere com a planilha
    [Tags]    RF19    RF20    api    calculo
    ${p}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]
    FOR    ${id}    IN    ${PORTFOLIO_VALIDACAO_1}    ${PORTFOLIO_VALIDACAO_2}
        ${ranking}=    GET On Session    ProductOwner    /api/portfolios/${id}/ranking    expected_status=200
        Comparar Ranking    ${ranking.json()}    ${id}
    END


*** Keywords ***
Preparar Suite
    ${po}=    Entrar Na API Como    ProductOwner
    Set Suite Variable    ${PO}    ${po}
    ${gerente}=    Entrar Na API Como    GerenteProjeto
    ${nome}=    Nome Unico    Portfolio Priorizacao
    ${portfolio}=    Criar Portfolio Via API    ProductOwner    ${nome}    ${po}[id]
    Set Suite Variable    ${PORTFOLIO}    ${portfolio}
    Adicionar Membro Ao Portfolio    ProductOwner    ${portfolio}[id]    ${gerente}[id]
    @{ids}=    Create List
    FOR    ${nome}    ${peso}    ${tipo}    IN ZIP    ${CRITERIOS}    ${PESOS}    ${TIPOS}
        ${r}=    Criar Criterio Via API    ProductOwner    ${portfolio}[id]    ${nome}    ${peso}    ${tipo}
        Append To List    ${ids}    ${r.json()}[id]
    END
    Set Suite Variable    @{CRITERIO_IDS}    @{ids}
    &{projetos}=    Create Dictionary
    FOR    ${nome}    ${prioridade}    IN    Alfa    Alta    Beta    Media    Gama    Critica    Delta    Baixa
        ${r}=    Criar Projeto Via API    GerenteProjeto    ${portfolio}[id]    ${nome}    ${gerente}[id]
        ...    prioridade=${prioridade}
        Set To Dictionary    ${projetos}    ${nome}=${r.json()}[id]
    END
    Set Suite Variable    &{PROJETOS}    &{projetos}
    Abrir Navegador
    Entrar Na Interface Como    ProductOwner    /portfolios
    Ativar Portfolio    ${portfolio}[id]

Avaliar Com Notas
    [Arguments]    ${projeto}    ${notas}
    ${mapa}=    Create Dictionary
    FOR    ${criterio}    ${nota}    IN ZIP    ${CRITERIO_IDS}    ${notas}
        Set To Dictionary    ${mapa}    ${criterio}=${nota}
    END
    Avaliar Projeto    ProductOwner    ${PROJETOS}[${projeto}]    ${mapa}

Notas Gravadas
    [Arguments]    ${projeto}
    ${projeto}=    Buscar Projeto    ProductOwner    ${PROJETOS}[${projeto}]
    ${por_id}=    Evaluate    {n['criterioId']: n['nota'] for n in $projeto['avaliacoes']}
    ${ordenadas}=    Evaluate    [$por_id.get(c) for c in $CRITERIO_IDS]
    RETURN    ${ordenadas}

Avaliacao Do Projeto Deve Ter
    [Arguments]    ${projeto}    ${quantidade}
    ${notas}=    Notas Gravadas    ${projeto}
    ${preenchidas}=    Evaluate    len([n for n in $notas if n])
    Should Be Equal As Integers    ${preenchidas}    ${quantidade}

Ajustar Pesos
    [Arguments]    @{pesos}
    ${lista}=    Create List
    FOR    ${criterio}    ${peso}    IN ZIP    ${CRITERIO_IDS}    ${pesos}
        ${item}=    Create Dictionary    criterioId=${criterio}    peso=${peso}
        Append To List    ${lista}    ${item}
    END
    PUT On Session    ProductOwner    /api/portfolios/${PORTFOLIO}[id]/criterios/pesos    json=${lista}    expected_status=200

Ranking Esperado Do Cenario
    [Arguments]    @{pesos}
    ${criterios}=    Evaluate    list(zip($pesos, $TIPOS))
    @{linhas}=    Create List
    FOR    ${ordem}    ${nome}    ${prioridade}    ${notas}    IN ZIP    ${{ range(4) }}
    ...    ${{ ['Alfa','Beta','Gama','Delta'] }}    ${{ ['Alta','Media','Critica','Baixa'] }}
    ...    ${{ [$ALFA, $BETA, $GAMA, $DELTA] }}
        ${score}=    Score Esperado Para    ${criterios}    ${notas}
        ${linha}=    Create List    ${nome}    ${score}    ${prioridade}    ${ordem}
        Append To List    ${linhas}    ${linha}
    END
    ${ranking}=    Ranking Esperado Para    ${linhas}
    RETURN    ${ranking}

Ranking Deve Conferir Com O Oraculo
    [Arguments]    @{pesos}
    ${esperado}=    Ranking Esperado Do Cenario    @{pesos}
    ${resposta}=    GET On Session    ProductOwner    /api/portfolios/${PORTFOLIO}[id]/ranking    expected_status=200
    ${obtido}=    Set Variable    ${resposta.json()}[ranking]
    Comparar Valor    ranking    quantidade    ${{ len($esperado) }}    ${{ len($obtido) }}
    FOR    ${e}    ${o}    IN ZIP    ${esperado}    ${obtido}
        Comparar Valor    ranking    ${e}[posicao]º nome    ${e}[nome]    ${o}[nome]
        Comparar Valor    ranking    ${e}[nome] score    ${e}[score]    ${o}[score]
    END
