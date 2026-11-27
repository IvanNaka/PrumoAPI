*** Settings ***
Documentation       Sessões HTTP com a Prumo API: login de teste das personas e keywords de dados.
Library             RequestsLibrary
Library             Collections
Library             String
Library             DateTime
Library             ../libraries/PrumoToken.py
Library             ../libraries/Dados.py
Variables           ../config/variables.py


*** Keywords ***
# ---------- sessão ----------
Iniciar Sessao Anonima
    Create Session    anonima    ${API_URL}    verify=${False}    disable_warnings=1

Login De Teste
    [Documentation]    POST /api/auth/teste (seção 4.9 b). Devolve o LoginResponse (token, expiraEm, usuario).
    [Arguments]    ${email}
    Skip If    not $AUTH_TESTE_CHAVE
    ...    Defina PRUMO_AUTH_TESTE_CHAVE (a mesma AuthTeste:Chave da API) para entrar com as contas simuladas.
    Iniciar Sessao Anonima
    ${headers}=    Create Dictionary    X-Auth-Teste-Chave=${AUTH_TESTE_CHAVE}
    ${corpo}=    Create Dictionary    email=${email}
    ${resposta}=    POST On Session    anonima    /api/auth/teste    json=${corpo}    headers=${headers}
    ...    expected_status=any
    IF    ${resposta.status_code} == 404
        Fail    Login de teste desabilitado na API (AuthTeste:Habilitado/Chave) ou chave incorreta.
    END
    Should Be Equal As Integers    ${resposta.status_code}    200    Conta ${email} não pôde entrar: ${resposta.text}
    RETURN    ${resposta.json()}

Entrar Na API Como
    [Documentation]    Cria a sessão ``alias`` (padrão: o nome da persona) autenticada com a conta simulada.
    ...    ``persona`` é uma chave de EMAILS (Desenvolvedor, QA, ProductOwner, TechLead, GerenteProjeto,
    ...    Diretoria, Validacao) ou Admin.
    [Arguments]    ${persona}    ${alias}=${persona}
    ${email}=    Email Da Persona    ${persona}
    ${login}=    Login De Teste    ${email}
    ${headers}=    Create Dictionary    Authorization=Bearer ${login}[token]    Content-Type=application/json
    Create Session    ${alias}    ${API_URL}    headers=${headers}    verify=${False}    disable_warnings=1
    RETURN    ${login}[usuario]

Email Da Persona
    [Arguments]    ${persona}
    IF    $persona == 'Admin'    RETURN    ${EMAIL_ADMIN}
    RETURN    ${EMAILS}[${persona}]

Validar Problem Details
    [Documentation]    Erros da API seguem a RFC 7807 (title/status/detail).
    [Arguments]    ${resposta}    ${status}    ${detail}=${None}
    Should Be Equal As Integers    ${resposta.status_code}    ${status}    ${resposta.text}
    ${corpo}=    Set Variable    ${resposta.json()}
    Should Be Equal As Integers    ${corpo}[status]    ${status}
    IF    $detail is not None    Should Be Equal    ${corpo}[detail]    ${detail}

Status Deve Ser
    [Arguments]    ${resposta}    ${status}
    Should Be Equal As Integers    ${resposta.status_code}    ${status}
    ...    ${resposta.request.method} ${resposta.request.path_url}: esperado ${status}, obtido ${resposta.status_code} — ${resposta.text}

# ---------- dados de teste (cada suíte cria os seus, sem tocar na massa de validação) ----------
Nome Unico
    [Arguments]    ${prefixo}
    ${nome}=    Gerar Nome Unico    ${prefixo}
    RETURN    ${nome}

Data Relativa
    [Documentation]    Data (AAAA-MM-DD) ``dias`` a partir de hoje.
    [Arguments]    ${dias}
    ${data}=    Get Current Date    UTC    increment=${dias} days    result_format=%Y-%m-%d
    RETURN    ${data}

Criar Portfolio Via API
    [Arguments]    ${sessao}    ${nome}    ${responsavel_id}    ${objetivo}=Portfólio criado pelos testes automatizados
    ${corpo}=    Create Dictionary    nome=${nome}    descricao=Criado pelo Robot Framework
    ...    objetivo=${objetivo}    responsavelId=${responsavel_id}
    ${resposta}=    POST On Session    ${sessao}    /api/portfolios    json=${corpo}    expected_status=201
    RETURN    ${resposta.json()}

