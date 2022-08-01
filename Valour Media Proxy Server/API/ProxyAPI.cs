using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Valour.MPS.Database;
using Valour.MPS.Proxy;
using System.Net.Http;
using Valour.MPS.Config;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Valour.MPS.API
{
    public static class ProxyAPI
    {
        static SHA256 SHA256 = SHA256.Create();

        public static void AddRoutes(WebApplication app)
        {
            ProxyRoute(app);
            SendUrlRoute(app);
        }

        /// <summary>
        /// The Proxy route proxies the page that corresponds with the given hash.
        /// 
        /// Type:
        /// GET
        /// 
        /// Route:
        /// /proxy/{url}
        /// 
        /// Query Params:
        /// auth: The authentication key
        /// 
        /// </summary>
        private static void ProxyRoute(WebApplication app)
        {
            app.MapGet("/proxy/{url}", (async (HttpContext context, HttpClient client, MediaDb db) =>

            {
                if (!context.Request.RouteValues.TryGetValue("url", out var url))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing url parameter");
                    return;
                }

                ProxyItem item = await db.ProxyItems.FindAsync(url);

                if (item != null)
                {
                    await (await client.GetStreamAsync(item.Origin)).CopyToAsync(context.Response.BodyWriter.AsStream());
                    return;
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Not found.");
                    return;
                }
            })
            );
        }

        /// <summary>
        /// The Proxy/SendUrl route allows a client to send a url and be returned a proxy item
        /// which can then be used to proxy the url in the future
        /// 
        /// Type:
        /// POST
        /// 
        /// Route:
        /// /Proxy/SendUrl
        /// 
        /// Query Params:
        /// url: The url to create proxy data for 
        /// auth: The authentication key
        /// 
        /// </summary>
        private static void SendUrlRoute(WebApplication app)
        {
            app.MapPost("/proxy/sendurl", (async (HttpContext context, HttpClient client, MediaDb db) =>

            {
                if (!context.Request.Query.TryGetValue("url", out var url_in))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing url parameter");
                    return;
                }

                if (!context.Request.Query.TryGetValue("auth", out var auth_in))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing auth parameter");
                    return;
                }

                string url = (string)url_in;
                string auth = (string)auth_in;

                if (auth != VmpsConfig.Current.AuthKey)
                {
                    Console.WriteLine("Failed authorization:");
                    Console.WriteLine(auth);

                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }

                byte[] h = SHA256.ComputeHash(Encoding.UTF8.GetBytes(url));
                string hash = BitConverter.ToString(h).Replace("-", "").ToLower();

                ProxyItem item = await db.ProxyItems.FindAsync(hash);

                ProxyResponse result = new ProxyResponse();

                if (item == null)
                {
                    // Check if end resource is media
                    var response = await client.GetAsync(url);

                    result.Status = (int)response.StatusCode;

                    // If failure, return the reason and stop
                    if (!response.IsSuccessStatusCode)
                    {
                        result.Error = response.ReasonPhrase;
                        await result.SerializeJsonAsync(context.Response.BodyWriter.AsStream());
                        return;
                    }

                    IEnumerable<string> content_types;

                    response.Content.Headers.TryGetValues("Content-Type", out content_types);

                    string content_type = content_types.FirstOrDefault().Split(';')[0];

                    item = new ProxyItem()
                    {
                        Id = hash,
                        Origin = url,
                        MimeType = content_type
                    };

                    await db.AddAsync(item);
                    await db.SaveChangesAsync();
                }
                else
                {
                    result.Status = 200;
                }

                result.Item = item;

                await result.SerializeJsonAsync(context.Response.BodyWriter.AsStream());
            })
            );
        }
    }
}
