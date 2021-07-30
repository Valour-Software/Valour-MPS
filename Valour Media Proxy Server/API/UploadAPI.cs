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
using System.Drawing;
using Valour.MPS.Extensions;

namespace Valour.MPS.API
{
    public static class UploadAPI
    {
        static SHA256 SHA256 = SHA256.Create();

        public static void AddRoutes(WebApplication app)
        {
            FileRoute(app);
            ImageRoute(app);
            ProfileImageRoute(app);
        }

        /// <summary>
        /// The File route allows the upload of general files
        /// 
        /// Type:
        /// POST
        /// 
        /// Route:
        /// /upload/file
        /// 
        /// Query parameters:
        /// auth: Authentication key
        /// 
        /// Form data:
        /// General files
        /// 
        /// </summary>
        private static void FileRoute(WebApplication app)
        {
            app.MapPost("/upload/file", (async (HttpContext context) =>
            {
                Console.WriteLine("File upload.");

                // Max file size is 10mb
                if (context.Request.ContentLength > 10240000)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Max file size is 10mb");
                    return;
                }

                if (!context.Request.Query.TryGetValue("auth", out var auth))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized. Include auth.");
                    return;
                }
                else if (auth != VMPS_Config.Current.Authorization_Key)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }

                if (ImageContent.Contains(context.Request.ContentType))
                {
                    context.Response.StatusCode = 415;
                    await context.Response.WriteAsync("Use /upload/image for images");
                    return;
                }

                int fileCount = context.Request.Form.Files.Count;

                if (fileCount == 0)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Please attach a file");
                    return;
                }

                if (fileCount > 5)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Please attach less than 5 files");
                    return;
                }

                long totalSize = 0;

                string[] uploads = new string[fileCount];

                var files = context.Request.Form.Files;

                // Safety checks
                foreach (var file in files)
                {
                    totalSize += file.Length;

                    if (totalSize > 10240000)
                    {
                        context.Response.StatusCode = 413;
                        await context.Response.WriteAsync("Max total size is 10mb");
                        return;
                    }

                    if (ImageContent.Contains(file.ContentType))
                    {
                        context.Response.StatusCode = 415;
                        await context.Response.WriteAsync("Use /upload/image for images");
                        return;
                    }
                }

                int i = 0;

                foreach (var file in files)
                {
                    string location = await StorageManager.SaveContent(file, "File");

                    uploads[i] = location;

                    i++;
                }

                await uploads.SerializeJsonAsync(context.Response.BodyWriter.AsStream());
            }));
        }

        /// <summary>
        /// The Image route allows the upload of general images
        /// 
        /// Type:
        /// POST
        /// 
        /// Route:
        /// /upload/image
        /// 
        /// Query parameters:
        /// auth: Authentication key
        /// 
        /// Form data:
        /// Images
        /// 
        /// </summary>
        private static void ImageRoute(WebApplication app)
        {
            app.MapPost("/upload/image", (async (HttpContext context) =>
            {
                Console.WriteLine("Image upload.");

                // Max file size is 10mb
                if (context.Request.ContentLength > 10240000)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Max file size is 10mb");
                    return;
                }

                if (!context.Request.Query.TryGetValue("auth", out var auth))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized. Include auth.");
                    return;
                }
                else if (auth != VMPS_Config.Current.Authorization_Key)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }

                int fileCount = context.Request.Form.Files.Count;

                if (fileCount == 0)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Please attach an image");
                    return;
                }

                if (fileCount > 5)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Please attach less than 5 images");
                    return;
                }

                long totalSize = 0;

                string[] uploads = new string[fileCount];
                Bitmap[] bitmaps = new Bitmap[fileCount];

                int i = 0;

                var files = context.Request.Form.Files;

                // Safety checks
                foreach (var file in files)
                {
                    totalSize += file.Length;

                    if (totalSize > 10240000)
                    {
                        context.Response.StatusCode = 413;
                        await context.Response.WriteAsync("Max total size is 10mb");
                        return;
                    }

                    if (!ImageContent.Contains(file.ContentType))
                    {
                        context.Response.StatusCode = 415;
                        await context.Response.WriteAsync("Please only upload images");
                        return;
                    }

                    var bitmap = file.TryGetImage();

                    if (bitmap == null)
                    {
                        context.Response.StatusCode = 415;
                        await context.Response.WriteAsync("Image malformed");
                        return;
                    }

                    // Needs to be copied to prevent breakage
                    bitmaps[i] = new Bitmap(bitmap);
                }

                i = 0;

                foreach (var file in files)
                {
                    uploads[i] = await StorageManager.SaveImage(bitmaps[i], "Image");
                    i++;
                }

                await uploads.SerializeJsonAsync(context.Response.BodyWriter.AsStream());
            }));
        }

        /// <summary>
        /// The ProfileImage route allows the upload of profile images (pfps)
        /// 
        /// Type:
        /// POST
        /// 
        /// Route:
        /// /upload/profileimage
        /// 
        /// Query parameters:
        /// auth: Authentication key
        /// 
        /// Form data:
        /// Image
        /// 
        /// </summary>
        private static void ProfileImageRoute(WebApplication app)
        {
            app.MapPost("/upload/profileimage", (async (HttpContext context) =>
            {
                Console.WriteLine("Profile image upload.");

                // Max file size is 2mb
                if (context.Request.ContentLength > 2621440)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Max file size is 2mb");
                    return;
                }

                if (!context.Request.Query.TryGetValue("auth", out var auth))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized. Include auth.");
                    return;
                }
                else if (auth != VMPS_Config.Current.Authorization_Key)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized.");
                    return;
                }

                int fileCount = context.Request.Form.Files.Count;

                Console.WriteLine("Count: " + fileCount);

                if (fileCount == 0)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Please attach an image");
                    return;
                }

                if (fileCount > 1)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Please attach one image only");
                    return;
                }

                var file = context.Request.Form.Files[0];

                if (file.Length > 2621440)
                {
                    context.Response.StatusCode = 413;
                    await context.Response.WriteAsync("Max total size is 2mb");
                    return;
                }

                var bitmap = file.TryGetImage();

                if (bitmap == null)
                {
                    context.Response.StatusCode = 415;
                    await context.Response.WriteAsync("Image malformed");
                    return;
                }

                // Resize image to pfp scale
                Bitmap formattedImage = await ImageUtility.ConvertToProfileImage(bitmap);

                string location = await StorageManager.SaveImage(formattedImage, "ProfileImage");

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(location);
            }));
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
