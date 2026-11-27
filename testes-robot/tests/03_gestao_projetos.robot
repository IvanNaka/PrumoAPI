*** Settings ***
Documentation       Gestão de projetos — persona Gerente de Projetos (Tabela 20; roteiro da seção 4.4.2).
...                 Cadastro, edição e status dos projetos, orçamento, custos e relatórios
...                 (RF10 a RF13, RF22 a RF24, RF43 e RF44; UC7).
Resource            ../resources/ui.resource
Library             ../libraries/ValoresEsperados.py
Suite Setup         Preparar Suite
Suite Teardown      Fechar Navegador
Test Tags           suite:gestao-projetos    persona:GerenteProjeto


*** Variables ***
${PROJETO_ID}       ${EMPTY}


*** Test Cases ***
Gerente cadastra um projeto com responsável, datas, orçamento e categoria
    [Tags]    RF10    RF12    RF22    UC7    ui    critico
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/projetos
    Titulo Da Pagina Deve Ser    Projetos
    Clicar No Botao    Novo projeto
    Preencher    id:nome    ${NOME_PROJETO}
    Preencher    id:descricao    Projeto do roteiro 4.4.2 cadastrado pela interface.
    Selecionar    id:responsavel    ${GERENTE}[id]
    Preencher Data    id:inicio    ${INICIO}
    Preencher Data    id:fim    ${FIM}
    Preencher    id:orcamento    120000
    Marcar Opcao    Transform
    Selecionar    id:prioridade    Alta
    Clicar No Botao    Salvar
    Wait Until Page Contains    ${NOME_PROJETO}
    ${id}=    Id Do Projeto Pelo Nome    GerenteProjeto    ${PORTFOLIO}[id]    ${NOME_PROJETO}
    Set Suite Variable    ${PROJETO_ID}    ${id}
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${id}
    Should Be Equal    ${projeto}[portfolioId]    ${PORTFOLIO}[id]
    Should Be Equal    ${projeto}[categoriaEstrategica]    Transform
    Should Be Equal As Numbers    ${projeto}[orcamentoAprovado]    120000
    Should Be Equal    ${projeto}[dataInicio]    ${INICIO}
    Should Be Equal    ${projeto}[dataFim]    ${FIM}

Projeto com término antes do início é recusado
    [Tags]    RF10    UC7    api
    ${r}=    Criar Projeto Via API    GerenteProjeto    ${PORTFOLIO}[id]    Datas invertidas    ${GERENTE}[id]
    ...    inicio=10    fim=-10    status=400
    Validar Problem Details    ${r}    400
    Should Contain    ${r.text}    A data de término deve ser igual ou posterior à data de início.

Gerente edita o projeto
    [Tags]    RF11    RF22    UC7    ui
    Precisa Do Projeto
    Abrir Tela    /projetos/${PROJETO_ID}
    Clicar No Botao    Editar
    Preencher    id:orcamento    150000
    Preencher    id:descricao    Escopo ampliado após revisão.
    Clicar No Botao    Salvar
    Wait Until Page Contains    Escopo ampliado após revisão.
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${PROJETO_ID}
    Should Be Equal As Numbers    ${projeto}[orcamentoAprovado]    150000

Gerente altera o status pelos botões da tela
    [Documentation]    Fluxo da Figura 26: Rascunho → Planejado → Em andamento → Suspenso → Em andamento.
    [Tags]    RF13    UC7    ui    critico
    Precisa Do Projeto
    Abrir Tela    /projetos/${PROJETO_ID}
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${PROJETO_ID}
    IF    $projeto['status'] == 'Rascunho'    Executar Acao Na Tela    Aprovar    Planejado
    Executar Acao Na Tela    Iniciar    EmAndamento
    Executar Acao Na Tela    Suspender    Suspenso
    Executar Acao Na Tela    Retomar    EmAndamento

Transições inválidas de status são recusadas
    [Tags]    RF13    UC7    api
    ${r}=    Criar Projeto Via API    GerenteProjeto    ${PORTFOLIO}[id]    Projeto transições    ${GERENTE}[id]
    ${id}=    Set Variable    ${r.json()}[id]
    ${resposta}=    Alterar Status Do Projeto    GerenteProjeto    ${id}    Finalizar    status=409
    Validar Problem Details    ${resposta}    409

