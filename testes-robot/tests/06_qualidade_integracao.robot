*** Settings ***
Documentation       Qualidade e integração — personas QA e Desenvolvedor (Tabela 20; seções 4.4.3 e 4.6.2).
...                 Métricas de qualidade (F8) e configuração/sincronização com o Jira
...                 (RF33, RF34, RF47 e RF51; UC14, UC15 e UC16).
...
...                 A sincronização real (seção 4.9 d) usa o projeto de testes do Jira Cloud informado em
...                 PRUMO_JIRA_URL, PRUMO_JIRA_EMAIL, PRUMO_JIRA_TOKEN e PRUMO_JIRA_PROJETO; sem elas,
...                 esses casos são pulados. ATENÇÃO: a suíte reconfigura a integração do ambiente de testes.
Resource            ../resources/ui.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Suite Teardown      Encerrar Suite
Test Tags           suite:qualidade-integracao


*** Variables ***
${URL_INVALIDA}     https://prumo-jira-inexistente.invalid
${RN24}             Falha de autenticação no Jira. Verifique URL, e-mail e token.
${PROJETO_JIRA}     ${EMPTY}


*** Test Cases ***
# ---------- QA: métricas de qualidade ----------
Indicador de qualidade do portfólio segue a fórmula F8
    [Documentation]    razão = bugs / entregas; índice = entregas / (entregas + bugs) × 100; meta ≤ 0,20.
    [Tags]    RF33    RF34    UC14    api    calculo    persona:QA    critico
    ${r}=    GET On Session    QA    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/indicadores/qualidade    expected_status=200
    Comparar Indicador    qualidade    ${r.json()}
    ${esperado}=    Valores Esperados    qualidade
    Comparar Valor    qualidade    atingiuMeta    ${esperado}[atingiuMeta]    ${r.json()}[atingiuMeta]

Qualidade por tipo de issue considera Story, Task, Feature e Bug
    [Tags]    RF33    RF34    UC14    api    persona:QA
    ${r}=    GET On Session    QA    /api/portfolios/${PORTFOLIO_VALIDACAO_1}/indicadores/qualidade    expected_status=200
    FOR    ${tipo}    IN    Story    Task    Feature    Bug
        Dictionary Should Contain Key    ${r.json()}[porTipo]    ${tipo}
    END
    ${soma}=    Evaluate    sum($r.json()['porTipo'].values())
    Should Be Equal As Integers    ${soma}    ${{ $r.json()['bugs'] + $r.json()['entregas'] }}

Qualidade de cada projeto confere com a planilha
    [Tags]    RF33    RF34    UC14    api    calculo    persona:QA
    FOR    ${nome}    IN    Portal do Cliente    App Mobile    Manutenção do ERP
        ${id}=    Id Do Projeto Pelo Nome    QA    ${PORTFOLIO_VALIDACAO_1}    ${nome}
        ${r}=    GET On Session    QA    /api/projetos/${id}/indicadores    expected_status=200
        ${esperado}=    Qualidade Esperada Do Projeto    ${nome}
        FOR    ${campo}    IN    bugs    entregas    razaoBugsPorEntrega    indiceQualidade
            Comparar Valor    qualidade projeto    ${nome}.${campo}    ${esperado}[${campo}]    ${r.json()}[qualidade][${campo}]
        END
    END

QA acompanha a qualidade no dashboard
    [Tags]    RF33    RF38    UC14    ui    calculo    persona:QA
    Entrar Na Interface Como    QA    /portfolios
    Ativar Portfolio    ${PORTFOLIO_VALIDACAO_1}
    Abrir Tela    /portfolios/${PORTFOLIO_VALIDACAO_1}/dashboard
    ${esperado}=    Valores Esperados    qualidade
    ${indice}=    Valor Do Card    Qualidade da entrega
    Comparar Valor    qualidade    indiceQualidade (tela)    ${esperado}[indiceQualidade]    ${indice}    origem=UI
    ${texto}=    Texto Do Card    Qualidade da entrega
    Should Contain    ${texto}    ${esperado}[bugs] bug(s) para ${esperado}[entregas] entrega(s)

