using Microsoft.AspNetCore.Hosting;
using DotNetEnv; // This will work after installing the package

namespace Prumo.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Env.Load(); 
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