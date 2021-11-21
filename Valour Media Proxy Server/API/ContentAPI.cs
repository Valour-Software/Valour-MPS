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
            int? rs = null;
            int? re = null;

            if (!string.IsNullOrWhiteSpace(Range))
            {
                range = true;

                if (Range.Length > 6)
                {

                    var inter = Range.Substring(6, Range.Length - 6);

                    string[] vals = inter.Split('-');

                    bool parsed = false;

                    // parse start
                    parsed = int.TryParse(vals[0], out int rs1);

                    if (parsed)
                        rs = rs1;

                    if (vals.Length > 1)
                    {
                        parsed = int.TryParse(vals[1], out int re1);

                        if (parsed)
                            re = re1;
                    }
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

            var res = await GetBytes(cache, (string)id, type, user_id, range, rs, re);

            byte[] bytes = res.data;
            int len = res.len;

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
                context.Response.Headers.Add("Content-Range", $"bytes {rs}-{re}/{len}");
                context.Response.Headers.Add("Content-Length", bytes.Length.ToString());
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

        private static async Task<(byte[] data, int len)> GetBytes(IMemoryCache cache, string id, string type, ulong user_id,
            // rs is range start, re is range end
            bool range = false, int? rs1 = null, int? re1 = null)
        {
            byte[] bytes = null;
            int len = 0;

            int rs = 0;
            int re = 0;

            if (rs1 != null)
                rs = (int)rs1;

            if (re1 != null)
                re = (int)re1;

            if (range)
            {
                if (!cache.TryGetValue(id + $"-range-{rs}-{re}", out bytes))
                {
                    bytes = new byte[re - rs];

                    // Check for user file
                    string user_path = $"../Content/users/{user_id}/{type}/{id}";

                    if (!File.Exists(user_path))
                    {
                        return (null, 0);
                    }

                    // Get root file
                    var root_path = $"../Content/{type}/{id}";

                    if (!File.Exists(root_path))
                    {
                        return (null, 0);
                    }

                    var stream = File.OpenRead(root_path);

                    len = (int)stream.Length;

                    if (re1 is null)
                        re = len - 1;

                    await stream.ReadAsync(bytes, rs, re - rs);

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
                        return (null, 0);
                    }

                    // Get root file
                    var root_path = $"../Content/{type}/{id}";

                    if (!File.Exists(root_path))
                    {
                        return (null, 0);
                    }

                    bytes = await File.ReadAllBytesAsync(root_path);
                    len = bytes.Length;
                    cache.Set(id, bytes);
                }
            }    

            return (bytes, len);
        }
    }
}
