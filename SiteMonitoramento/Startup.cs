using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using HealthChecks.UI.Client;

namespace SiteMonitoramento
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configurando a verificação de disponibilidade de diferentes
            // serviços através de Health Checks
            services.AddHealthChecks()
                .AddKafka(new ProducerConfig()
                {
                    BootstrapServers = Configuration.GetConnectionString("Kafka")
                }, name: "kafka", tags: new string[] { "messaging" })
                .AddRabbitMQ(Configuration.GetConnectionString("RabbitMQ"),
                    name: "rabbitmq", tags: new string[] { "messaging" })
                .AddSqlServer(Configuration.GetConnectionString("SqlServer"),
                    name: "sqlserver", tags: new string[] { "db", "data" })
                .AddMongoDb(Configuration.GetConnectionString("MongoDB"),
                    name: "mongodb", tags: new string[] { "db", "data" });

            services.AddHealthChecksUI()
                .AddInMemoryStorage();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Gera o endpoint que retornará os dados utilizados no dashboard
            app.UseHealthChecks("/healthchecks-data-ui", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // Ativa o dashboard para a visualização da situação de cada Health Check
            app.UseHealthChecksUI(options =>
            {
                options.UIPath = "/monitor";
            });
        }
    }
}