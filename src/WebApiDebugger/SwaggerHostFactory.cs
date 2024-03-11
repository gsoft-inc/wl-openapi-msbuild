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
            // Mandatory for Swagger to discover the endpoints (leave as is)
            services.AddControllers();
            
            /// Should have the same configuration as the main program (document name, filter,...)
            /// Ideally extract the configuration logic in a extension method (i.e: AddSwagger)
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
