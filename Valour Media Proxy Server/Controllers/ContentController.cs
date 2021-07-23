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

        private async Task<byte[]> GetImageBytes(string id, string type)
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
        public async Task<IActionResult> GetFile(string id)
        {
            byte[] bytes;
            string meta = "";

            // Get cached file
            if (!_memoryCache.TryGetValue(id, out bytes))
            {
                string path = "../Content/File/" + id;

                if (!File.Exists(path))
                {
                    return null;
                }

                bytes = await File.ReadAllBytesAsync(path);

                _memoryCache.Set(id, bytes);
            }

            // Get cached meta
            if (!_memoryCache.TryGetValue(id + "-meta", out meta))
            {
                string path = "../Content/File/" + id + ".meta";

                if (!File.Exists(path))
                {
                    return null;
                }

                meta = await File.ReadAllTextAsync(path);

                _memoryCache.Set(id + "-meta", bytes);
            }

            return new FileContentResult(bytes, meta);
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
