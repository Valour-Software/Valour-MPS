using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
             string type, string id, ulong user_id,
             [FromHeader] string Range)
        {
            bool range = false;
            int rs = 0;
            int re = 0;

            if (!string.IsNullOrWhiteSpace(Range))
            {
                range = true;

                if (Range.Length < 9)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Malformed range header");
                    return;
                }


                var inter = Range.Substring(0, 6);

                string[] vals = inter.Split('-');

                if (vals.Length < 2)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Malformed range header");
                    return;
                }

                bool parsed = false;

                // parse start
                parsed = int.TryParse(vals[0], out rs);

                if (!parsed)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Malformed range header");
                    return;
                }

                parsed = int.TryParse(vals[1], out re);

                if (!parsed)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Malformed range header");
                    return;
                }
            }


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

            byte[] bytes = await GetBytes(cache, (string)id, type, user_id, range, rs, re);

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

            // Handle partial requests
            if (range)
            {
                context.Response.StatusCode = 206;
            }


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

        private static async Task<byte[]> GetBytes(IMemoryCache cache, string id, string type, ulong user_id,
            // rs is range start, re is range end
            bool range = false, int rs = 0, int re = 0)
        {
            byte[] bytes = null;

            if (range)
            {
                if (!cache.TryGetValue(id + $"-range-{rs}-{re}", out bytes))
                {
                    bytes = new byte[re - rs];

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

                    var stream = File.OpenRead(root_path);

                    await stream.ReadAsync(bytes, rs, re - re);

                    cache.Set(id + $"-range-{rs}-{re}", bytes);
                }
            }
            else
            {
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
            }    

            return bytes;
        }
    }
}
