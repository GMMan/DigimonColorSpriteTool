using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigimonColorSpriteTool
{
    public static class ImageConverter
    {
        public static Image ConvertRgb565ToImage(byte[] pixels, int width, int height, bool useGreenAsAlpha)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width cannot be negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");
            if (pixels.Length != width * height * 2) throw new ArgumentException("Image size is incorrect.");

            // Modified ConvertImage565 with alpha
            int i = 0;
            var img = new Image<Rgba32>(width, height);
            img.ProcessPixelRows(proc =>
            {
                for (int y = 0; y < proc.Height; ++y)
                {
                    var row = proc.GetRowSpan(y);
                    for (int x = 0; x < proc.Width; ++x)
                    {
                        ushort c = (ushort)(pixels[i++] | (pixels[i++] << 8));
                        float b = ((c >> 0) & 0x1f) / 31f;
                        float g = ((c >> 5) & 0x3f) / 63f;
                        float r = ((c >> 11) & 0x1f) / 31f;
                        float a = 1;
                        if (!useGreenAsAlpha && b == 0 && g == 1 && r == 0)
                            a = 0;
                        row[x] = new Rgba32(r, g, b, a);
                    }
                }
            });
            return img;
        }

        public static byte[] ConvertImageToRgb565(Image img, bool useGreenAsAlpha)
        {
            if (img == null) throw new ArgumentNullException(nameof(img));

            var rgbImg = img.CloneAs<Rgba32>();
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                rgbImg.ProcessPixelRows(proc =>
                {
                    for (int y = 0; y < proc.Height; ++y)
                    {
                        var row = proc.GetRowSpan(y);
                        for (int x = 0; x < proc.Width; ++x)
                        {
                            var pixel = row[x].ToScaledVector4();
                            int r = (int)Math.Round(pixel.X * 31);
                            int g = (int)Math.Round(pixel.Y * 63);
                            int b = (int)Math.Round(pixel.Z * 31);
                            if (pixel.W == 0)
                            {
                                r = 0;
                                g = 63;
                                b = 0;
                            }
                            else if (!useGreenAsAlpha && r == 0 && g == 63 && b == 0)
                            {
                                --g;
                            }

                            ushort c = (ushort)((r << 11) | (g << 5) | b);
                            bw.Write(c);
                        }
                    }
                });
                ms.Flush();
                return ms.ToArray();
            }
        }
    }
}
