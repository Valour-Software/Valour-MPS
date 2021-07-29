using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Valour.MPS.API;
using Valour.MPS.Config;
using System.Text.Json;
using System.IO;
using Valour.MPS.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Net.Http;

namespace Valour_Media_Proxy_Server
{
    public class Program
    {
        public const string ConfigPath = "Config/vmps_config.json";

        public static void Main(string[] args)
        {
            // Get configuration
            GetConfig();

            var builder = WebApplication.CreateBuilder(args);

            // Set up services
            ConfigureServices(builder);

            // Build web app
            var app = builder.Build();

            // Set up app
            ConfigureApp(app);

            // Add API routes
            ProxyAPI.AddRoutes(app);

            // Run app
            app.Run();
        }

        public static void GetConfig()
        {
            VMPS_Config config = null;

            if (File.Exists(ConfigPath))
            {
                string cf_data = File.ReadAllText(ConfigPath);
                config = JsonSerializer.Deserialize<VMPS_Config>(cf_data);
            }
            // If no config exists, create an empty one
            else
            {
                config = new VMPS_Config()
                {
                    Authorization_Key = "key",
                    Database_Address = "localhost",
                    Database_User = "dbuser",
                    Database_Password = "dbpass"
                };

                string cf_data = JsonSerializer.Serialize(config);
                File.WriteAllText(ConfigPath, cf_data);
            }
        }

        public static void ConfigureApp(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Valour_Media_Proxy_Server v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public static void ConfigureServices(WebApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddSingleton<HttpClient>();

            services.AddDbContextPool<MediaDB>(options =>
            {
                options.UseMySql(MediaDB.ConnectionString, ServerVersion.Parse("8.0.20-mysql"), options => options.EnableRetryOnFailure());
            });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            }
            );

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Valour_Media_Proxy_Server", Version = "v1" });
            });
            services.AddMemoryCache();
            services.AddResponseCaching();
        }
    }
}