Gerente conclui e cancela projetos
    [Tags]    RF13    UC7    api
    ${r}=    Criar Projeto Via API    GerenteProjeto    ${PORTFOLIO}[id]    Projeto a concluir    ${GERENTE}[id]
    ${id}=    Set Variable    ${r.json()}[id]
    Levar Projeto Ate Em Andamento    ${id}
    ${concluido}=    Alterar Status Do Projeto    GerenteProjeto    ${id}    Finalizar
    Should Be Equal    ${concluido.json()}[status]    Concluido
    Should Not Be Empty    ${concluido.json()}[dataConclusao]
    ${r}=    Criar Projeto Via API    GerenteProjeto    ${PORTFOLIO}[id]    Projeto a cancelar    ${GERENTE}[id]
    ${cancelado}=    Alterar Status Do Projeto    GerenteProjeto    ${r.json()}[id]    Cancelar
    Should Be Equal    ${cancelado.json()}[status]    Cancelado
    ${edicao}=    Create Dictionary    nome=Não pode editar
    ${negado}=    PUT On Session    GerenteProjeto    /api/projetos/${r.json()}[id]    json=${edicao}    expected_status=any
    Should Be True    ${negado.status_code} in (400, 409)

Gerente registra custos e despesas do projeto
    [Tags]    RF23    UC7    ui    critico
    Precisa Do Projeto
    Abrir Tela    /projetos/${PROJETO_ID}
    Clicar No Botao    Orçamento
    Registrar Lancamento Na Tela    Licenças de software    10000    Custo    ${ONTEM}
    Registrar Lancamento Na Tela    Viagem para implantação    5000    Despesa    ${ONTEM}
    ${lancamentos}=    GET On Session    GerenteProjeto    /api/projetos/${PROJETO_ID}/lancamentos    expected_status=200
    ${total}=    Evaluate    sum(l['valor'] for l in $lancamentos.json())
    Should Be Equal As Numbers    ${total}    15000

Lançamento inválido é recusado
    [Tags]    RF23    api
    Precisa Do Projeto
    ${futuro}=    Data Relativa    5
    ${corpo}=    Create Dictionary    descricao=Futuro    valor=${100}    tipo=Custo    dataLancamento=${futuro}
    ${r}=    POST On Session    GerenteProjeto    /api/projetos/${PROJETO_ID}/lancamentos    json=${corpo}
    ...    expected_status=400
    Validar Problem Details    ${r}    400    Lançamento inválido: valor deve ser positivo e data não pode ser futura.
    ${corpo}=    Create Dictionary    descricao=Negativo    valor=${-1}    tipo=Despesa    dataLancamento=${ONTEM}
    ${r}=    POST On Session    GerenteProjeto    /api/projetos/${PROJETO_ID}/lancamentos    json=${corpo}
    ...    expected_status=400

Burn Rate do projeto confere com a fórmula F5
    [Documentation]    Custo realizado = lançamentos (sem worklogs) = 15.000; meses decorridos = máx(1, dias/30).
    [Tags]    RF24    api    calculo
    Precisa Do Projeto
    ${esperado}=    Burn Rate Esperado    150000    ${INICIO}    ${FIM}    15000
    ${ind}=    GET On Session    GerenteProjeto    /api/projetos/${PROJETO_ID}/indicadores    expected_status=200
    ${burn}=    Set Variable    ${ind.json()}[burnRate]
    FOR    ${campo}    IN    custoRealizado    burnRateMensal    projecaoCustoTotal    percentualConsumido    saldo
        Comparar Valor    burnRate projeto    ${campo}    ${esperado}[${campo}]    ${burn}[${campo}]
    END
    Comparar Valor    burnRate projeto    estouro    ${esperado}[estouro]    ${burn}[estouro]

Aba Orçamento mostra o percentual consumido
    [Tags]    RF24    ui    calculo
    Precisa Do Projeto
    Abrir Tela    /projetos/${PROJETO_ID}
    Clicar No Botao    Orçamento
    ${esperado}=    Burn Rate Esperado    150000    ${INICIO}    ${FIM}    15000
    ${texto}=    Formatar Numero    ${esperado}[percentualConsumido]
    Wait Until Page Contains    ${texto}%

Gerente gera o relatório do portfólio em PDF e Excel
    [Tags]    RF43    RF44    api    critico
    ${pdf}=    GET On Session    GerenteProjeto    /api/portfolios/${PORTFOLIO}[id]/relatorios/portfolio
    ...    params=formato=pdf    expected_status=200
    Should Be Equal    ${pdf.headers}[Content-Type]    application/pdf
    Should Be True    $pdf.content.startswith(b'%PDF')
    ${excel}=    GET On Session    GerenteProjeto    /api/portfolios/${PORTFOLIO}[id]/relatorios/portfolio
    ...    params=formato=excel    expected_status=200
    Should Contain    ${excel.headers}[Content-Type]    spreadsheetml
    Should Be True    $excel.content.startswith(b'PK')
    ${conteudo}=    Texto Da Planilha    ${excel.content}
    Should Contain    ${conteudo}    ${PORTFOLIO}[nome]
    Should Contain    ${conteudo}    ${NOME_PROJETO}

