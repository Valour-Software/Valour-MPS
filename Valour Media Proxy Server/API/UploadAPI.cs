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
using Valour.MPS.Storage;
using Valour.MPS.Images;
using Valour.MPS.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;

namespace Valour.MPS.API
{
    public class UploadAPI
    {
        static SHA256 SHA256 = SHA256.Create();

        public static HashSet<ExifTag> AllowedExif = new HashSet<ExifTag>()
        {
            ExifTag.ImageWidth,
            ExifTag.ImageDescription,
            ExifTag.ImageLength,

            ExifTag.Orientation,
            ExifTag.DateTime
        };

        public static void AddRoutes(WebApplication app)
        {
            app.MapPost("/upload/{user_id}/{type}", UploadRoute);
        }

        public static void HandleExif(Image image)
        {
            // Remove unneeded exif data
            if (image.Metadata != null && image.Metadata.ExifProfile != null)
            {
                List<ExifTag> toRemove = new List<ExifTag>();

                var exifs = image.Metadata.ExifProfile.Values;

                foreach (var exif in exifs)
                {
                    if (!AllowedExif.Contains(exif.Tag))
                        toRemove.Add(exif.Tag);
                }

                foreach (var tag in toRemove)
                    image.Metadata.ExifProfile.RemoveValue(tag);
            }
        }

        [FileUploadOperation.FileContentType]
        private static async Task UploadRoute(HttpContext context, string auth, string type, ulong user_id)
        {
            Console.WriteLine(type + " upload");

            long max_length = 10240000;
            bool should_be_image = false;

            // Get max length
            switch (type)
            {
                case "file":
                    max_length = 10240000;
                    break;
                case "image":
                    max_length = 10240000;
                    should_be_image = true;
                    break;
                case "profile":
                    max_length = 2621440;
                    should_be_image = true;
                    break;
                case "planet":
                    max_length = 8388608;
                    should_be_image = true;
                    break;
                case "app":
                    max_length = 10240000;
                    should_be_image = true;
                    break;
                default:
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Unknown type " + type);
                    return;
            }

            if (context.Request.ContentLength > max_length)
            {
                context.Response.StatusCode = 413;
                await context.Response.WriteAsync("Max file size is " + max_length + " bytes");
                return;
            }

            if (auth != VMPS_Config.Current.Authorization_Key)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            var file = context.Request.Form.Files.FirstOrDefault();

            if (file is null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Please attach a file");
                return;
            }

            string path = "";

            if (should_be_image)
            {
                if (!ImageContent.Contains(file.ContentType))
                {
                    context.Response.StatusCode = 415;
                    await context.Response.WriteAsync("Use /upload/file for non-images");
                    return;
                }

                var stream = file.OpenReadStream();

                var image_data = await Image.LoadWithFormatAsync(stream);

                var image = image_data.Image;

                if (image == null)
                {
                    context.Response.StatusCode = 415;
                    await context.Response.WriteAsync("Image malformed");
                    return;
                }

                HandleExif(image);

                // Extra image processing
                switch (type)
                {
                    case "profile":
                        image.Mutate(x => x.Resize(256, 256));
                        break;
                    case "planet":
                        image.Mutate(x => x.Resize(512, 512));
                        break;
                    case "app":
                        image.Mutate(x => x.Resize(512, 512));
                }

                // Save image to stream
                using (MemoryStream ms = new())
                {
                    string content_type = "";
                    string ext = "";

                    // Support transparency
                    if (image_data.Format is not PngFormat)
                    {
                        // No transparency
                        image.Save(ms, StorageManager.jpegEncoder);
                        content_type = "image/jpeg";
                        ext = ".jpg";
                    }
                    else
                    {
                        // Has transparency
                        image.Save(ms, StorageManager.pngEncoder);
                        content_type = "image/png";
                        ext = ".png";
                    }

                    // Save to disk
                    path = await StorageManager.Save(ms, content_type, ext, type, user_id);
                }
            }
            // Handle non-image files
            else
            {
                if (ImageContent.Contains(context.Request.ContentType))
                {
                    context.Response.StatusCode = 415;
                    await context.Response.WriteAsync("Use /upload/image for images");
                    return;
                }

                using (MemoryStream ms = new())
                {
                    await file.CopyToAsync(ms);

                    string ext = Path.GetExtension(file.FileName);

                    path = await StorageManager.Save(ms, file.ContentType, ext, type, user_id);
                }
            }

            await context.Response.WriteAsync(path);
        }

        ////////////////////
        // Helper methods //
        ////////////////////

        public static HashSet<string> ImageContent = new HashSet<string>()
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
    }
}
