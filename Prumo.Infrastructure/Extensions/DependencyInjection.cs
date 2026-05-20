using Microsoft.Extensions.DependencyInjection;

namespace Prumo.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            //services.AddScoped<IPlantaoRepository, PlantaoRepository>();
            //services.AddScoped<IMedicoRepository, MedicoRepository>();
            //services.AddScoped<IHospitalRepository, HospitalRepository>();
            //services.AddScoped<IAtendimentoRepository, AtendimentoRepository>();
            //services.AddScoped<IAtendenteRepository, AtendenteRepository>();

            return services;
        }
    }
}
