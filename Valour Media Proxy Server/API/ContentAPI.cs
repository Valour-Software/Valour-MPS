using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Valour.MPS.Database;

namespace Valour.MPS.API
{
    public class ContentAPI
    {

        public static void AddRoutes(WebApplication app)
        {
            app.MapGet("/content/{user_id}/{type}/{id}", GetRoute);
        }

        private static async Task GetRoute(IMemoryCache cache, HttpContext context, MediaDB db,
             string type, string id, ulong user_id)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing type");
                return;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing id");
                return;
            }

            if (user_id == 0){
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing user id");
                return;
            }

            type = type.ToLower();

            byte[] bytes = await GetBytes(cache, (string)id, type, user_id);

            if (bytes == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Could not find " + type);
                return;
            }

            string[] meta = await GetMeta(cache, (string)id, type);

            if (meta == null || string.IsNullOrWhiteSpace(meta[0]))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Could not find metadata");
                return;
            }

            string ext = "";
            if (meta.Length > 1)
                ext = meta[1];

            context.Response.ContentType = meta[0];
            await context.Response.BodyWriter.WriteAsync(bytes);
            
        }

        ////////////////////
        // Helper methods //
        ////////////////////

        private static async Task<string[]> GetMeta(IMemoryCache cache, string id, string type)
        {
            string[] meta;

            if (!cache.TryGetValue(id + "-meta", out meta))
            {
                string path = "../Content/" + type + "/" + id + ".meta";

                if (!File.Exists(path))
                {
                    return null;
                }

                meta = await File.ReadAllLinesAsync(path);

                cache.Set(id + "-meta", meta);
            }

            return meta;
        }

        private static async Task<byte[]> GetBytes(IMemoryCache cache, string id, string type, ulong user_id)
        {
            byte[] bytes;

            if (!cache.TryGetValue(id, out bytes))
            {
                // Check for user file
                string user_path = $"../Content/users/{user_id}/{type}/{id}";

                if (!File.Exists(user_path))
                {
                    return null;
                }

                // Get root file
                var root_path = $"../Content/{type}/{id}";

                if (!File.Exists(root_path))
                {
                    return null;
                }

                bytes = await File.ReadAllBytesAsync(root_path);

                cache.Set(id, bytes);
            }

            return bytes;
        }
    }
}
