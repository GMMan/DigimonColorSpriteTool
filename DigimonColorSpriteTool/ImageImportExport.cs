using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigimonColorSpriteTool
{
    public class ImageImportExport : IDisposable
    {
        static readonly string[] FILE_NAME_PATTERNS =
        [
            "{0}", "{0:d3}", "{0}_0x{0:x}"
        ];
        static readonly string[] FILE_EXTENSIONS =
        [
            ".png", ".bmp"
        ];
        static readonly string NAME_SUFFIX = "_name";
        static readonly string CUTIN_SUFFIX = "_cutin";

        Stream fwStream;
        BinaryReader br;
        List<ImageInfo> imageInfos = new List<ImageInfo>();
        FirmwareInfo firmwareInfo;
        private bool disposedValue;
        List<ImageType> imageTypes = new();
        List<ImageType> specialImageTypes = new();

        public int NumImages => imageInfos.Count;

        public ImageImportExport(Stream fwStream, FirmwareInfo firmwareInfo)
        {
            this.fwStream = fwStream ?? throw new ArgumentNullException(nameof(fwStream));
            this.firmwareInfo = firmwareInfo ?? throw new ArgumentNullException(nameof(firmwareInfo));
            br = new BinaryReader(fwStream);
            ReadMetadata();
            BuildImageTypes();
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

        void BuildImageTypes()
        {
            int i;
            for (i = 0; i < firmwareInfo.NumFramesPerChara; ++i)
            {
                imageTypes.Add(ImageType.CharacterSprite);
            }
            if (firmwareInfo.HasCutin)
            {
                imageTypes.Add(ImageType.Cutin);
            }
            if (firmwareInfo.NumFramesPerSpecialChara > firmwareInfo.NumFramesPerChara)
            {
                specialImageTypes.AddRange(imageTypes);
                for (; i < firmwareInfo.NumFramesPerSpecialChara; ++i)
                {
                    specialImageTypes.Add(ImageType.CharacterSprite);
                }
                if (firmwareInfo.HasCutin && !firmwareInfo.OmitSpecialCutin)
                {
                    specialImageTypes.Add(ImageType.Cutin);
                }
                if (firmwareInfo.HasName)
                {
                    specialImageTypes.Add(ImageType.Name);
                }
            }
            if (firmwareInfo.HasName)
            {
                imageTypes.Add(ImageType.Name);
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

        public void ImportSpriteSheet(string path, int startImageIndex, bool useGreenAsAlpha, uint? rows, uint? cols, bool isSpecial)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            uint numFrames = isSpecial ? firmwareInfo.NumFramesPerSpecialChara : firmwareInfo.NumFramesPerChara;
            if (startImageIndex < 0 || startImageIndex + numFrames >= imageInfos.Count)
                throw new ArgumentOutOfRangeException(nameof(startImageIndex), "Invalid start image index.");
            if (!rows.HasValue) rows = numFrames;
            if (!cols.HasValue) cols = 1;
            if (rows * cols < numFrames)
                throw new ArgumentException("Not enough rows and cols for number of frame per character.");

            using var sheetImg = Image.Load(path);
            int sheetFrameWidth = (int)(sheetImg.Width / cols);
            int sheetFrameHeight = (int)(sheetImg.Height / rows);
            // Do not allow oversized sprites, even if it technically would work
            if (sheetFrameWidth > firmwareInfo.CharaSpriteWidth || sheetFrameHeight > firmwareInfo.CharaSpriteHeight)
                throw new ArgumentException("Sheet frame is too large for device character frame.", nameof(path));
            if (firmwareInfo.CharaSpriteWidth % sheetFrameWidth != 0)
                throw new ArgumentException("Sheet frame width cannot be scaled by an integral factor.", nameof(path));
            if (firmwareInfo.CharaSpriteHeight % sheetFrameHeight != 0)
                throw new ArgumentException("Sheet frame height cannot be scaled by an integral factor.", nameof(path));
            int scaleFactorX = (int)(firmwareInfo.CharaSpriteWidth / sheetFrameWidth);
            int scaleFactorY = (int)(firmwareInfo.CharaSpriteHeight / sheetFrameHeight);

            List<ImageType> allImageTypes = isSpecial ? specialImageTypes : imageTypes;
            int currRow = 0;
            int currCol = 0;
            int cutinIndex = 0;
            for (int i = 0; i < allImageTypes.Count; ++i)
            {
                switch (allImageTypes[i])
                {
                    case ImageType.CharacterSprite:
                        {
                            using var frameImg = new Image<Rgba32>(sheetFrameWidth, sheetFrameHeight);
                            frameImg.Mutate(x => x.DrawImage(sheetImg, new Point(-sheetFrameWidth * currCol, -sheetFrameHeight * currRow), 1.0f));
                            if (scaleFactorX != 1 || scaleFactorY != 1)
                            {
                                frameImg.Mutate(x => x.Resize(sheetFrameWidth * scaleFactorX, sheetFrameHeight * scaleFactorY, KnownResamplers.NearestNeighbor));
                            }
                            byte[] pixels = ImageConverter.ConvertImageToRgb565(frameImg, useGreenAsAlpha);
                            imageInfos[startImageIndex + i].OverrideData = pixels;
                            ++currCol;
                            if (currCol >= cols)
                            {
                                ++currRow;
                                currCol = 0;
                            }
                            break;

                        }
                    case ImageType.Cutin:
                        {
                            string cutinName = CUTIN_SUFFIX;
                            if (isSpecial)
                            {
                                cutinName += cutinIndex++;
                            }
                            string cutinPath = $"{Path.ChangeExtension(path, null)}{cutinName}{Path.GetExtension(path)}";
                            if (File.Exists(cutinPath))
                            {
                                using var cutinImg = Image.Load(cutinPath);
                                byte[] pixels = ImageConverter.ConvertImageToRgb565(cutinImg, useGreenAsAlpha);
                                var nameInfo = imageInfos[startImageIndex + i];
                                nameInfo.OverrideData = pixels;
                                nameInfo.Width = (ushort)cutinImg.Width;
                                nameInfo.Height = (ushort)cutinImg.Height;
                            }
                        }
                        break;
                    case ImageType.Name:
                        {
                            string namePath = $"{Path.ChangeExtension(path, null)}{NAME_SUFFIX}{Path.GetExtension(path)}";
                            if (File.Exists(namePath))
                            {
                                using var nameImg = Image.Load(namePath);
                                byte[] pixels = ImageConverter.ConvertImageToRgb565(nameImg, useGreenAsAlpha);
                                var nameInfo = imageInfos[startImageIndex + i];
                                nameInfo.OverrideData = pixels;
                                nameInfo.Width = (ushort)nameImg.Width;
                                nameInfo.Height = (ushort)nameImg.Height;
                            }
                        }
                        break;
                }
            }
        }

        public void ImportSpriteSheetFolder(string folderPath, bool useGreenAsAlpha, uint? rows, uint? cols)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            int startImageIndex = (int)firmwareInfo.CharasStartIndex;
            for (int i = 0; i < firmwareInfo.NumCharas; ++i)
            {
                string? filePath = FindFile(folderPath, i);
                bool isSpecial = Array.IndexOf(firmwareInfo.SpecialCharaIndexes, (uint)i) != -1;
                if (i < firmwareInfo.NumJogressCharas) ++startImageIndex;
                if (filePath != null)
                {
                    ImportSpriteSheet(filePath, startImageIndex, useGreenAsAlpha, rows, cols, isSpecial);
                }
                startImageIndex += isSpecial ? specialImageTypes.Count : imageTypes.Count;

                if (firmwareInfo.PendulumNameStart is uint nameStart)
                {
                    string namePath = $"{Path.ChangeExtension(filePath, null)}{NAME_SUFFIX}{Path.GetExtension(filePath)}";
                    if (File.Exists(namePath))
                    {
                        using var nameImg = Image.Load(namePath);
                        byte[] pixels = ImageConverter.ConvertImageToRgb565(nameImg, useGreenAsAlpha);
                        var nameInfo = imageInfos[(int)(nameStart + i)];
                        nameInfo.OverrideData = pixels;
                        nameInfo.Width = (ushort)nameImg.Width;
                        nameInfo.Height = (ushort)nameImg.Height;
                    }
                }
            }
        }

        public void ExportSpriteSheet(string basePath, string extension, int startImageIndex, bool useGreenAsAlpha, bool isSpecial)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(basePath)) throw new ArgumentNullException(nameof(basePath));
            if (string.IsNullOrEmpty(extension)) throw new ArgumentNullException(nameof(extension));
            uint numFrames = isSpecial ? firmwareInfo.NumFramesPerSpecialChara : firmwareInfo.NumFramesPerChara;
            if (startImageIndex < 0 || startImageIndex + numFrames >= imageInfos.Count)
                throw new ArgumentOutOfRangeException(nameof(startImageIndex), "Invalid start image index.");
            using var sheetImg = new Image<Rgba32>((int)firmwareInfo.CharaSpriteWidth, (int)(firmwareInfo.CharaSpriteHeight * numFrames));

            List<ImageType> allImageTypes = isSpecial ? specialImageTypes : imageTypes;
            int cutinIndex = 0;
            for (int i = 0; i < allImageTypes.Count; ++i)
            {
                switch (allImageTypes[i])
                {
                    case ImageType.CharacterSprite:
                        {
                            using var frameImg = GetImage(imageInfos[startImageIndex + i], useGreenAsAlpha);
                            sheetImg.Mutate(x => x.DrawImage(frameImg, new Point(0, (i - cutinIndex) * (int)firmwareInfo.CharaSpriteHeight), 1.0f));
                        }
                        break;
                    case ImageType.Cutin:
                        {
                            string cutinName = CUTIN_SUFFIX;
                            if (isSpecial)
                            {
                                cutinName += cutinIndex++;
                            }
                            using var cutinSprite = GetImage(imageInfos[startImageIndex + i], useGreenAsAlpha);
                            cutinSprite.Save($"{basePath}{cutinName}{extension}");
                        }
                        break;
                    case ImageType.Name:
                        {
                            using var nameSprite = GetImage(imageInfos[startImageIndex + i], useGreenAsAlpha);
                            nameSprite.Save($"{basePath}{NAME_SUFFIX}{extension}");
                        }
                        break;
                }
            }
            sheetImg.Save(basePath + extension);
        }

        public void ExportSpriteSheetFolder(string folderPath, string extension, bool useGreenAsAlpha)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            int startImageIndex = (int)firmwareInfo.CharasStartIndex;
            for (int i = 0; i < firmwareInfo.NumCharas; ++i)
            {
                string filePath = Path.Combine(folderPath, $"{i}");
                bool isSpecial = Array.IndexOf(firmwareInfo.SpecialCharaIndexes, (uint)i) != -1;
                if (i < firmwareInfo.NumJogressCharas) ++startImageIndex;
                ExportSpriteSheet(filePath, extension, startImageIndex, useGreenAsAlpha, isSpecial);
                startImageIndex += isSpecial ? specialImageTypes.Count : imageTypes.Count;

                if (firmwareInfo.PendulumNameStart is uint nameStart)
                {
                    using var nameSprite = GetImage(imageInfos[(int)(nameStart + i)], useGreenAsAlpha);
                    nameSprite.Save($"{filePath}{NAME_SUFFIX}{extension}");
                }
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
