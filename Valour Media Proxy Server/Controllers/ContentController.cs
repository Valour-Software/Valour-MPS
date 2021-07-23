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

        public async Task<byte[]> GetImageBytes(string id, string type)
        {
            byte[] bytes;

            if (!_memoryCache.TryGetValue(id, out bytes))
            {
                string path = "../Content/" + type + "/" + id;

                if (!File.Exists(path))
                {
                    return null;
                }

                bytes = await File.ReadAllBytesAsync(path);

                _memoryCache.Set(id, bytes);
            }

            return bytes;
        }

        [HttpGet]
        public async Task<IActionResult> ProfileImage(string id)
        {
            byte[] bytes = await GetImageBytes(id, "Image");

            if (bytes == null)
            {
                return new NotFoundResult();
            }

            return new FileContentResult(bytes, "image/jpeg");
        }

        [HttpGet]
        public async Task<IActionResult> Image(string id)
        {
            byte[] bytes = await GetImageBytes(id, "ProfileImage");

            if (bytes == null)
            {
                return new NotFoundResult();
            }

            return new FileContentResult(bytes, "image/jpeg");
        }
    }
}
