*** Settings ***
Documentation       Navegador (SeleniumLibrary / Selenium WebDriver) e sessão no front Prumo.
...                 Em caso de falha, a SeleniumLibrary salva a captura de tela no log (evidência).
Library             SeleniumLibrary    timeout=15s    run_on_failure=Capture Page Screenshot
Library             Collections
Library             ../libraries/Formatos.py
Resource            api.resource
Variables           ../config/variables.py


*** Keywords ***
Abrir Navegador
    [Documentation]    Abre ``BROWSER`` (chrome, firefox ou edge) sem cabeçalho quando HEADLESS=true.
    [Arguments]    ${largura}=${LARGURA_JANELA}    ${altura}=${ALTURA_JANELA}
    ${opcoes}=    Opcoes Do Navegador    ${BROWSER}    ${HEADLESS}    ${BROWSER_BINARY}    ${largura}    ${altura}
    IF    $DRIVER_PATH
        Open Browser    about:blank    ${BROWSER}    options=${opcoes}    service=executable_path='${DRIVER_PATH}'
    ELSE
        Open Browser    about:blank    ${BROWSER}    options=${opcoes}
    END
    Set Window Size    ${largura}    ${altura}

Fechar Navegador
    Close All Browsers

Entrar Na Interface Como
    [Documentation]    Faz o login de teste da persona e grava no sessionStorage o mesmo que o front
    ...    grava depois do login Google (token, expiração e usuário); em seguida abre ``caminho``.
    [Arguments]    ${persona}    ${caminho}=/portfolios
    ${email}=    Email Da Persona    ${persona}
    ${login}=    Login De Teste    ${email}
    ${usuario}=    Evaluate    json.dumps($login['usuario'])    modules=json
    Go To    ${FRONT_URL}/login
    Execute Javascript
    ...    sessionStorage.setItem('prumo_token', arguments[0]);
    ...    sessionStorage.setItem('prumo_expira_em', arguments[1]);
    ...    sessionStorage.setItem('prumo_usuario', arguments[2]);
    ...    ARGUMENTS    ${login}[token]    ${login}[expiraEm]    ${usuario}
    Abrir Tela    ${caminho}
    RETURN    ${login}[usuario]

Abrir Tela
    [Arguments]    ${caminho}
    Go To    ${FRONT_URL}${caminho}
    Aguardar Carregamento

Aguardar Carregamento
    Wait Until Page Does Not Contain    Carregando    timeout=15s
    Wait For Condition    return document.readyState === 'complete'

Titulo Da Pagina Deve Ser
    [Arguments]    ${titulo}
    Wait Until Element Contains    css:.page-header h1    ${titulo}

Url Deve Terminar Com
    [Arguments]    ${final}
    Wait Until Keyword Succeeds    10s    250ms    Url Termina Com    ${final}

Url Termina Com
    [Arguments]    ${final}
    ${url}=    Get Location
    Should End With    ${url}    ${final}

Clicar No Botao
    [Arguments]    ${texto}
    ${botao}=    Set Variable    xpath=//button[normalize-space(.)='${texto}' and not(@disabled)]
    Wait Until Element Is Visible    ${botao}
    Click Button    ${botao}

Botao Deve Estar Visivel
    [Arguments]    ${texto}
    Wait Until Element Is Visible    xpath=//button[normalize-space(.)='${texto}']

Botao Nao Deve Existir
    [Arguments]    ${texto}
    Page Should Not Contain Element    xpath=//button[normalize-space(.)='${texto}']

Marcar Opcao
    [Documentation]    Clica no rádio cujo rótulo começa com ``texto``.
    [Arguments]    ${texto}
    Click Element    xpath=//label[starts-with(normalize-space(.), '${texto}')]//input

Mensagem Deve Aparecer
    [Documentation]    Toast, alerta ou mensagem de erro com o texto informado.
    [Arguments]    ${texto}
    Wait Until Page Contains    ${texto}

Texto Do Card
    [Documentation]    Texto do card cujo título (.card-title) é ``titulo``.
    [Arguments]    ${titulo}
    ${card}=    Set Variable    xpath=//article[.//h2[contains(@class,'card-title') and normalize-space(.)='${titulo}']]
    Wait Until Element Is Visible    ${card}
    ${texto}=    Get Text    ${card}
    RETURN    ${texto}

Valor Do Card
    [Documentation]    Primeiro número do elemento ``seletor`` (padrão .kpi-value) dentro do card ``titulo``.
    [Arguments]    ${titulo}    ${seletor}=kpi-value    ${indice}=1
    ${xpath}=    Set Variable
    ...    xpath=(//article[.//h2[normalize-space(.)='${titulo}']]//*[contains(@class,'${seletor}')])[${indice}]
    Wait Until Element Is Visible    ${xpath}
    ${texto}=    Get Text    ${xpath}
    ${valor}=    Numero Brasileiro    ${texto}
    RETURN    ${valor}

Preencher
    [Documentation]    Espera o campo ficar visível e digita ``valor`` (substituindo o conteúdo).
    [Arguments]    ${locator}    ${valor}
    Wait Until Element Is Visible    ${locator}
    Input Text    ${locator}    ${valor}    clear=True

Selecionar
    [Arguments]    ${locator}    ${valor}    ${por}=value
    Wait Until Element Is Visible    ${locator}
    Wait Until Keyword Succeeds    5s    250ms    Run Keyword    Select From List By ${por}    ${locator}    ${valor}

Clicar
    [Documentation]    Espera o elemento ficar visível e habilitado e clica.
    [Arguments]    ${locator}
    Wait Until Element Is Visible    ${locator}
    Wait Until Element Is Enabled    ${locator}
    Click Element    ${locator}

Ativar Portfolio
    [Documentation]    Define o portfólio ativo, como quando o usuário clica no card (UC3).
    [Arguments]    ${portfolio_id}
    Execute Javascript    sessionStorage.setItem('portfolioAtivoId', arguments[0]);    ARGUMENTS    ${portfolio_id}
