using Microsoft.AspNetCore.Hosting;
using DotNetEnv; // This will work after installing the package

namespace Prumo.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Env.Load();

            // Relatórios em PDF (T22): licença Community do QuestPDF.
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<Startup>();
                 });
    }
}