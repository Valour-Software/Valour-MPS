using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace Valour.MPS.Images
{
    public static class ImageUtility
    {

        public static EncoderParameters profileEncodeParams = new EncoderParameters(1);
        public static ImageCodecInfo jpegEncoder;

        static ImageUtility()
        {
            profileEncodeParams.Param[0] = new EncoderParameter(Encoder.Quality, 80L);
            jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static async Task<Bitmap> ConvertToProfileImage(Bitmap sourceImage)
        {
            // Destination rect and bitmap
            var destRect = new Rectangle(0, 0, 256, 256);
            var destImage = new Bitmap(256, 256);

            // Do heavy calculations in another thread
            await Task.Run(() =>
            {
                //destImage.SetResolution(sourceImage.HorizontalResolution,
                //                        sourceImage.VerticalResolution);

                // Do resize
                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipX);
                        graphics.DrawImage(sourceImage, destRect, 0, 0,
                                           sourceImage.Width, sourceImage.Height,
                                           GraphicsUnit.Pixel, wrapMode);
                    }
                }


            });

            return destImage;
        }

        public static async Task<Bitmap> ConvertToPlanetImage(Bitmap sourceImage)
        {
            // Destination rect and bitmap
            var destRect = new Rectangle(0, 0, 256, 256);
            var destImage = new Bitmap(512, 512);

            // Do heavy calculations in another thread
            await Task.Run(() =>
            {
                //destImage.SetResolution(sourceImage.HorizontalResolution,
                //                        sourceImage.VerticalResolution);

                // Do resize
                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipX);
                        graphics.DrawImage(sourceImage, destRect, 0, 0,
                                           sourceImage.Width, sourceImage.Height,
                                           GraphicsUnit.Pixel, wrapMode);
                    }
                }


            });

            return destImage;
        }
    }
}
