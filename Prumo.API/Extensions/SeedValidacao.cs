using Microsoft.EntityFrameworkCore;
using Plantonize.Plantao.Infrastructure;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.API.Extensions
{
    /// <summary>
    /// Dados de validação do Cap. 4, seção 4.3 (T25). Roda só com <c>PRUMO_SEED_VALIDACAO=true</c> e é
    /// idempotente: usuários são conferidos pelo e-mail e o restante só é criado se o portfólio de
    /// validação ainda não existir. Os e-mails (contas Google reais) vêm de <c>SeedValidacao:Emails:*</c>.
    /// </summary>
    public static class SeedValidacao
    {
        public static readonly Guid Portfolio1Id = new("5eed0000-0000-4000-8000-000000000001");
        public static readonly Guid Portfolio2Id = new("5eed0000-0000-4000-8000-000000000002");

        private static readonly (string Chave, string Nome, string EmailPadrao, RoleName[] Perfis)[] Usuarios =
        {
            ("Desenvolvedor", "Ana Desenvolvedora", "desenvolvedor@prumo", new[] { RoleName.Desenvolvedor }),
            ("QA", "Paulo QA", "qa@prumo", new[] { RoleName.QA }),
            ("ProductOwner", "Priscila Product Owner", "po@prumo", new[] { RoleName.ProductOwner }),
            ("TechLead", "Tiago Tech Lead", "techlead@prumo", new[] { RoleName.TechLead }),
            ("GerenteProjeto", "Gabriela Gerente", "gerente@prumo", new[] { RoleName.GerenteProjeto }),
            ("Diretoria", "Daniel Diretor", "diretoria@prumo", new[] { RoleName.Diretoria }),
            // D15: um usuário com vários perfis para percorrer o roteiro inteiro.
            ("Validacao", "Usuário de Validação", "validacao@prumo", Enum.GetValues<RoleName>()),
        };

        public static bool Habilitado(IConfiguration configuration) =>
            string.Equals(configuration["PRUMO_SEED_VALIDACAO"], "true", StringComparison.OrdinalIgnoreCase);

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedValidacao");
            var db = scope.ServiceProvider.GetRequiredService<PrumoDbContext>();

            var users = await SeedUsersAsync(db, configuration);
            if (await db.Portfolios.AnyAsync(p => p.Id == Portfolio1Id))
            {
                logger.LogInformation("Seed de validação já aplicado; nada a fazer.");
                return;
            }

            await SeedDataAsync(db, users);

            // Score e ranking (F1/F2) dos projetos com avaliação completa.
            var prioritization = scope.ServiceProvider.GetRequiredService<IPrioritizationService>();
            await prioritization.RecalculateIfNeededAsync(Portfolio1Id);
            await prioritization.RecalculateIfNeededAsync(Portfolio2Id);

            // Notificações dos 5 tipos a partir dos dados (T21).
            var criadas = await scope.ServiceProvider.GetRequiredService<INotificationRulesService>().ExecuteAsync();
            logger.LogInformation("Seed de validação aplicado ({Criadas} notificação(ões) gerada(s)).", criadas);
        }

        private static async Task<Dictionary<string, User>> SeedUsersAsync(PrumoDbContext db, IConfiguration configuration)
        {
            var result = new Dictionary<string, User>();
            foreach (var (chave, nome, padrao, perfis) in Usuarios)
            {
                var email = (configuration[$"SeedValidacao:Emails:{chave}"] ?? padrao).Trim().ToLowerInvariant();
                var user = await db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    user = new User { Name = nome, Email = email, IsActive = true };
                    db.Users.Add(user);
                }

                foreach (var perfil in perfis.Where(p => user.Roles.All(r => r.Role != p)))
                {
                    var role = new UserRole { UserId = user.Id, Role = perfil };
                    user.Roles.Add(role);
                    if (db.Entry(user).State != EntityState.Added)
                    {
                        db.UserRoles.Add(role);
                    }
                }

                result[chave] = user;
            }

            await db.SaveChangesAsync();
            return result;
        }

        private static async Task SeedDataAsync(PrumoDbContext db, Dictionary<string, User> u)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var agora = DateTime.UtcNow;
            DateTime Dia(int dias) => agora.Date.AddDays(dias).AddHours(12);

            // Portfólios, com todos os usuários como membros.
            var p1 = new Portfolio
            {
                Id = Portfolio1Id, Name = "Transformação Digital", OwnerId = u["GerenteProjeto"].Id, Status = PortfolioStatus.Monitoramento,
                Description = "Iniciativas de canais digitais e modernização da plataforma.",
                Goal = "Digitalizar os canais de atendimento ao cliente até o fim de 2027.",
            };
            var p2 = new Portfolio
            {
                Id = Portfolio2Id, Name = "Eficiência Operacional", OwnerId = u["ProductOwner"].Id, Status = PortfolioStatus.Priorizado,
                Description = "Automação de processos internos e continuidade dos sistemas.",
                Goal = "Reduzir em 15% o custo operacional anual.",
            };
            foreach (var portfolio in new[] { p1, p2 })
            {
                foreach (var user in u.Values)
                {
                    portfolio.Members.Add(new PortfolioMember { PortfolioId = portfolio.Id, UserId = user.Id });
                }
            }
            db.Portfolios.AddRange(p1, p2);

            // 4 critérios por portfólio.
            var criterios = new Dictionary<Guid, List<PriorityCriteria>>();
            foreach (var portfolio in new[] { p1, p2 })
            {
                criterios[portfolio.Id] = new List<PriorityCriteria>
                {
                    new() { Name = "Valor de negócio", Description = "Retorno esperado para o negócio.", Type = CriteriaType.Beneficio, ValueWeight = 4, PortfolioId = portfolio.Id, UserId = portfolio.OwnerId },
                    new() { Name = "Esforço", Description = "Tamanho do trabalho (quanto menor, melhor).", Type = CriteriaType.Custo, ValueWeight = 2, PortfolioId = portfolio.Id, UserId = portfolio.OwnerId },
                    new() { Name = "Risco", Description = "Incerteza técnica e de negócio (quanto menor, melhor).", Type = CriteriaType.Custo, ValueWeight = 2, PortfolioId = portfolio.Id, UserId = portfolio.OwnerId },
                    new() { Name = "Alinhamento estratégico", Description = "Aderência aos OKRs da empresa.", Type = CriteriaType.Beneficio, ValueWeight = 2, PortfolioId = portfolio.Id, UserId = portfolio.OwnerId },
                };
                db.PriorityCriterias.AddRange(criterios[portfolio.Id]);
            }

            // 12 projetos (6 por portfólio).
            Project Projeto(Portfolio portfolio, string nome, StrategicCategory categoria, ProjectStatus status, Priority prioridade,
                decimal orcamento, int inicio, int fim, string? jira = null, string? owner = null) => new()
            {
                PortfolioId = portfolio.Id, Name = nome, StrategicCategory = categoria, Status = status, Priority = prioridade,
                ApprovedBudget = orcamento, StartDate = hoje.AddDays(inicio), EndDate = hoje.AddDays(fim), JiraProjectKey = jira,
                OwnerId = u[owner ?? "GerenteProjeto"].Id, Description = $"Projeto de validação: {nome}.",
                CompletedAt = status == ProjectStatus.Concluido ? Dia(fim - 5) : null,
            };

            var portal = Projeto(p1, "Portal do Cliente", StrategicCategory.Grow, ProjectStatus.EmAndamento, Priority.Alta, 400_000, -120, 120, "PORTAL");
            var app = Projeto(p1, "App Mobile", StrategicCategory.Transform, ProjectStatus.EmRisco, Priority.Critica, 300_000, -90, 60);
            var nuvem = Projeto(p1, "Migração para Nuvem", StrategicCategory.Transform, ProjectStatus.EmAndamento, Priority.Alta, 250_000, -200, -10);
            var chatbot = Projeto(p1, "Chatbot de Atendimento", StrategicCategory.Grow, ProjectStatus.EmAndamento, Priority.Media, 80_000, -60, 90);
            var erp = Projeto(p1, "Manutenção do ERP", StrategicCategory.Run, ProjectStatus.EmAndamento, Priority.Media, 150_000, -300, 65);
            var bi = Projeto(p1, "BI Comercial", StrategicCategory.Grow, ProjectStatus.Planejado, Priority.Baixa, 120_000, 10, 200);
            var faturamento = Projeto(p2, "Automação de Faturamento", StrategicCategory.Grow, ProjectStatus.EmAndamento, Priority.Alta, 200_000, -100, 150, owner: "ProductOwner");
            var parque = Projeto(p2, "Renovação do Parque de TI", StrategicCategory.Run, ProjectStatus.Suspenso, Priority.Media, 180_000, -150, 100, owner: "ProductOwner");
            var contratos = Projeto(p2, "Gestão de Contratos", StrategicCategory.Run, ProjectStatus.Concluido, Priority.Media, 90_000, -300, -30, owner: "ProductOwner");
            var fornecedores = Projeto(p2, "Portal de Fornecedores", StrategicCategory.Transform, ProjectStatus.Planejado, Priority.Alta, 220_000, 20, 300, owner: "ProductOwner");
            var seguranca = Projeto(p2, "Segurança da Informação", StrategicCategory.Run, ProjectStatus.EmAndamento, Priority.Critica, 160_000, -80, 180, owner: "ProductOwner");
            var dataLake = Projeto(p2, "Data Lake", StrategicCategory.Transform, ProjectStatus.Rascunho, Priority.Media, 350_000, 30, 400, owner: "ProductOwner");
            var projetosP1 = new[] { portal, app, nuvem, chatbot, erp, bi };
            var projetosP2 = new[] { faturamento, parque, contratos, fornecedores, seguranca, dataLake };
            db.Projects.AddRange(projetosP1.Concat(projetosP2));

            // Notas (1 a 5) em todos os critérios para 10 projetos; BI Comercial e Data Lake ficam incompletos.
            var notas = new Dictionary<Project, int[]>
            {
                [portal] = new[] { 5, 3, 2, 5 }, [app] = new[] { 5, 4, 4, 4 }, [nuvem] = new[] { 4, 4, 3, 4 },
                [chatbot] = new[] { 3, 2, 2, 3 }, [erp] = new[] { 3, 2, 1, 2 }, [bi] = new[] { 4, 3 },
                [faturamento] = new[] { 5, 2, 2, 4 }, [parque] = new[] { 3, 3, 2, 2 }, [contratos] = new[] { 3, 2, 1, 3 },
                [fornecedores] = new[] { 4, 4, 3, 5 }, [seguranca] = new[] { 5, 3, 2, 4 }, [dataLake] = new[] { 4 },
            };
            foreach (var (projeto, valores) in notas)
            {
                var lista = criterios[projeto.PortfolioId];
                for (var i = 0; i < valores.Length; i++)
                {
                    db.ProjectEvaluations.Add(new ProjectEvaluation
                    {
                        ProjectId = projeto.Id, PriorityCriteriaId = lista[i].Id, UserId = u["Validacao"].Id, Score = valores[i], EvaluatedAt = agora,
                    });
                }

                var completo = valores.Length == lista.Count;
                projeto.EvaluationStatus = completo ? EvaluationStatus.Priorizado : EvaluationStatus.Avaliando;
                projeto.LastPrioritizationDate = completo ? agora : null;
            }
            faturamento.EvaluationStatus = EvaluationStatus.Aprovado;

            // 3 OKRs com 2 KRs cada, associados aos portfólios e à maioria dos projetos (BI Comercial fica sem OKR).
            Objective Okr(string titulo, params (string Kr, decimal Meta, decimal Atual)[] krs)
            {
                var o = new Objective { Title = titulo, StartDate = new DateOnly(hoje.Year, 1, 1), EndDate = new DateOnly(hoje.Year, 12, 31) };
                foreach (var (kr, meta, atual) in krs)
                {
                    o.KeyResults.Add(new KeyResult { ObjectiveId = o.Id, Title = kr, TargetValue = meta, CurrentValue = atual });
                }
                return o;
            }

            var okrCliente = Okr("Aumentar a satisfação do cliente nos canais digitais",
                ("NPS dos canais digitais", 70, 52), ("Atendimentos resolvidos no autoatendimento (%)", 60, 35));
            var okrCustos = Okr("Reduzir o custo operacional",
                ("Economia anual (R$ mil)", 1500, 600), ("Processos automatizados", 20, 9));
            var okrContinuidade = Okr("Garantir a continuidade dos sistemas críticos",
                ("Disponibilidade dos sistemas (%)", 99.9m, 99.5m), ("Sistemas com plano de contingência", 12, 7));
            db.Objectives.AddRange(okrCliente, okrCustos, okrContinuidade);
            foreach (var (portfolio, okr) in new[] { (p1, okrCliente), (p1, okrCustos), (p1, okrContinuidade), (p2, okrCustos), (p2, okrContinuidade) })
            {
                db.PortfolioObjectives.Add(new PortfolioObjective { PortfolioId = portfolio.Id, ObjectiveId = okr.Id });
            }
            foreach (var (projeto, okr) in new[]
                     {
                         (portal, okrCliente), (app, okrCliente), (chatbot, okrCliente), (nuvem, okrCustos), (nuvem, okrContinuidade),
                         (erp, okrContinuidade), (faturamento, okrCustos), (parque, okrContinuidade), (contratos, okrCustos),
                         (fornecedores, okrCustos), (seguranca, okrContinuidade), (dataLake, okrCustos),
                     })
            {
                db.ProjectObjectives.Add(new ProjectObjective { ProjectId = projeto.Id, ObjectiveId = okr.Id });
            }

            // Lançamentos para todos os projetos (Chatbot estoura o orçamento).
            foreach (var (projeto, valores) in new (Project, decimal[])[]
                     {
                         (portal, new[] { 45_000m, 30_000m }), (app, new[] { 60_000m, 25_000m }), (nuvem, new[] { 90_000m, 40_000m }),
                         (chatbot, new[] { 50_000m, 32_000m }), (erp, new[] { 35_000m, 20_000m }), (bi, new[] { 3_000m }),
                         (faturamento, new[] { 40_000m, 15_000m }), (parque, new[] { 70_000m }), (contratos, new[] { 55_000m, 20_000m }),
                         (fornecedores, new[] { 5_000m }), (seguranca, new[] { 30_000m, 12_000m }), (dataLake, new[] { 2_000m }),
                     })
            {
                for (var i = 0; i < valores.Length; i++)
                {
                    db.BudgetExpenses.Add(new BudgetExpense
                    {
                        ProjectId = projeto.Id, Amount = valores[i], Category = i == 0 ? BudgetExpenseCategory.Custo : BudgetExpenseCategory.Despesa,
                        Description = i == 0 ? "Serviços e licenças" : "Infraestrutura e viagens",
                        Date = hoje.AddDays(-15 - 20 * i),
                    });
                }
            }

            // Business case para 5 projetos e retornos realizados para 4.
            foreach (var (projeto, investimento, fluxo, meses) in new[]
                     {
                         (portal, 400_000m, 45_000m, 18), (app, 300_000m, 40_000m, 12), (erp, 150_000m, 20_000m, 12),
                         (faturamento, 200_000m, 30_000m, 12), (fornecedores, 220_000m, 25_000m, 18),
                     })
            {
                var bc = new BusinessCase { ProjectId = projeto.Id, InitialInvestment = investimento, AnnualDiscountRate = 12 };
                db.BusinessCases.Add(bc);
                for (var mes = 1; mes <= meses; mes++)
                {
                    db.CashFlowForecasts.Add(new CashFlowForecast { BusinessCaseId = bc.Id, Month = mes, Value = fluxo });
                }
            }
            foreach (var (projeto, valor) in new[] { (portal, 38_000m), (app, 15_000m), (erp, 22_000m), (faturamento, 31_000m) })
            {
                for (var mes = 1; mes <= 3; mes++)
                {
                    db.RealizedReturns.Add(new RealizedReturn
                    {
                        ProjectId = projeto.Id, Date = projeto.StartDate.AddMonths(mes), Value = valor, Description = $"Retorno do mês {mes}",
                    });
                }
            }

            // Dependências (Portal depende do App Mobile, que está em risco).
            db.ProjectDependencies.AddRange(
                new ProjectDependency { PortfolioId = p1.Id, ProjectId = portal.Id, DependsOnProjectId = app.Id, UserId = u["GerenteProjeto"].Id, Reason = "Login único do app é usado pelo portal." },
                new ProjectDependency { PortfolioId = p1.Id, ProjectId = bi.Id, DependsOnProjectId = portal.Id, UserId = u["GerenteProjeto"].Id, Reason = "Dados de uso do portal." },
                new ProjectDependency { PortfolioId = p2.Id, ProjectId = fornecedores.Id, DependsOnProjectId = faturamento.Id, UserId = u["ProductOwner"].Id, Reason = "Integração com o faturamento." });

            // 2 equipes com 3 membros; a Squad Digital fica acima de 100% de ocupação.
            var squadDigital = new Team { Name = "Squad Digital", PortfolioId = p1.Id, OwnerUserId = u["TechLead"].Id };
            var squadOperacoes = new Team { Name = "Squad Operações", PortfolioId = p2.Id, OwnerUserId = u["TechLead"].Id };
            TeamUser Membro(Team team, string nome, string email, decimal custo, int capacidade, User? user = null) =>
                new() { TeamId = team.Id, Name = nome, Email = email, HourlyCost = custo, MonthlyCapacityHours = capacidade, UserId = user?.Id };
            var membrosDigital = new[]
            {
                Membro(squadDigital, u["Desenvolvedor"].Name, u["Desenvolvedor"].Email, 120, 160, u["Desenvolvedor"]),
                Membro(squadDigital, u["QA"].Name, u["QA"].Email, 100, 160, u["QA"]),
                Membro(squadDigital, "Carla Souza", "carla.souza@prumo.dev", 130, 80),
            };
            var membrosOperacoes = new[]
            {
                Membro(squadOperacoes, u["TechLead"].Name, u["TechLead"].Email, 150, 160, u["TechLead"]),
                Membro(squadOperacoes, "Bruno Lima", "bruno.lima@prumo.dev", 110, 160),
                Membro(squadOperacoes, "Diego Rocha", "diego.rocha@prumo.dev", 90, 160),
            };
            db.Teams.AddRange(squadDigital, squadOperacoes);
            db.TeamUsers.AddRange(membrosDigital.Concat(membrosOperacoes));

            // Issues e worklogs de exemplo (Origem = "Seed") dos 4 tipos.
            var seq = 0;
            void Issues(Project projeto, TeamUser[] membros, decimal estimativaAberta, bool muitasAtrasadas)
            {
                var chave = new string(projeto.Name.Where(char.IsLetter).Take(4).ToArray()).ToUpperInvariant();
                var modelos = new (ExternalIssueType Tipo, bool Concluida, int Criada, int? Concluida_, int? Prazo)[]
                {
                    (ExternalIssueType.Story, true, -80, -70, -65),
                    (ExternalIssueType.Story, true, -60, -45, -40),
                    (ExternalIssueType.Task, true, -50, -47, -45),
                    (ExternalIssueType.Feature, true, -70, -20, -15),
                    (ExternalIssueType.Bug, true, -30, -28, -25),
                    (ExternalIssueType.Task, false, -40, null, muitasAtrasadas ? -5 : -3),
                    (ExternalIssueType.Story, false, -20, null, muitasAtrasadas ? -2 : 20),
                    (ExternalIssueType.Bug, false, -10, null, muitasAtrasadas ? -1 : 15),
                };

                for (var i = 0; i < modelos.Length; i++)
                {
                    var m = modelos[i];
                    var membro = membros[i % membros.Length];
                    var issue = new ExternalIssue
                    {
                        ProjectId = projeto.Id, ExternalId = $"SEED-{chave}-{++seq}", Source = "Seed",
                        Title = $"{m.Tipo} {i + 1} — {projeto.Name}", Type = m.Tipo,
                        Status = m.Concluida ? "Concluído" : "Em andamento", Done = m.Concluida,
                        CreatedAt = Dia(m.Criada), CompletedAt = m.Concluida_.HasValue ? Dia(m.Concluida_.Value) : null,
                        DueDate = m.Prazo.HasValue ? hoje.AddDays(m.Prazo.Value) : null, AssigneeEmail = membro.Email,
                        EstimateHours = m.Concluida ? 16 : estimativaAberta, UpdatedAt = agora,
                    };
                    var horas = m.Concluida ? new[] { 8m, 6m } : new[] { 4m };
                    for (var w = 0; w < horas.Length; w++)
                    {
                        var data = m.Concluida ? hoje.AddDays(m.Concluida_!.Value - w) : hoje.AddDays(-Math.Min(hoje.Day - 1, 2));
                        issue.Worklogs.Add(new ExternalWorklog
                        {
                            IssueId = issue.Id, ExternalId = $"SEED-W-{seq}-{w}", AuthorEmail = membro.Email, Hours = horas[w], Date = data,
                        });
                    }
                    issue.SpentHours = horas.Sum();
                    db.Issues.Add(issue);
                }
            }

            Issues(portal, membrosDigital, 40, false);
            Issues(app, membrosDigital, 40, true);
            Issues(nuvem, membrosDigital, 40, false);
            Issues(chatbot, membrosDigital, 40, false);
            Issues(erp, membrosDigital, 40, false);
            Issues(faturamento, membrosOperacoes, 60, false);
            Issues(seguranca, membrosOperacoes, 60, false);

            await db.SaveChangesAsync();
        }
    }
}
