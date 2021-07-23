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

        static StorageManager()
        {
            if (!Directory.Exists("../Content/ProfileImage"))
            {
                Directory.CreateDirectory("../Content/ProfileImage");
            }
            if (!Directory.Exists("../Content/Image"))
            {
                Directory.CreateDirectory("../Content/Image");
            }
        }

        /// <summary>
        /// Saves the given image
        /// </summary>
        /// <returns>The path to the image</returns>
        public static async Task<string> SaveImage(Bitmap image, string type)
        {
            return await Task.Run(() =>
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
                        using (FileStream file = new FileStream("../" + imagePath, FileMode.Create, FileAccess.Write))
                        {
                            stream.WriteTo(file);
                        }
                    }

                    return "https://msp.valour.gg/" + imagePath;
                }
            });
        }
    }
}
