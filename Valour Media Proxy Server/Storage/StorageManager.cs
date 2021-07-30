using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Valour.MPS.Images;

namespace Valour.MPS.Storage
{
    public static class StorageManager
    {
        static SHA256 SHA256 = SHA256.Create();

        public const string _ProfilePath = "../Content/ProfileImage";
        public const string _ImagePath = "../Content/Image";
        public const string _FilePath = "../Content/File";

        static StorageManager()
        {
            if (!Directory.Exists(_ProfilePath))
            {
                Directory.CreateDirectory(_ProfilePath);
            }
            if (!Directory.Exists(_ImagePath))
            {
                Directory.CreateDirectory(_ImagePath);
            }
            if (!Directory.Exists(_FilePath))
            {
                Directory.CreateDirectory(_FilePath);
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes > 1073741824)
                return Math.Ceiling(bytes / 1073741824M).ToString("#,### GB");
            else if (bytes > 1048576)
                return Math.Ceiling(bytes / 1048576M).ToString("#,### MB");
            else if (bytes >= 1)
                return Math.Ceiling(bytes / 1024M).ToString("#,### KB");
            else if (bytes < 0)
                return "";
            else
                return bytes.ToString("#,### B");
        }

        public static async Task EnsureDiskSpace()
        {
            await Task.Run(() =>
            {
                var dir = "../Content";
                FileInfo fInfo = new FileInfo(dir);
                DriveInfo driveInfo = new DriveInfo(fInfo.Directory.Root.FullName);

                Console.WriteLine($"Available space: {FormatBytes(driveInfo.AvailableFreeSpace)} / " +
                                                       $"{FormatBytes(driveInfo.TotalSize)}");

                // If we have at least 20% free space, don't do anything
                if (driveInfo.AvailableFreeSpace > (driveInfo.TotalSize * 0.2f))
                {
                    return;
                }

                Console.WriteLine($"Detected low space! Clearing drive space.");

                // Delete oldest files (this will SOON properly store in the cloud)
                // We won't delete profile pictures because those need to last
                FileSystemInfo[] fileInfo = new DirectoryInfo(_FilePath).GetFileSystemInfos();
                FileSystemInfo[] imageInfo = new DirectoryInfo(_ImagePath).GetFileSystemInfos();

                int removed = 0;

                // Delete the oldest half of the files
                foreach (var info in fileInfo.OrderBy(x => x.CreationTimeUtc).Take(fileInfo.Length / 2))
                {
                    // Do not delete directories!!!
                    if (info.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        continue;
                    }

                    info.Delete();
                    removed++;
                }

                // Delete the oldest half of the files
                foreach (var info in imageInfo.OrderBy(x => x.CreationTimeUtc).Take(imageInfo.Length / 2))
                {
                    // Do not delete directories!!!
                    if (info.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        continue;
                    }

                    info.Delete();
                    removed++;
                }

                Console.WriteLine($"Removed {removed} files. New free space is {FormatBytes(driveInfo.AvailableFreeSpace)}");
            });
        }

        /// <summary>
        /// Saves the given image
        /// </summary>
        /// <returns>The path to the image</returns>
        public static async Task<string> SaveImage(Bitmap image, string type)
        {
            return await Task.Run(async () =>
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    image.Save(stream, ImageUtility.jpegEncoder,
                                       ImageUtility.profileEncodeParams);

                    // Get hash from image
                    byte[] h = SHA256.ComputeHash(stream.ToArray());
                    string hash = BitConverter.ToString(h).Replace("-", "").ToLower();

                    string imagePath = "Content/" + type + "/" + hash + ".jpg";

                    if (!File.Exists(imagePath))
                    {
                        await EnsureDiskSpace();

                        using (FileStream file = new FileStream("../" + imagePath, FileMode.Create, FileAccess.Write))
                        {
                            stream.WriteTo(file);
                        }
                    }

                    return "https://vmps.valour.gg/" + imagePath;
                }
            });
        }

        public static async Task<string> SaveContent(IFormFile file, string type)
        {
            return await Task.Run(async () =>
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    // Get hash from file
                    byte[] h = SHA256.ComputeHash(stream.ToArray());
                    string hash = BitConverter.ToString(h).Replace("-", "").ToLower();

                    string ext = Path.GetExtension(file.FileName);

                    string contentPath = "Content/" + type + "/" + hash + ext;
                    string metaPath = "Content/" + type + "/" + hash + ext + ".meta";

                    if (!File.Exists(contentPath))
                    {
                        await EnsureDiskSpace();

                        using (FileStream fileStream = new FileStream("../" + contentPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.WriteTo(fileStream);
                        }

                        // Write metadata
                        await File.WriteAllTextAsync("../" + metaPath, file.ContentType);
                    }

                    return "https://msp.valour.gg/" + contentPath;
                }
            });
        }
    }
}
