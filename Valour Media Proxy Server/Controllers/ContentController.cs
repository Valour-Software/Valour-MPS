using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Valour.MPS.Controllers
{
    [ApiController]
    [Route("[controller]/[action]/{id}")]
    public class ContentController
    {

        private IMemoryCache _memoryCache;

        public ContentController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<IActionResult> ProfileImage(string id)
        {
            byte[] bytes;

            if (!_memoryCache.TryGetValue(id, out bytes))
            {
                string path = "../Content/ProfileImage/" + id;

                if (!File.Exists(path))
                {
                    return new NotFoundResult();
                }

                bytes = await File.ReadAllBytesAsync(path);

                _memoryCache.Set(id, bytes);
            }

            return new FileContentResult(bytes, "image/jpeg");
        }
    }
}
