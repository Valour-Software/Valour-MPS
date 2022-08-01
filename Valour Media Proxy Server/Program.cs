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
using Microsoft.AspNetCore.Http.Features;
using Valour.MPS.Extensions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Amazon.S3;
using Amazon.Runtime;
using System.Threading.Tasks;
using Valour.MPS.Storage;
using Npgsql;

namespace Valour_Media_Proxy_Server
{
    public class Program
    {
        public const string ConfigPath = "Config/vmps_config.json";

        public static async Task Main(string[] args)
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
            ContentAPI.AddRoutes(app);
            UploadAPI.AddRoutes(app);

            BasicAWSCredentials cred = new(VmpsConfig.Current.S3Access, VmpsConfig.Current.S3Secret);
            AmazonS3Config config = new AmazonS3Config()
            {
                ServiceURL = VmpsConfig.Current.R2Endpoint
            };

            AmazonS3Client client = new(cred, config);
            BucketManager.Client = client;

            /*
            await client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest()
            {
                Key = "test-key",
                BucketName = "valourmps/egg",
                ContentBody = "Test",
                DisablePayloadSigning = true
            });
            */

            // Run app
            app.Run();
        }

        public static void GetConfig()
        {
            VmpsConfig config = null;

            if (File.Exists(ConfigPath))
            {
                string cf_data = File.ReadAllText(ConfigPath);
                config = JsonSerializer.Deserialize<VmpsConfig>(cf_data);
            }
            // If no config exists, create an empty one
            else
            {
                config = new VmpsConfig()
                {
                    AuthKey = "key",
                    DbAddr = "localhost",
                    DbUser = "dbuser",
                    DbPass = "dbpass",
                    S3Access = "s3access",
                    S3Secret = "s3secret",
                    R2Endpoint = "r2endpoint"
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
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VMPS v2"));
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

            services.AddHttpClient();

            services.Configure<FormOptions>(options =>
            {
                options.MemoryBufferThreshold = 10240000;
                options.MultipartBodyLengthLimit = 10240000;
            });

            services.AddDbContextPool<MediaDb>(options =>
            {
                options.UseNpgsql(MediaDb.ConnectionString);
            });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            });

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Valour MPS System", Version = "v1.1" });
                c.OperationFilter<FileUploadOperation>();
            });
            services.AddMemoryCache();
            services.AddResponseCaching();
        }
    }
}