# ---------- Desenvolvedor: configuração do Jira (UC15) ----------
Desenvolvedor configura a integração com o Jira
    [Tags]    RF47    RF51    UC15    ui    persona:Desenvolvedor    critico
    Entrar Na Interface Como    Desenvolvedor    /integracoes/jira
    Titulo Da Pagina Deve Ser    Integração com o Jira
    Preencher    id:url    ${URL_JIRA}
    Preencher    id:email    ${EMAIL_JIRA}
    Preencher    id:token    ${TOKEN_ERRADO}
    Preencher    id:intervalo    30
    Clicar No Botao    Salvar
    Wait Until Keyword Succeeds    10s    500ms    Integracao Deve Ter    intervaloSincronizacaoMinutos    ${30}
    # Salvar já testa a conexão (Figura 29); espera esse teste terminar antes de seguir.
    Aguardar Fim Do Teste De Conexao
    ${i}=    GET On Session    Desenvolvedor    /api/integracoes/jira    expected_status=200
    Should Be True    ${i.json()}[configurada]
    Should Be True    ${i.json()}[tokenConfigurado]
    Should Not Contain    ${i.text}    ${TOKEN_ERRADO}

Intervalo de sincronização fora de 15 a 1440 minutos é recusado
    [Tags]    RF51    UC15    api    persona:Desenvolvedor
    FOR    ${minutos}    IN    5    2000
        ${corpo}=    Create Dictionary    url=${URL_JIRA}    email=${EMAIL_JIRA}    intervaloSincronizacaoMinutos=${minutos}
        ${r}=    PUT On Session    Desenvolvedor    /api/integracoes/jira    json=${corpo}    expected_status=400
        Validar Problem Details    ${r}    400    O intervalo de sincronização deve ficar entre 15 e 1440 minutos.
    END

Falha de autenticação no Jira é tratada (UC16 / Figura 29)
    [Documentation]    Credenciais inválidas: a API responde RN24 e a integração vai para "Erro de conexão".
    [Tags]    RF47    UC15    UC16    ui    api    persona:Desenvolvedor    critico
    Aguardar Fim Do Teste De Conexao
    Abrir Tela    /integracoes/jira
    Clicar No Botao    Testar conexão
    # A resolução de um host inexistente pode demorar, conforme a rede do ambiente.
    Wait Until Page Contains    ${RN24}    timeout=90s
    Wait Until Keyword Succeeds    10s    500ms    Integracao Deve Ter    status    ErroConexao
    Wait Until Page Contains    Erro de conexão

# ---------- Sincronização real com o projeto de testes do Jira (seção 4.9 d) ----------
Sincronização importa as issues do projeto de testes
    [Tags]    RF47    UC16    api    jira    persona:Desenvolvedor    critico
    Precisa Do Jira
    Configurar Jira    ${JIRA_TOKEN}
    ${i}=    POST On Session    Desenvolvedor    /api/integracoes/jira/testar    expected_status=200
    Should Be Equal    ${i.json()}[status]    Conectada
    ${log}=    Sincronizar E Aguardar
    Should Be True    ${log}[sucesso]    ${log}[mensagemErro]
    Should Be True    ${log}[issuesProcessadas] > 0    Nenhuma issue importada do projeto ${JIRA_PROJETO}.
    Set Suite Variable    ${LOG_SINCRONIZACAO}    ${log}
    ${ind}=    Indicadores Do Projeto Jira
    Should Be True    ${ind}[saude][totalIssues] > 0
    Set Suite Variable    ${TOTAL_ISSUES}    ${ind}[saude][totalIssues]

Projeto de testes tem os quatro tipos de issue e worklogs (seção 4.3.3)
    [Documentation]    A massa do Jira deve ter Story, Task, Feature e Bug e worklogs. Os tipos são contados entre
    ...    as issues concluídas (indicador de qualidade, F8); os worklogs, pelo log da sincronização.
    [Tags]    RF47    UC16    api    jira    persona:Desenvolvedor
    Precisa Do Jira
    Skip If    not $TOTAL_ISSUES    Depende da primeira sincronização.
    ${ind}=    Indicadores Do Projeto Jira
    ${por_tipo}=    Set Variable    ${ind}[qualidade][porTipo]
    ${faltando}=    Evaluate    [t for t in ('Story', 'Task', 'Feature', 'Bug') if not $por_tipo.get(t)]
    Should Be Empty    ${faltando}    Tipos sem issues concluídas no projeto ${JIRA_PROJETO}: ${faltando} (contagem: ${por_tipo})
    Should Be True    ${LOG_SINCRONIZACAO}[worklogsProcessados] > 0    Nenhum worklog importado do projeto ${JIRA_PROJETO}.

Segunda sincronização não duplica registros
    [Tags]    RF47    UC16    api    jira    persona:Desenvolvedor    critico
    Precisa Do Jira
    Skip If    not $TOTAL_ISSUES    Depende da primeira sincronização.
    ${log}=    Sincronizar E Aguardar
    Should Be True    ${log}[sucesso]    ${log}[mensagemErro]
    ${ind}=    Indicadores Do Projeto Jira
    Should Be Equal As Integers    ${ind}[saude][totalIssues]    ${TOTAL_ISSUES}
    Should Be Equal As Integers    ${log}[issuesProcessadas]    ${LOG_SINCRONIZACAO}[issuesProcessadas]

