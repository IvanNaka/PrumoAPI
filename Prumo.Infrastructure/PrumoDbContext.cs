using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders; 
using Microsoft.Extensions.Configuration;
using Prumo.Domain.Entities;


using Prumo.Infrastructure.Configurations; 

namespace Plantonize.Plantao.Infrastructure
{
    public class PrumoDbContext : DbContext
    {
        public PrumoDbContext(DbContextOptions<PrumoDbContext> options, IConfiguration configuration) : base(options){}
        //public DbSet<Medico> Medicos { get; set; }
        //public DbSet<Hospital> Hospitais { get; set; }
        //public DbSet<Domain.Entities.Plantao> Plantoes { get; set; }
        //public DbSet<Atendimento> Atendimentos { get; set; }
        //public DbSet<Atendente> Atendentes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IConfigurationScan).Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Todos os enums são gravados como texto (Seção 3.1 do plano de conformidade).
            configurationBuilder.Properties<Enum>().HaveConversion<string>();
        }
    }
}
