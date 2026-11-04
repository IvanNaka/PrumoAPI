*** Settings ***
Documentation       Controle de acesso — todos os perfis (Tabela 20; RF01 a RF03, UC1).
...                 Ações permitidas e bloqueadas para cada persona, autenticação e gestão de usuários.
Resource            ../resources/ui.resource
Suite Setup         Preparar Suite
Suite Teardown      Fechar Navegador
Test Tags           suite:controle-acesso


*** Variables ***
${RN27}             Seu perfil não tem permissão para esta ação.
${RN03}             Usuário sem permissão de acesso ao Prumo.
${INEXISTENTE}      00000000-0000-4000-8000-0000000000aa


*** Test Cases ***
# ---------- RF01 / UC1: autenticação ----------
API responde ao health check
    [Tags]    RF01    UC1    api    smoke
    ${resposta}=    GET On Session    anonima    /    expected_status=200
    Should Be Equal    ${resposta.text}    Healthy

Rota protegida sem token retorna 401
    [Tags]    RF01    UC1    api    smoke    critico
    ${resposta}=    GET On Session    anonima    /api/portfolios    expected_status=401
    Validar Problem Details    ${resposta}    401    Token de autenticação inválido.

Token com assinatura inválida retorna 401
    [Tags]    RF01    UC1    api    critico
    ${headers}=    Create Dictionary    Authorization=Bearer eyJhbGciOiJIUzI1NiJ9.e30.assinatura-falsa
    ${resposta}=    GET On Session    anonima    /api/auth/me    headers=${headers}    expected_status=401
    Validar Problem Details    ${resposta}    401

Token expirado retorna 401
    [Tags]    RF01    UC1    api
    Skip If    not $JWT_KEY    Defina PRUMO_JWT_KEY para gerar um token expirado.
    ${token}=    Gerar Token Expirado    ${JWT_KEY}    ${JWT_ISSUER}    ${JWT_AUDIENCE}
    ${headers}=    Create Dictionary    Authorization=Bearer ${token}
    ${resposta}=    GET On Session    anonima    /api/auth/me    headers=${headers}    expected_status=401
    Validar Problem Details    ${resposta}    401

Login Google com token inválido é recusado
    [Tags]    RF01    UC1    api
    ${corpo}=    Create Dictionary    idToken=nao-e-um-token-do-google
    ${resposta}=    POST On Session    anonima    /api/auth/google    json=${corpo}    expected_status=401
    Validar Problem Details    ${resposta}    401    Token de autenticação inválido.

Cada persona entra com o próprio perfil
    [Tags]    RF01    RF02    UC1    api    smoke
    [Template]    Persona Deve Ter Perfil
    Desenvolvedor     Desenvolvedor
    QA                QA
    ProductOwner      ProductOwner
    TechLead          TechLead
    GerenteProjeto    GerenteProjeto
    Diretoria         Diretoria

Tela de login apresenta a opção de entrar com Google
    [Tags]    RF01    UC1    ui    smoke
    Abrir Tela    /login
    Element Should Be Visible    css:img[alt="Prumo"]
    Element Text Should Be    css:.login-subtitle    Gestão de portfólio de projetos de TI
    Page Should Contain Element    css:.google-signin-slot

Rota protegida sem sessão redireciona para o login
    [Tags]    RF01    UC1    ui    critico
    Go To    ${FRONT_URL}/login
    Execute Javascript    sessionStorage.clear()
    Go To    ${FRONT_URL}/portfolios
    Url Deve Terminar Com    /login

# ---------- RF02: ações permitidas e bloqueadas por perfil ----------
Cadastrar portfólio: PO e Gerente sim; demais perfis não
    [Tags]    RF02    RF04    api    permissoes    critico
    [Template]    Permissao Deve Ser
    ProductOwner      POST    /api/portfolios    permitido
    GerenteProjeto    POST    /api/portfolios    permitido
    Desenvolvedor     POST    /api/portfolios    bloqueado
    QA                POST    /api/portfolios    bloqueado
    TechLead          POST    /api/portfolios    bloqueado
    Diretoria         POST    /api/portfolios    bloqueado

