using AspNetCore.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Valour.MPS.Config;
using Valour.MPS.Database;

namespace Valour_Media_Proxy_Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public const string ConfigPath = "Config/vmps_config.json";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
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

            services.AddProxies();

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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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
    }
}
