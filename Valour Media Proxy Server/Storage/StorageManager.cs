using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Valour.MPS.Images;

namespace Valour.MPS.Storage
{
    public static class StorageManager
    {
        static SHA256 SHA256 = SHA256.Create();

        public static readonly List<string> _ContentTypes = new()
        {
            "planet",
            "profile",
            "image",
            "file"
        };

        public static JpegEncoder jpegEncoder = new()
        {
            Quality = 80,
            Subsample = JpegSubsample.Ratio444
        };

        public static PngEncoder pngEncoder = new()
        {
            CompressionLevel = PngCompressionLevel.BestCompression
        };

        static StorageManager()
        {
            foreach (string type in _ContentTypes)
                if (!Directory.Exists("../Content/" + type))
                    Directory.CreateDirectory("../Content/" + type);
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
                // We won't delete profile pictures and planet icons because those need to last
                FileSystemInfo[] fileInfo = new DirectoryInfo("../Content/profile").GetFileSystemInfos();
                FileSystemInfo[] imageInfo = new DirectoryInfo("../Content/planet").GetFileSystemInfos();

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
        public static async Task<string> Save(MemoryStream ms, string content_type, string ext, string type)
        {
            type = type.ToLower();

            // Get hash from image
            byte[] h = SHA256.ComputeHash(ms);
            string hash = BitConverter.ToString(h).Replace("-", "").ToLower();

            string filePath = "Content/" + type + "/" + hash + ext;
            string metaPath = "Content/" + type + "/" + hash + ext + ".meta";

            if (!File.Exists(filePath))
            {
                await EnsureDiskSpace();

                using (FileStream fs = new("../" + filePath, FileMode.Create, FileAccess.Write))
                {
                    ms.WriteTo(fs);
                }

                // Write metadata
                await File.WriteAllTextAsync("../" + metaPath, $"{content_type}\n{ext}");
            }

            return "https://vmps.valour.gg/" + filePath;
            
        }
    }
}
