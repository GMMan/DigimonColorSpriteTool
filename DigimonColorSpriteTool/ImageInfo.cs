using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigimonColorSpriteTool
{
    internal class ImageInfo
    {
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public int DataOffset { get; set; }
        public string? FilePath { get; set; }
        public byte[]? OverrideData { get; set; } // Override data takes precedence over FilePath
    }
}
