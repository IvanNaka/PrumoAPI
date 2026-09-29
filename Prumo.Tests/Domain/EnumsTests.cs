using Prumo.Domain.Enums;

namespace Prumo.Tests.Domain
{
    // T01: os 12 enums precisam ter exatamente os valores da Seção 3.1 do plano.
    public class EnumsTests
    {
        private static string[] Names<T>() where T : struct, Enum => Enum.GetNames<T>();

        [Fact]
        public void Enums_TemExatamenteOsValoresDaEspecificacao()
        {
            Assert.Equal(new[] { "Desenvolvedor", "QA", "ProductOwner", "TechLead", "GerenteProjeto", "Diretoria", "Administrador" }, Names<RoleName>());
            Assert.Equal(new[] { "Rascunho", "Planejado", "EmAndamento", "EmRisco", "Suspenso", "Concluido", "Cancelado", "Arquivado" }, Names<ProjectStatus>());
            Assert.Equal(new[] { "Criado", "Configurado", "EmAnalise", "Priorizado", "Monitoramento", "Reavaliacao", "Encerrado" }, Names<PortfolioStatus>());
            Assert.Equal(new[] { "NaoAvaliado", "Avaliando", "Priorizado", "Reavaliado", "Aprovado", "Rejeitado" }, Names<EvaluationStatus>());
            Assert.Equal(new[] { "Baixa", "Media", "Alta", "Critica" }, Names<Priority>());
            Assert.Equal(new[] { "Run", "Grow", "Transform" }, Names<StrategicCategory>());
            Assert.Equal(new[] { "Beneficio", "Custo" }, Names<CriteriaType>());
            Assert.Equal(new[] { "Atraso", "Risco", "Conflito", "Desalinhamento", "EstouroOrcamento" }, Names<AlertType>());
            Assert.Equal(new[] { "Gerada", "Enfileirada", "Enviada", "Lida", "Ignorada", "Arquivada" }, Names<AlertStatus>());
            Assert.Equal(new[] { "NaoConfigurada", "Configurada", "TestandoConexao", "Conectada", "ErroConexao", "Sincronizando", "FalhaSincronizacao" }, Names<IntegrationStatus>());
            Assert.Equal(new[] { "Story", "Task", "Feature", "Bug" }, Names<ExternalIssueType>());
            Assert.Equal(new[] { "Custo", "Despesa" }, Names<BudgetExpenseCategory>());
        }

        [Fact]
        public void Prioridade_TemOsValoresNumericosDaEspecificacao()
        {
            Assert.Equal(1, (int)Priority.Baixa);
            Assert.Equal(2, (int)Priority.Media);
            Assert.Equal(3, (int)Priority.Alta);
            Assert.Equal(4, (int)Priority.Critica);
        }
    }
}