Relatório gerado pela tela entra no histórico
    [Tags]    RF43    RF44    ui
    Abrir Tela    /portfolios/${PORTFOLIO}[id]/relatorios
    Titulo Da Pagina Deve Ser    Relatórios
    ${antes}=    Quantidade No Historico
    Clicar    xpath=//article[.//h2[normalize-space(.)='Relatório do portfólio']]//button[contains(normalize-space(.),'Excel')]
    Wait Until Keyword Succeeds    15s    1s    Historico Deve Ter Mais Que    ${antes}


*** Keywords ***
Preparar Suite
    ${gerente}=    Entrar Na API Como    GerenteProjeto
    Set Suite Variable    ${GERENTE}    ${gerente}
    ${nome}=    Nome Unico    Portfolio Gerente
    ${portfolio}=    Criar Portfolio Via API    GerenteProjeto    ${nome}    ${gerente}[id]
    Set Suite Variable    ${PORTFOLIO}    ${portfolio}
    ${projeto}=    Nome Unico    Projeto Gerente
    Set Suite Variable    ${NOME_PROJETO}    ${projeto}
    ${inicio}=    Data Relativa    -60
    ${fim}=    Data Relativa    120
    ${ontem}=    Data Relativa    -1
    Set Suite Variable    ${INICIO}    ${inicio}
    Set Suite Variable    ${FIM}    ${fim}
    Set Suite Variable    ${ONTEM}    ${ontem}
    Abrir Navegador
    Entrar Na Interface Como    GerenteProjeto    /portfolios
    Ativar Portfolio    ${PORTFOLIO}[id]

Precisa Do Projeto
    Skip If    not $PROJETO_ID    Depende do cadastro do projeto pela interface.

Preencher Data
    [Documentation]    Campo type=date: o valor vai no formato AAAA-MM-DD e o Angular recebe o evento input.
    [Arguments]    ${locator}    ${data}
    Wait Until Element Is Visible    ${locator}
    ${elemento}=    Get WebElement    ${locator}
    Execute Javascript
    ...    arguments[0].value = arguments[1];
    ...    arguments[0].dispatchEvent(new Event('input', {bubbles: true}));
    ...    arguments[0].dispatchEvent(new Event('change', {bubbles: true}));
    ...    ARGUMENTS    ${elemento}    ${data}

Executar Acao Na Tela
    [Arguments]    ${rotulo}    ${status_esperado}
    Clicar No Botao    ${rotulo}
    Wait Until Keyword Succeeds    10s    500ms    Status Do Projeto Deve Ser    ${status_esperado}

Status Do Projeto Deve Ser
    [Arguments]    ${status}
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${PROJETO_ID}
    Should Be Equal    ${projeto}[status]    ${status}

Levar Projeto Ate Em Andamento
    [Arguments]    ${id}
    ${projeto}=    Buscar Projeto    GerenteProjeto    ${id}
    IF    $projeto['status'] == 'Rascunho'    Alterar Status Do Projeto    GerenteProjeto    ${id}    Aprovar
    Alterar Status Do Projeto    GerenteProjeto    ${id}    Iniciar

Registrar Lancamento Na Tela
    [Arguments]    ${descricao}    ${valor}    ${tipo}    ${data}
    Clicar No Botao    Novo lançamento
    Preencher    id:desc    ${descricao}
    Preencher    id:valor    ${valor}
    Marcar Opcao    ${tipo}
    Preencher Data    id:data    ${data}
    Clicar No Botao    Salvar
    Wait Until Element Is Visible    xpath=//td[normalize-space(.)='${descricao}']

Formatar Numero
    [Documentation]    12.5 -> "12,5"; 10 -> "10" (formato num() do front).
    [Arguments]    ${valor}
    ${texto}=    Evaluate    f"{$valor:,.2f}".rstrip('0').rstrip('.').replace(',', 'X').replace('.', ',').replace('X', '.')
    RETURN    ${texto}

Texto Da Planilha
    [Arguments]    ${conteudo}
    ${texto}=    Evaluate
    ...    ' '.join(str(c) for ws in openpyxl.load_workbook(io.BytesIO($conteudo)).worksheets for r in ws.iter_rows(values_only=True) for c in r if c is not None)
    ...    modules=openpyxl,io
    RETURN    ${texto}

Quantidade No Historico
    ${n}=    Get Element Count    xpath=//div[contains(@class,'card')][h2[normalize-space(.)='Histórico']]//tbody/tr[td[4]]
    RETURN    ${n}

Historico Deve Ter Mais Que
    [Arguments]    ${antes}
    ${agora}=    Quantidade No Historico
    Should Be True    ${agora} > ${antes}