Intervalo configurado é respeitado pela sincronização automática
    [Documentation]    Logo após uma sincronização, o agendador (que roda a cada minuto) não pode disparar outra
    ...    antes do intervalo de 15 minutos.
    [Tags]    RF51    UC16    api    jira    lento    persona:Desenvolvedor
    Precisa Do Jira
    Configurar Jira    ${JIRA_TOKEN}    intervalo=15    ativo=${True}
    Sincronizar E Aguardar
    ${antes}=    GET On Session    Desenvolvedor    /api/integracoes/jira/logs    expected_status=200
    Sleep    75s    Espera pelo menos um ciclo do agendador.
    ${depois}=    GET On Session    Desenvolvedor    /api/integracoes/jira/logs    expected_status=200
    Length Should Be    ${depois.json()}    ${{ len($antes.json()) }}

Credenciais inválidas no Jira real geram erro de autenticação
    [Tags]    RF47    UC16    api    jira    persona:Desenvolvedor
    Precisa Do Jira
    # Salvar já testa a conexão: com token inválido, a própria gravação responde RN24.
    ${r}=    Configurar Jira    token-invalido-${TOKEN_ERRADO}    status=400
    Validar Problem Details    ${r}    400    ${RN24}
    ${r}=    POST On Session    Desenvolvedor    /api/integracoes/jira/testar    expected_status=400
    Validar Problem Details    ${r}    400    ${RN24}
    Integracao Deve Ter    status    ErroConexao
    [Teardown]    Run Keyword If    $JIRA_URL    Configurar Jira    ${JIRA_TOKEN}


*** Keywords ***
Preparar Suite
    Entrar Na API Como    QA
    Entrar Na API Como    Desenvolvedor
    ${p}=    GET On Session    QA    /api/portfolios/${PORTFOLIO_VALIDACAO_1}    expected_status=200
    Preparar Oraculo    ${p.json()}[dataCriacao]
    Set Suite Variable    ${TOTAL_ISSUES}    ${0}
    Set Suite Variable    ${LOG_SINCRONIZACAO}    ${None}
    Set Suite Variable    ${URL_JIRA}    ${{ $JIRA_URL or $URL_INVALIDA }}
    Set Suite Variable    ${EMAIL_JIRA}    ${{ $JIRA_EMAIL or 'robot@prumo.test' }}
    Set Suite Variable    ${TOKEN_ERRADO}    token-errado-do-robot
    IF    $JIRA_URL and $JIRA_TOKEN and $JIRA_PROJETO    Preparar Projeto Do Jira
    Abrir Navegador

Preparar Projeto Do Jira
    [Documentation]    Projeto do Prumo ligado ao projeto de testes do Jira (JiraProjectKey).
    ${gerente}=    Entrar Na API Como    GerenteProjeto
    Set Suite Variable    ${GERENTE_ID}    ${gerente}[id]
    Liberar Chave Jira De Execucoes Anteriores
    ${nome}=    Nome Unico    Portfolio Jira
    ${portfolio}=    Criar Portfolio Via API    GerenteProjeto    ${nome}    ${gerente}[id]
    ${dev}=    Entrar Na API Como    Desenvolvedor
    Adicionar Membro Ao Portfolio    GerenteProjeto    ${portfolio}[id]    ${dev}[id]
    ${r}=    Criar Projeto Via API    GerenteProjeto    ${portfolio}[id]    Projeto Jira    ${gerente}[id]
    ${id}=    Set Variable    ${r.json()}[id]
    Definir Chave Jira    ${id}    ${JIRA_PROJETO}
    # Em andamento, para o projeto entrar nos indicadores de saúde (F12).
    Alterar Status Do Projeto    GerenteProjeto    ${id}    Aprovar
    Alterar Status Do Projeto    GerenteProjeto    ${id}    Iniciar
    Set Suite Variable    ${PROJETO_JIRA}    ${id}

Definir Chave Jira
    [Arguments]    ${id}    ${chave}
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${id}
    ${corpo}=    Create Dictionary    nome=${projeto}[nome]    responsavelId=${projeto}[responsavelId]
    ...    dataInicio=${projeto}[dataInicio]    dataFim=${projeto}[dataFim]    orcamentoAprovado=${projeto}[orcamentoAprovado]
    ...    categoriaEstrategica=${projeto}[categoriaEstrategica]    prioridade=${projeto}[prioridade]    jiraProjectKey=${chave}
    PUT On Session    GerenteProjeto    /api/projetos/${id}    json=${corpo}    expected_status=200

