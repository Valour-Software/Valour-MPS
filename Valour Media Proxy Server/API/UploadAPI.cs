using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Valour.MPS.Config;
using Valour.MPS.Database;
using Valour.MPS.Extensions;
using Valour.MPS.Media;
using Valour.MPS.Storage;
using Valour.Shared;

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
            app.MapPost("/upload/{userId}/{category}", UploadRoute);
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
        [RequestSizeLimit(10240000)]
        private static async Task<IResult> UploadRoute(HttpContext context, string auth, ContentCategory category, long userId, MediaDb db)
        {
            Console.WriteLine(category + " upload");

            long max_length = 10240000;
            bool should_be_image = false;

            // Get max length 
            switch (category)
            {
                case ContentCategory.File:
                    max_length = 10240000;
                    break;
                case ContentCategory.Image:
                    max_length = 10240000;
                    should_be_image = true;
                    break;
                case ContentCategory.Profile:
                    max_length = 2621440;
                    should_be_image = true;
                    break;
                case ContentCategory.Planet:
                    max_length = 8388608;
                    should_be_image = true;
                    break;
                case ContentCategory.App:
                    max_length = 10240000;
                    should_be_image = true;
                    break;
                default:
                    return Results.BadRequest("Unknown type: " + category);
            }

            if (context.Request.ContentLength > max_length)
                return Results.BadRequest("Max file size is " + max_length + " bytes");


            if (auth != VmpsConfig.Current.AuthKey)
                return Results.Unauthorized();

            var file = context.Request.Form.Files.FirstOrDefault();

            if (file is null)
                return Results.BadRequest("Please attach a file");

            TaskResult result;

            if (should_be_image)
            {
                if (!ImageContent.Contains(file.ContentType))
                    return Results.BadRequest("Use /upload/file for non-images");

                var stream = file.OpenReadStream();

                var image_data = await Image.LoadWithFormatAsync(stream);

                var image = image_data.Image;

                if (image == null)
                    return Results.BadRequest("Image malformed"); 

                HandleExif(image);

                // Extra image processing
                switch (category)
                {
                    case ContentCategory.Profile:
                        image.Mutate(x => x.Resize(256, 256));
                        break;
                    case ContentCategory.Planet:
                        image.Mutate(x => x.Resize(512, 512));
                        break;
                    case ContentCategory.App:
                        image.Mutate(x => x.Resize(512, 512));
                        break;
                }

                // Save image to stream
                using MemoryStream ms = new();
                
                string contentType;
                string extension;

                // Support transparency
                if (image_data.Format is not PngFormat)
                {
                    // No transparency
                    image.Save(ms, StorageManager.jpegEncoder);
                    contentType = "image/jpeg";
                    extension = ".jpg";
                }
                else
                {
                    // Has transparency
                    image.Save(ms, StorageManager.pngEncoder);
                    contentType = "image/png";
                    extension = ".png";
                }

                result = await BucketManager.Upload(ms, extension, userId, contentType, category, db);

                // Save to disk
                //path = await StorageManager.Save(ms, content_type, ext, category, user_id);
                
            }
            // Handle non-image files
            else
            {
                if (ImageContent.Contains(context.Request.ContentType))
                    return Results.BadRequest("Use /upload/image for images");

                using MemoryStream ms = new();
                
                await file.CopyToAsync(ms);

                string ext = Path.GetExtension(file.FileName);

                result = await BucketManager.Upload(ms, ext, userId, file.ContentType, category, db);
                //path = await StorageManager.Save(ms, file.ContentType, ext, category, userId);
            }

            return Results.Ok(result.Message);
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
