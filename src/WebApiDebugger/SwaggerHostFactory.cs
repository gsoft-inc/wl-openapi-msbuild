using System.Diagnostics.CodeAnalysis;

namespace WebApiDebugger;

public static class SwaggerHostFactory
{
    public static IHost CreateHost()
    {
        return Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(UseSwaggerHostStartup)
            .Build();
    }

    private static void UseSwaggerHostStartup(IWebHostBuilder builder)
    {
        builder.UseStartup<SwaggerHostStartup>();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a startup class")]
    private sealed class SwaggerHostStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
