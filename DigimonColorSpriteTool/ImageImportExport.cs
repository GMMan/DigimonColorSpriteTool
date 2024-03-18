using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigimonColorSpriteTool
{
    public class ImageImportExport : IDisposable
    {
        static readonly string[] FILE_NAME_PATTERNS = new[]
        {
            "{0}", "{0:d3}", "{0}_0x{0:x}"
        };
        static readonly string[] FILE_EXTENSIONS = new[]
        {
            ".png", ".bmp"
        };

        Stream fwStream;
        BinaryReader br;
        List<ImageInfo> imageInfos = new List<ImageInfo>();
        FirmwareInfo firmwareInfo;
        private bool disposedValue;

        public int NumImages => imageInfos.Count;

        public ImageImportExport(Stream fwStream, FirmwareInfo firmwareInfo)
        {
            this.fwStream = fwStream ?? throw new ArgumentNullException(nameof(fwStream));
            this.firmwareInfo = firmwareInfo ?? throw new ArgumentNullException(nameof(firmwareInfo));
            br = new BinaryReader(fwStream);
            ReadMetadata();
        }

        void ReadMetadata()
        {
            // Read size table
            fwStream.Seek(firmwareInfo.SizeTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < firmwareInfo.NumImages; i++)
            {
                imageInfos.Add(new ImageInfo
                {
                    Width = br.ReadUInt16(),
                    Height = br.ReadUInt16(),
                });
            }

            // Read offsets
            fwStream.Seek(firmwareInfo.SpritePackBase, SeekOrigin.Begin);
            foreach (var info in imageInfos)
            {
                info.DataOffset = br.ReadInt32();
            }
        }

        void CheckDisposed()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);
        }

        static string? FindFile(string folderPath, int index)
        {
            foreach (var extension in FILE_EXTENSIONS)
            {
                foreach (var pattern in FILE_NAME_PATTERNS)
                {
                    string filePath = Path.Combine(folderPath, string.Format(pattern, index) + extension);
                    if (File.Exists(filePath)) return filePath;
                }
            }
            return null;
        }

        public void SetOverridesByFolder(string folderPath)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            for (int i = 0; i < imageInfos.Count; ++i)
            {
                string? filePath = FindFile(folderPath, i);
                imageInfos[i].FilePath = filePath;
                imageInfos[i].OverrideData = null;
            }
        }

        Image GetImage(ImageInfo info, bool useGreenAsAlpha)
        {
            fwStream.Seek(firmwareInfo.SpritePackBase + info.DataOffset, SeekOrigin.Begin);
            return ImageConverter.ConvertRgb565ToImage(br.ReadBytes(info.Width * info.Height * 2), info.Width, info.Height, useGreenAsAlpha);
        }

        public void ExportImage(int index, string destPath, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (index < 0 || index >= imageInfos.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (string.IsNullOrEmpty(destPath)) throw new ArgumentNullException(nameof(destPath));
            using var img = GetImage(imageInfos[index], useGreenAsAlpha);
            img.Save(destPath);
        }

        public void ExportAllImages(string destFolder, string extension, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(destFolder)) throw new ArgumentNullException(nameof(destFolder));
            if (string.IsNullOrEmpty(extension)) throw new ArgumentNullException(nameof(extension));
            for (int i = 0; i < imageInfos.Count; ++i)
            {
                ExportImage(i, Path.Combine(destFolder, $"{i}{extension}"), useGreenAsAlpha);
            }
        }

        public void Rebuild(string destPath, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(destPath)) throw new ArgumentNullException(nameof(destPath));
            using FileStream newStream = File.Create(destPath);
            BinaryWriter bw = new(newStream);

            // Copy firmware
            fwStream.Seek(0, SeekOrigin.Begin);
            fwStream.CopyTo(newStream);

            // Write image data
            List<int> offsets = new();
            newStream.Seek(firmwareInfo.SpritePackBase + imageInfos.Count * 4, SeekOrigin.Begin);
            int i = 0;
            foreach (var info in imageInfos)
            {
                offsets.Add((int)(newStream.Position - firmwareInfo.SpritePackBase));
                if (info.OverrideData != null)
                {
                    if (info.OverrideData.Length != info.Width * info.Height * 2)
                        Console.Error.WriteLine($"Warning: New image data {i} has different size ({info.OverrideData.Length}) than original ({info.Width * info.Height * 2}).");
                    bw.Write(info.OverrideData);
                }
                else if (info.FilePath != null)
                {
                    using (var img = Image.Load(info.FilePath))
                    {
                        if (img.Width != info.Width || img.Height != info.Height)
                        {
                            Console.Error.WriteLine($"Warning: New file {i} has different dimension ({img.Width}x{img.Height}) compared to original ({info.Width}x{info.Height}).");
                            info.Width = (ushort)img.Width;
                            info.Height = (ushort)img.Height;
                        }
                        var convertedImg = ImageConverter.ConvertImageToRgb565(img, useGreenAsAlpha);
                        bw.Write(convertedImg);
                    }
                }
                else
                {
                    fwStream.Seek(firmwareInfo.SpritePackBase + info.DataOffset, SeekOrigin.Begin);
                    byte[] data = br.ReadBytes(info.Width * info.Height * 2);
                    bw.Write(data);
                }

                ++i;
            }

            // Write image offsets
            newStream.Seek(firmwareInfo.SpritePackBase, SeekOrigin.Begin);
            foreach (var offset in offsets)
            {
                bw.Write(offset);
            }

            // Write size table
            newStream.Seek(firmwareInfo.SizeTableOffset, SeekOrigin.Begin);
            foreach (var info in imageInfos)
            {
                bw.Write(info.Width);
                bw.Write(info.Height);
            }
        }

        public void ImportSpriteSheet(string path, int startImageIndex, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (startImageIndex < 0 || startImageIndex + firmwareInfo.NumFramesPerChara >= imageInfos.Count)
                throw new ArgumentOutOfRangeException(nameof(startImageIndex), "Invalid start image index.");
            using var sheetImg = Image.Load(path);
            if (sheetImg.Width != firmwareInfo.CharaSpriteWidth) throw new ArgumentException("Image is not 48 pixels in width.", nameof(path));
            if (sheetImg.Height % firmwareInfo.CharaSpriteHeight != 0) throw new ArgumentException("Image height is not a multiple of 48 pixels.", nameof(path));
            if (sheetImg.Height / firmwareInfo.CharaSpriteHeight != firmwareInfo.NumFramesPerChara) throw new ArgumentException("Image does not have 15 frames.", nameof(path));

            for (int i = 0; i < firmwareInfo.NumFramesPerChara; ++i)
            {
                using var frameImg = new Image<Rgba32>((int)firmwareInfo.CharaSpriteWidth, (int)firmwareInfo.CharaSpriteHeight);
                frameImg.Mutate(x => x.DrawImage(sheetImg, new Point(0, i * -(int)firmwareInfo.CharaSpriteHeight), 1.0f));
                byte[] pixels = ImageConverter.ConvertImageToRgb565(frameImg, useGreenAsAlpha);
                imageInfos[startImageIndex + i].OverrideData = pixels;
            }
        }

        public void ImportSpriteSheetFolder(string folderPath, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            int startImageIndex = (int)firmwareInfo.CharasStartIndex;
            for (int i = 0; i < firmwareInfo.NumCharas; ++i)
            {
                string? filePath = FindFile(folderPath, i);
                if (filePath == null) continue;
                if (i < firmwareInfo.NumJogressCharas) ++startImageIndex;
                ImportSpriteSheet(filePath, startImageIndex, useGreenAsAlpha);
                startImageIndex += (int)firmwareInfo.NumFramesPerChara;
            }
        }

        public void ExportSpriteSheet(string path, int startImageIndex, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (startImageIndex < 0 || startImageIndex + firmwareInfo.NumFramesPerChara >= imageInfos.Count)
                throw new ArgumentOutOfRangeException(nameof(startImageIndex), "Invalid start image index.");
            using var sheetImg = new Image<Rgba32>((int)firmwareInfo.CharaSpriteWidth, (int)(firmwareInfo.CharaSpriteHeight * firmwareInfo.NumFramesPerChara));
            for (int i = 0; i < firmwareInfo.NumFramesPerChara; ++i)
            {
                using var frameImg = GetImage(imageInfos[startImageIndex + i], useGreenAsAlpha);
                sheetImg.Mutate(x => x.DrawImage(frameImg, new Point(0, i * (int)firmwareInfo.CharaSpriteHeight), 1.0f));
            }
            sheetImg.Save(path);
        }

        public void ExportSpriteSheetFolder(string folderPath, string extension, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            int startImageIndex = (int)firmwareInfo.CharasStartIndex;
            for (int i = 0; i < firmwareInfo.NumCharas; ++i)
            {
                string filePath = Path.Combine(folderPath, $"{i}{extension}");
                if (i < firmwareInfo.NumJogressCharas) ++startImageIndex;
                ExportSpriteSheet(filePath, startImageIndex, useGreenAsAlpha);
                startImageIndex += (int)firmwareInfo.NumFramesPerChara;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    fwStream.Close();
                    imageInfos.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
