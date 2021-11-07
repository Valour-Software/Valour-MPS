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
            app.MapGet("/content/file/{id}", FileRoute);
            app.MapGet("/content/profileimage/{id}", ProfileImageRoute);
            app.MapGet("/content/planetimage/{id}", PlanetImageRoute);
            app.MapGet("/content/image/{id}", ImageRoute);
        }

        /// <summary>
        /// The File route returns the file associated with the given id.
        /// 
        /// Type:
        /// GET
        /// 
        /// Route:
        /// /content/file/{id}
        /// 
        /// </summary>
        private static async Task FileRoute(IMemoryCache cache, HttpContext context, MediaDB db)
        {
            if (!context.Request.RouteValues.TryGetValue("id", out var id))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing id parameter");
                return;
            }

            byte[] bytes;
            string meta = "";

            // Get cached file
            if (!cache.TryGetValue(id, out bytes))
            {
                string path = "../Content/File/" + id;

                if (!File.Exists(path))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("File could not be found.");
                    return;
                }

                bytes = await File.ReadAllBytesAsync(path);

                cache.Set(id, bytes);
            }

            // Get cached meta
            if (!cache.TryGetValue(id + "-meta", out meta))
            {
                string path = "../Content/File/" + id + ".meta";

                if (!File.Exists(path))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("File meta could not be found.");
                    return;
                }

                meta = await File.ReadAllTextAsync(path);

                cache.Set(id + "-meta", bytes);
            }

            context.Response.ContentType = meta;
            await context.Response.BodyWriter.WriteAsync(bytes);

        }

        /// <summary>
        /// The ProfileImage route returns the profile image associated with the given id.
        /// 
        /// Type:
        /// GET
        /// 
        /// Route:
        /// /content/profileimage/{id}
        /// 
        /// </summary>
        private static async Task ProfileImageRoute(IMemoryCache cache, HttpContext context, MediaDB db)
        {
            if (!context.Request.RouteValues.TryGetValue("id", out var id))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing id parameter");
                return;
            }

            byte[] bytes = await GetImageBytes(cache, (string)id, "ProfileImage");

            if (bytes == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Could not find profile image");
                return;
            }

            context.Response.ContentType = "image/jpeg";
            await context.Response.BodyWriter.WriteAsync(bytes);
        }

        /// <summary>
        /// The PlanetImage route returns the profile image associated with the given id.
        /// 
        /// Type:
        /// GET
        /// 
        /// Route:
        /// /content/planetimage/{id}
        /// 
        /// </summary>
        private static async Task PlanetImageRoute(IMemoryCache cache, HttpContext context, MediaDB db)
        {
            if (!context.Request.RouteValues.TryGetValue("id", out var id))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing id parameter");
                return;
            }

            byte[] bytes = await GetImageBytes(cache, (string)id, "PlanetImage");

            if (bytes == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Could not find planet image");
                return;
            }

            context.Response.ContentType = "image/jpeg";
            await context.Response.BodyWriter.WriteAsync(bytes);
        }

        /// <summary>
        /// The Image route returns the image associated with the given id.
        /// 
        /// Type:
        /// GET
        /// 
        /// Route:
        /// /content/image/{id}
        /// 
        /// </summary>
        private static async Task ImageRoute(IMemoryCache cache, HttpContext context, MediaDB db){
            if (!context.Request.RouteValues.TryGetValue("id", out var id))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing id parameter");
                    return;
                }

                byte[] bytes = await GetImageBytes(cache, (string)id, "Image");

                if (bytes == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Could not find image");
                    return;
                }

                context.Response.ContentType = "image/jpeg";
                await context.Response.BodyWriter.WriteAsync(bytes);
        }

        ////////////////////
        // Helper methods //
        ////////////////////

        private static async Task<byte[]> GetImageBytes(IMemoryCache cache, string id, string type)
        {
            byte[] bytes;

            if (!cache.TryGetValue(id, out bytes))
            {
                string path = "../Content/" + type + "/" + id;

                if (!File.Exists(path))
                {
                    return null;
                }

                bytes = await File.ReadAllBytesAsync(path);

                cache.Set(id, bytes);
            }

            return bytes;
        }
    }
}