Adicionar Membro Ao Portfolio
    [Arguments]    ${sessao}    ${portfolio_id}    ${usuario_id}
    ${corpo}=    Create Dictionary    usuarioId=${usuario_id}
    POST On Session    ${sessao}    /api/portfolios/${portfolio_id}/membros    json=${corpo}    expected_status=200

Criar Criterio Via API
    [Arguments]    ${sessao}    ${portfolio_id}    ${nome}    ${peso}    ${tipo}=Beneficio    ${status}=201
    ${corpo}=    Create Dictionary    nome=${nome}    descricao=Critério de teste    peso=${peso}    tipo=${tipo}
    ${resposta}=    POST On Session    ${sessao}    /api/portfolios/${portfolio_id}/criterios    json=${corpo}
    ...    expected_status=${status}
    RETURN    ${resposta}

Criar Projeto Via API
    [Arguments]    ${sessao}    ${portfolio_id}    ${nome}    ${responsavel_id}    ${orcamento}=100000
    ...    ${categoria}=Grow    ${prioridade}=Media    ${inicio}=-30    ${fim}=180    ${status}=201
    ${data_inicio}=    Data Relativa    ${inicio}
    ${data_fim}=    Data Relativa    ${fim}
    ${corpo}=    Create Dictionary    nome=${nome}    descricao=Projeto de teste    responsavelId=${responsavel_id}
    ...    dataInicio=${data_inicio}    dataFim=${data_fim}    orcamentoAprovado=${orcamento}
    ...    categoriaEstrategica=${categoria}    prioridade=${prioridade}
    ${resposta}=    POST On Session    ${sessao}    /api/portfolios/${portfolio_id}/projetos    json=${corpo}
    ...    expected_status=${status}
    RETURN    ${resposta}

Alterar Status Do Projeto
    [Arguments]    ${sessao}    ${projeto_id}    ${acao}    ${status}=200
    ${corpo}=    Create Dictionary    acao=${acao}
    ${resposta}=    POST On Session    ${sessao}    /api/projetos/${projeto_id}/status    json=${corpo}
    ...    expected_status=${status}
    RETURN    ${resposta}

Avaliar Projeto
    [Documentation]    ``notas``: dicionário {criterioId: nota}.
    [Arguments]    ${sessao}    ${projeto_id}    ${notas}    ${status}=200
    ${lista}=    Create List
    FOR    ${criterio}    ${nota}    IN    &{notas}
        ${item}=    Create Dictionary    criterioId=${criterio}    nota=${nota}
        Append To List    ${lista}    ${item}
    END
    ${resposta}=    PUT On Session    ${sessao}    /api/projetos/${projeto_id}/avaliacoes    json=${lista}
    ...    expected_status=${status}
    RETURN    ${resposta}

Criar OKR Via API
    [Arguments]    ${sessao}    ${titulo}    @{krs}
    [Documentation]    ``krs``: dicionários {descricao, meta, valorAtual}.
    ${lista}=    Create List    @{krs}
    ${corpo}=    Create Dictionary    titulo=${titulo}    descricao=OKR de teste    keyResults=${lista}
    ${resposta}=    POST On Session    ${sessao}    /api/okrs    json=${corpo}    expected_status=201
    RETURN    ${resposta.json()}

Key Result
    [Arguments]    ${descricao}    ${meta}    ${atual}
    ${kr}=    Create Dictionary    descricao=${descricao}    meta=${meta}    valorAtual=${atual}
    RETURN    ${kr}

Buscar Projeto
    [Arguments]    ${sessao}    ${projeto_id}
    ${resposta}=    GET On Session    ${sessao}    /api/projetos/${projeto_id}    expected_status=200
    RETURN    ${resposta.json()}

Id Do Projeto Pelo Nome
    [Arguments]    ${sessao}    ${portfolio_id}    ${nome}
    ${resposta}=    GET On Session    ${sessao}    /api/portfolios/${portfolio_id}/projetos    expected_status=200
    FOR    ${projeto}    IN    @{resposta.json()}
        IF    $projeto['nome'] == $nome    RETURN    ${projeto}[id]
    END
    Fail    Projeto "${nome}" não encontrado no portfólio ${portfolio_id}.
