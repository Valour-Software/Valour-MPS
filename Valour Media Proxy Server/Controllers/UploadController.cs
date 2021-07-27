using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Valour.MPS.Extensions;
using Valour.MPS.Images;
using Valour.MPS.Storage;

namespace Valour.MPS.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UploadController
    {
        [HttpPost]
        // 10MB max size for general pictures
        [RequestSizeLimit(10240000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10240000)]
        public async Task<IActionResult> SendImage(IFormFile file)
        {
            Bitmap sourceImage = file.TryGetImage();
            Bitmap destImage = new Bitmap(sourceImage);
            string location = await StorageManager.SaveImage(destImage, "Image");

            UploadResponse response = new UploadResponse()
            {
                Url = location,
                Success = true
            };

            return new JsonResult(response);
        }

        public HashSet<string> ImageContent = new HashSet<string>()
        {
            "image/gif",
            "image/jpeg",
            "image/png",
            "image/tiff",
            "image/vnd.microsoft.icon",
            "image/x-icon",
            "image/vnd.djvu",
            "image/svg+xml"
        };

        [HttpPost]
        // 10MB max size for general media
        [RequestSizeLimit(10240000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10240000)]
        public async Task<IActionResult> SendFile(IFormFile file)
        {
            // Images should be sent thru /SendImage
            if (ImageContent.Contains(file.ContentType))
            {
                return new UnsupportedMediaTypeResult();
            }

            string location = await StorageManager.SaveContent(file, "File");

            UploadResponse response = new UploadResponse()
            {
                Url = location,
                Success = true
            };

            return new JsonResult(response);
        }

        [HttpPost]
        // 2MB max size for profile pictures
        [RequestSizeLimit(2621440)]
        [RequestFormLimits(MultipartBodyLengthLimit = 2621440)]
        public async Task<IActionResult> SendProfileImage(IFormFile file)
        {
            Bitmap sourceImage = file.TryGetImage();

            // If the file is not acceptable
            if (sourceImage == null)
            {
                return new UnsupportedMediaTypeResult();
            }

            // Resize image to pfp scale
            Bitmap formattedImage = await ImageUtility.ConvertToProfileImage(sourceImage);

            string location = await StorageManager.SaveImage(formattedImage, "ProfileImage");

            //var result = new FileStreamResult(stream, "image/jpeg")
            //{
            //    FileDownloadName = file.FileName
            //};

            UploadResponse response = new UploadResponse()
            {
                Url = location,
                Success = true
            };

            return new JsonResult(response);
        }
    }
}