Gerir critérios: só o PO
    [Tags]    RF02    RF07    api    permissoes    critico
    [Template]    Permissao Deve Ser
    ProductOwner      POST    /api/portfolios/${INEXISTENTE}/criterios    permitido
    GerenteProjeto    POST    /api/portfolios/${INEXISTENTE}/criterios    bloqueado
    Desenvolvedor     POST    /api/portfolios/${INEXISTENTE}/criterios    bloqueado
    QA                POST    /api/portfolios/${INEXISTENTE}/criterios    bloqueado
    TechLead          POST    /api/portfolios/${INEXISTENTE}/criterios    bloqueado
    Diretoria         POST    /api/portfolios/${INEXISTENTE}/criterios    bloqueado

Cadastrar projetos: só o Gerente de Projetos
    [Tags]    RF02    RF10    api    permissoes    critico
    [Template]    Permissao Deve Ser
    GerenteProjeto    POST    /api/portfolios/${INEXISTENTE}/projetos    permitido
    ProductOwner      POST    /api/portfolios/${INEXISTENTE}/projetos    bloqueado
    Desenvolvedor     POST    /api/portfolios/${INEXISTENTE}/projetos    bloqueado
    QA                POST    /api/portfolios/${INEXISTENTE}/projetos    bloqueado
    TechLead          POST    /api/portfolios/${INEXISTENTE}/projetos    bloqueado
    Diretoria         POST    /api/portfolios/${INEXISTENTE}/projetos    bloqueado

Priorizar e definir OKRs: só o PO
    [Tags]    RF02    RF14    RF19    api    permissoes    critico
    [Template]    Permissao Deve Ser
    ProductOwner      POST    /api/portfolios/${INEXISTENTE}/priorizacao    permitido
    GerenteProjeto    POST    /api/portfolios/${INEXISTENTE}/priorizacao    bloqueado
    Diretoria         POST    /api/portfolios/${INEXISTENTE}/priorizacao    bloqueado
    ProductOwner      POST    /api/okrs                                     permitido
    GerenteProjeto    POST    /api/okrs                                     bloqueado
    TechLead          POST    /api/okrs                                     bloqueado
    Desenvolvedor     POST    /api/okrs                                     bloqueado

Governar portfólio: Gerente e Diretoria
    [Tags]    RF02    api    permissoes
    [Template]    Permissao Deve Ser
    GerenteProjeto    POST    /api/portfolios/${INEXISTENTE}/acoes/aprovar    permitido
    Diretoria         POST    /api/portfolios/${INEXISTENTE}/acoes/aprovar    permitido
    ProductOwner      POST    /api/portfolios/${INEXISTENTE}/acoes/aprovar    bloqueado
    TechLead          POST    /api/portfolios/${INEXISTENTE}/acoes/aprovar    bloqueado
    QA                POST    /api/portfolios/${INEXISTENTE}/acoes/aprovar    bloqueado

Financeiro: Gerente edita; PO e Diretoria consultam; time técnico não
    [Tags]    RF02    RF23    api    permissoes    critico
    [Template]    Permissao Deve Ser
    GerenteProjeto    POST    /api/projetos/${INEXISTENTE}/lancamentos    permitido
    ProductOwner      POST    /api/projetos/${INEXISTENTE}/lancamentos    bloqueado
    Diretoria         POST    /api/projetos/${INEXISTENTE}/lancamentos    bloqueado
    ProductOwner      GET     /api/projetos/${INEXISTENTE}/lancamentos    permitido
    Diretoria         GET     /api/projetos/${INEXISTENTE}/lancamentos    permitido
    Desenvolvedor     GET     /api/projetos/${INEXISTENTE}/lancamentos    bloqueado
    QA                GET     /api/projetos/${INEXISTENTE}/lancamentos    bloqueado
    TechLead          GET     /api/projetos/${INEXISTENTE}/lancamentos    bloqueado

Dependências: Tech Lead e Gerente
    [Tags]    RF02    RF26    api    permissoes
    [Template]    Permissao Deve Ser
    TechLead          POST    /api/dependencias    permitido
    GerenteProjeto    POST    /api/dependencias    permitido
    ProductOwner      POST    /api/dependencias    bloqueado
    Desenvolvedor     POST    /api/dependencias    bloqueado
    QA                POST    /api/dependencias    bloqueado
    Diretoria         POST    /api/dependencias    bloqueado

