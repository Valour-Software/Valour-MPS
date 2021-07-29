using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valour.MPS.Config;
using Valour.MPS.Database;
using Valour.MPS.Proxy;
using AspNetCore.Proxy;

namespace Valour.MPS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProxyController : Controller
    {

        private readonly MediaDB DB;

        static SHA256 SHA256 = SHA256.Create();
        static HttpClient HttpClient = new HttpClient();

        public ProxyController(MediaDB db)
        {
            this.DB = db;
        }

        [HttpGet]
        [Route("{url}")]
        public async Task Proxy(string url)
        {
            ProxyItem item = await DB.ProxyItems.FindAsync(url);

            if (item != null)
            {
                await this.HttpProxyAsync(item.Origin_Url);
            }
            else
            {
                Response.StatusCode = 404;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> SendUrl(string url, string auth)
        {
            if (auth != VMPS_Config.Current.Authorization_Key)
            {
                Console.WriteLine("Failed authorization:");
                Console.WriteLine(auth);
                return new UnauthorizedResult();
            }

            byte[] h = SHA256.ComputeHash(Encoding.UTF8.GetBytes(url));
            string hash = BitConverter.ToString(h).Replace("-", "").ToLower();

            ProxyItem item = await DB.ProxyItems.FindAsync(hash);

            ProxyResponse result = new ProxyResponse();

            if (item == null)
            {
                // Check if end resource is media
                var response = await HttpClient.GetAsync(url);

                result.Status = (int)response.StatusCode;

                // If failure, return the reason and stop
                if (!response.IsSuccessStatusCode)
                {
                    result.Error = response.ReasonPhrase;

                    return new JsonResult(result);
                }

                IEnumerable<string> content_types;

                response.Content.Headers.TryGetValues("Content-Type", out content_types);

                string content_type = content_types.FirstOrDefault().Split(';')[0];

                item = new ProxyItem()
                {
                    Id = hash,
                    Origin_Url = url,
                    Mime_Type = content_type
                };

                await DB.AddAsync(item);
                await DB.SaveChangesAsync();
            }
            else
            {
                result.Status = 200;
            }

            result.Item = item;

            return new JsonResult(result); 
        }
    }
}