Liberar Chave Jira De Execucoes Anteriores
    [Documentation]    Cada issue do Jira pertence a um só projeto do Prumo: tira a chave dos projetos criados
    ...    por execuções anteriores desta suíte.
    ${portfolios}=    GET On Session    GerenteProjeto    /api/portfolios    expected_status=200
    FOR    ${p}    IN    @{portfolios.json()}
        IF    not $p['nome'].startswith('Portfolio Jira')    CONTINUE
        ${projetos}=    GET On Session    GerenteProjeto    /api/portfolios/${p}[id]/projetos    expected_status=200
        FOR    ${projeto}    IN    @{projetos.json()}
            ${detalhe}=    Buscar Projeto    GerenteProjeto    ${projeto}[id]
            IF    $detalhe.get('jiraProjectKey') == $JIRA_PROJETO and $detalhe['status'] not in ('Cancelado', 'Arquivado')
                Definir Chave Jira    ${projeto}[id]    ${EMPTY}
            END
        END
    END

Encerrar Suite
    [Documentation]    Sem Jira real, deixa a integração falsa desativada para o agendador não tentar sincronizar.
    ...    Com Jira real, solta a chave do projeto de testes para a próxima execução.
    IF    $PROJETO_JIRA    Run Keyword And Ignore Error    Definir Chave Jira    ${PROJETO_JIRA}    ${EMPTY}
    IF    not $JIRA_URL
        ${corpo}=    Create Dictionary    url=${URL_JIRA}    email=${EMAIL_JIRA}    ativo=${False}
        PUT On Session    Desenvolvedor    /api/integracoes/jira    json=${corpo}    expected_status=any
    END
    Fechar Navegador

Precisa Do Jira
    Skip If    not ($JIRA_URL and $JIRA_TOKEN and $JIRA_PROJETO)
    ...    Defina PRUMO_JIRA_URL, PRUMO_JIRA_EMAIL, PRUMO_JIRA_TOKEN e PRUMO_JIRA_PROJETO (projeto de testes do Jira).

Integracao Deve Ter
    [Arguments]    ${campo}    ${valor}
    ${i}=    GET On Session    Desenvolvedor    /api/integracoes/jira    expected_status=200
    Should Be Equal    ${i.json()}[${campo}]    ${valor}

Aguardar Fim Do Teste De Conexao
    Wait Until Keyword Succeeds    90s    1s    Status Da Integracao Nao Deve Ser    TestandoConexao

Status Da Integracao Nao Deve Ser
    [Arguments]    ${status}
    ${i}=    GET On Session    Desenvolvedor    /api/integracoes/jira    expected_status=200
    Should Not Be Equal    ${i.json()}[status]    ${status}

Configurar Jira
    [Arguments]    ${token}    ${intervalo}=60    ${ativo}=${False}    ${status}=200
    # O corpo leva o API token: nada deste trecho vai para o log.
    ${nivel}=    Set Log Level    NONE
    ${corpo}=    Create Dictionary    url=${JIRA_URL}    email=${JIRA_EMAIL}    apiToken=${token}
    ...    intervaloSincronizacaoMinutos=${{ int($intervalo) }}    ativo=${ativo}
    ${r}=    PUT On Session    Desenvolvedor    /api/integracoes/jira    json=${corpo}    expected_status=${status}
    [Teardown]    Set Log Level    ${nivel}
    RETURN    ${r}

Indicadores Do Projeto Jira
    ${r}=    GET On Session    Desenvolvedor    /api/projetos/${PROJETO_JIRA}/indicadores    expected_status=200
    ...    params=de=2000-01-01
    RETURN    ${r.json()}

Sincronizar E Aguardar
    [Documentation]    POST /sincronizar (202) e espera um log novo terminar.
    ${antes}=    GET On Session    Desenvolvedor    /api/integracoes/jira/logs    expected_status=200
    ${n}=    Get Length    ${antes.json()}
    POST On Session    Desenvolvedor    /api/integracoes/jira/sincronizar    expected_status=202
    ${log}=    Wait Until Keyword Succeeds    ${JIRA_TIMEOUT_SINCRONIZACAO}    3s    Log Novo Concluido    ${n}
    RETURN    ${log}

Log Novo Concluido
    [Arguments]    ${quantidade_anterior}
    ${logs}=    GET On Session    Desenvolvedor    /api/integracoes/jira/logs    expected_status=200
    Should Be True    len($logs.json()) > ${quantidade_anterior}
    ${ultimo}=    Set Variable    ${logs.json()}[0]
    Should Not Be Equal    ${ultimo}[fim]    ${None}
    RETURN    ${ultimo}