Equipes: só o Tech Lead
    [Tags]    RF02    RF29    api    permissoes
    [Template]    Permissao Deve Ser
    TechLead          POST    /api/equipes    permitido
    GerenteProjeto    POST    /api/equipes    bloqueado
    ProductOwner      POST    /api/equipes    bloqueado
    Desenvolvedor     POST    /api/equipes    bloqueado
    QA                POST    /api/equipes    bloqueado
    Diretoria         POST    /api/equipes    bloqueado

Integração Jira: só o Desenvolvedor
    [Tags]    RF02    RF47    api    permissoes
    [Template]    Permissao Deve Ser
    Desenvolvedor     GET    /api/integracoes/jira    permitido
    QA                GET    /api/integracoes/jira    bloqueado
    TechLead          GET    /api/integracoes/jira    bloqueado
    ProductOwner      GET    /api/integracoes/jira    bloqueado
    GerenteProjeto    GET    /api/integracoes/jira    bloqueado
    Diretoria         GET    /api/integracoes/jira    bloqueado

Relatórios: Gerente, PO e Diretoria
    [Tags]    RF02    RF43    api    permissoes
    [Template]    Permissao Deve Ser
    GerenteProjeto    GET    /api/portfolios/${INEXISTENTE}/relatorios/portfolio    permitido
    ProductOwner      GET    /api/portfolios/${INEXISTENTE}/relatorios/portfolio    permitido
    Diretoria         GET    /api/portfolios/${INEXISTENTE}/relatorios/executivo    permitido
    Desenvolvedor     GET    /api/portfolios/${INEXISTENTE}/relatorios/portfolio    bloqueado
    QA                GET    /api/portfolios/${INEXISTENTE}/relatorios/portfolio    bloqueado
    TechLead          GET    /api/portfolios/${INEXISTENTE}/relatorios/portfolio    bloqueado

Gestão de usuários: só o Administrador
    [Tags]    RF02    RF03    api    permissoes    critico
    [Template]    Permissao Deve Ser
    Admin             GET    /api/usuarios    permitido
    ProductOwner      GET    /api/usuarios    bloqueado
    GerenteProjeto    GET    /api/usuarios    bloqueado
    Diretoria         GET    /api/usuarios    bloqueado
    TechLead          GET    /api/usuarios    bloqueado
    QA                GET    /api/usuarios    bloqueado
    Desenvolvedor     GET    /api/usuarios    bloqueado

Quem não é membro não acessa o portfólio
    [Tags]    RF02    RF05    api    permissoes    critico
    ${po}=    Entrar Na API Como    ProductOwner
    ${nome}=    Nome Unico    Portfolio Restrito
    ${portfolio}=    Criar Portfolio Via API    ProductOwner    ${nome}    ${po}[id]
    Entrar Na API Como    Desenvolvedor
    ${resposta}=    GET On Session    Desenvolvedor    /api/portfolios/${portfolio}[id]    expected_status=403
    Validar Problem Details    ${resposta}    403    Você não tem permissão para acessar este portfólio.
    ${lista}=    GET On Session    Desenvolvedor    /api/portfolios    expected_status=200
    ${ids}=    Evaluate    [p['id'] for p in $lista.json()]
    Should Not Contain    ${ids}    ${portfolio}[id]

Desenvolvedor não vê ações de gestão na interface
    [Tags]    RF02    ui    permissoes
    Entrar Na Interface Como    Desenvolvedor    /portfolios
    Titulo Da Pagina Deve Ser    Portfólios
    Botao Nao Deve Existir    Novo portfólio
    Abrir Tela    /admin/usuarios
    Mensagem Deve Aparecer    ${RN27}
    Url Deve Terminar Com    /portfolios

Product Owner vê o cadastro de portfólio
    [Tags]    RF02    RF04    ui    permissoes
    Entrar Na Interface Como    ProductOwner    /portfolios
    Botao Deve Estar Visivel    Novo portfólio

Diretoria não acessa a integração com o Jira
    [Tags]    RF02    RF47    ui    permissoes
    Entrar Na Interface Como    Diretoria    /integracoes/jira
    Mensagem Deve Aparecer    ${RN27}
    Url Deve Terminar Com    /portfolios

