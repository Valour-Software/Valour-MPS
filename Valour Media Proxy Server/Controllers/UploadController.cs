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
        public async Task SendImage(IFormFile file)
        {

        }

        [HttpPost]
        public async Task SendMedia(IFormFile file)
        {

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

            string location = await StorageManager.SaveProfileImage(formattedImage);

            //var result = new FileStreamResult(stream, "image/jpeg")
            //{
            //    FileDownloadName = file.FileName
            //};

            UploadResponse response = new UploadResponse()
            {
                Location = location,
                Success = true
            };

            return new JsonResult(response);
        }
    }
}
