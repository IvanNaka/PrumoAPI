namespace Prumo.Application.Common
{
    /// <summary>
    /// Mensagens das regras de negócio (Seção 3.3). O texto é exatamente o do documento.
    /// </summary>
    public static class Messages
    {
        public const string RN01_TokenInvalido = "Token de autenticação inválido.";
        public const string RN02_FalhaGoogle = "Não foi possível realizar o login. Tente novamente.";
        public const string RN03_SemPermissao = "Usuário sem permissão de acesso ao Prumo.";
        public const string RN04_CamposObrigatorios = "Preencha os campos obrigatórios.";
        public const string RN05_NenhumPortfolio = "Nenhum portfólio encontrado.";
        public const string RN06_NaoMembro = "Você não tem permissão para acessar este portfólio.";
        public const string RN07_PesoInvalido = "Peso inválido. Informe um valor maior que 0 e até 10.";
        public const string SomaPesosExcedida = "A soma dos pesos dos critérios não pode ultrapassar 10.";
        public const string SomaPesosDiferenteDeDez = "A soma dos pesos dos critérios deve ser exatamente 10.";
        public const string RN08_NomeCriterio = "O nome do critério é obrigatório.";
        public const string RN09_CriterioRepetido = "Já existe um critério com este nome neste portfólio.";
        public const string RN10_CriterioNaoEncontrado = "Critério não encontrado.";
        public const string RN11_CriterioComAvaliacoes = "Critério associado a avaliações não pode ser excluído.";
        public const string RN12_DataFim = "A data de término deve ser igual ou posterior à data de início.";
        public const string RN13_OkrSemKr = "Um objetivo precisa de pelo menos um Key Result.";
        public const string RN14_OkrNaoEncontrado = "OKR não encontrado.";
        public const string RN15_SemCriterios = "Cadastre ao menos um critério antes de priorizar.";
        public const string RN16_SemProjetos = "Não há projetos cadastrados para priorização.";
        public const string RN17_NotaInvalida = "A nota deve estar entre 1 e 5.";
        public const string RN18_DadosInsuficientes = "Dados insuficientes para calcular este indicador.";
        public const string RN19_Ciclo = "Dependência circular não permitida.";
        public const string RN20_AutoDependencia = "Um projeto não pode depender de si mesmo.";
        public const string RN21_DependenciaRepetida = "Esta dependência já existe.";
        public const string RN23_PortfolioEncerrado = "Portfólio encerrado não pode ser alterado.";
        public const string RN24_FalhaJira = "Falha de autenticação no Jira. Verifique URL, e-mail e token.";
        public const string RN25_JiraIndisponivel = "API do Jira indisponível. Nova tentativa agendada.";
        public const string RN26_TokenJiraExpirado = "Token do Jira expirado. Refaça a autenticação.";
        public const string RN27_PerfilSemPermissao = "Seu perfil não tem permissão para esta ação.";
        public const string RN28_LancamentoInvalido = "Lançamento inválido: valor deve ser positivo e data não pode ser futura.";
        public const string RetornoInvalido = "Retorno inválido: valor deve ser positivo e data não pode ser futura.";
        public const string RN29_SomentePriorizados = "Apenas projetos priorizados podem ser aprovados ou rejeitados.";

        // Acesso inicial: usuário sem perfil só pode entrar em uma equipe (código de convite) ou criar uma.
        public const string SemEquipe = "Entre em uma equipe ou crie uma para acessar o Prumo.";
        public const string CodigoConviteInvalido = "Código de convite inválido.";
        public const string JaParticipa = "Você já participa do Prumo.";
        public const string JaMembroEquipe = "Você já faz parte desta equipe.";

        public static string RN22_Transicao(object atual, string acao) =>
            $"Transição de '{atual}' para '{acao}' não permitida.";

        public static BusinessRuleException NotFound(string detail) => new(404, detail);
    }
}