# ---------- RF03: gestão de usuários ----------
Administrador cadastra, edita, desativa e reativa um usuário
    [Tags]    RF03    api    critico
    Entrar Na API Como    Admin
    ${sufixo}=    Evaluate    __import__('uuid').uuid4().hex[:8]
    ${email}=    Set Variable    usuario.${sufixo}@prumo.test
    ${perfis}=    Create List    QA
    ${corpo}=    Create Dictionary    nome=Usuário ${sufixo}    email=${email}    perfis=${perfis}
    ${criado}=    POST On Session    Admin    /api/usuarios    json=${corpo}    expected_status=201
    ${id}=    Set Variable    ${criado.json()}[id]
    ${login}=    Login De Teste    ${email}
    Should Contain    ${login}[usuario][perfis]    QA

    ${novos}=    Create List    QA    Desenvolvedor
    ${edicao}=    Create Dictionary    nome=Usuário ${sufixo} (editado)    perfis=${novos}
    ${editado}=    PUT On Session    Admin    /api/usuarios/${id}    json=${edicao}    expected_status=200
    Should Be Equal    ${editado.json()}[nome]    Usuário ${sufixo} (editado)

    ${inativo}=    Create Dictionary    ativo=${False}
    PATCH On Session    Admin    /api/usuarios/${id}/ativo    json=${inativo}    expected_status=any
    ${negado}=    Login Recusado    ${email}
    Should Be Equal As Integers    ${negado.status_code}    403
    Validar Problem Details    ${negado}    403    ${RN03}

    ${ativo}=    Create Dictionary    ativo=${True}
    PATCH On Session    Admin    /api/usuarios/${id}/ativo    json=${ativo}    expected_status=any
    ${login}=    Login De Teste    ${email}
    Should Be Equal    ${login}[usuario][email]    ${email}
    Set Suite Variable    ${USUARIO_CRIADO}    ${email}

Administrador vê o usuário cadastrado na tela de usuários
    [Tags]    RF03    ui
    Skip If    not $USUARIO_CRIADO    Depende do teste anterior.
    Entrar Na Interface Como    Admin    /admin/usuarios
    Titulo Da Pagina Deve Ser    Usuários
    Wait Until Page Contains    ${USUARIO_CRIADO}


*** Keywords ***
Preparar Suite
    Iniciar Sessao Anonima
    Set Suite Variable    ${USUARIO_CRIADO}    ${EMPTY}
    Abrir Navegador

Persona Deve Ter Perfil
    [Arguments]    ${persona}    ${perfil}
    ${usuario}=    Entrar Na API Como    ${persona}
    Should Contain    ${usuario}[perfis]    ${perfil}
    ${me}=    GET On Session    ${persona}    /api/auth/me    expected_status=200
    Should Be Equal    ${me.json()}[email]    ${usuario}[email]

Permissao Deve Ser
    [Documentation]    ``bloqueado``: 403 com a mensagem RN27 (a policy barra antes de qualquer efeito).
    ...    ``permitido``: a policy deixa passar (a requisição pode falhar depois por validação, 400/404,
    ...    pois os testes usam ids inexistentes e corpos vazios para não alterar dados).
    [Arguments]    ${persona}    ${metodo}    ${caminho}    ${esperado}
    Entrar Na API Como    ${persona}
    ${vazio}=    Create Dictionary
    IF    $metodo == 'GET'
        ${resposta}=    GET On Session    ${persona}    ${caminho}    expected_status=any
    ELSE
        ${resposta}=    Run Keyword    ${metodo} On Session    ${persona}    ${caminho}    json=${vazio}
        ...    expected_status=any
    END
    ${detalhe}=    Evaluate    ($resposta.json() or {}).get('detail') if $resposta.text and $resposta.text.startswith('{') else None
    IF    $esperado == 'bloqueado'
        Should Be Equal As Integers    ${resposta.status_code}    403    ${persona} ${metodo} ${caminho} deveria ser bloqueado
        Should Be Equal    ${detalhe}    ${RN27}
    ELSE
        Should Not Be Equal As Integers    ${resposta.status_code}    401
        Should Not Be True    ${resposta.status_code} == 403 and $detalhe == $RN27
        ...    ${persona} ${metodo} ${caminho} deveria ser permitido (${resposta.status_code} ${detalhe})
    END

Login Recusado
    [Arguments]    ${email}
    ${headers}=    Create Dictionary    X-Auth-Teste-Chave=${AUTH_TESTE_CHAVE}
    ${corpo}=    Create Dictionary    email=${email}
    ${resposta}=    POST On Session    anonima    /api/auth/teste    json=${corpo}    headers=${headers}
    ...    expected_status=any
    RETURN    ${resposta}
