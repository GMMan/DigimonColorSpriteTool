using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigimonColorSpriteTool
{
    public class FirmwareInfo
    {
        public uint SpritePackBase { get; set; }
        public uint CharaSpriteWidth { get; set; } = 48;
        public uint CharaSpriteHeight { get; set; } = 48;
        public uint SizeTableOffset { get; set; }
        public uint NumImages { get; set; }
        public uint NumCharas { get; set; }
        public uint NumFramesPerChara { get; set; }
        public uint CharasStartIndex { get; set; }
        public uint NumJogressCharas { get; set; }

        public static readonly Dictionary<string, FirmwareInfo> Presets = new()
        {
            { "dmc1", new FirmwareInfo
                {
                    SpritePackBase = 0x80000,
                    SizeTableOffset = 38296,
                    NumImages = 597,
                    NumCharas = 18,
                    NumFramesPerChara = 15,
                    CharasStartIndex = 210,
                    NumJogressCharas = 0,
                }
            },
            { "dmc2", new FirmwareInfo
                {
                    SpritePackBase = 0x80000,
                    SizeTableOffset = 40346,
                    NumImages = 597,
                    NumCharas = 18,
                    NumFramesPerChara = 15,
                    CharasStartIndex = 210,
                    NumJogressCharas = 1,
                }
            },
            { "dmc3", new FirmwareInfo
                {
                    SpritePackBase = 0x80000,
                    SizeTableOffset = 38632,
                    NumImages = 628,
                    NumCharas = 19,
                    NumFramesPerChara = 15,
                    CharasStartIndex = 210,
                    NumJogressCharas = 0,
                }
            },
            { "dmc4", new FirmwareInfo
                {
                    SpritePackBase = 0x80000,
                    SizeTableOffset = 41032,
                    NumImages = 613,
                    NumCharas = 19,
                    NumFramesPerChara = 15,
                    CharasStartIndex = 210,
                    NumJogressCharas = 1,
                }
            },
            { "dmc5", new FirmwareInfo
                {
                    SpritePackBase = 0x80000,
                    SizeTableOffset = 38296,
                    NumImages = 613,
                    NumCharas = 19,
                    NumFramesPerChara = 15,
                    CharasStartIndex = 210,
                    NumJogressCharas = 2,
                }
            },
            { "penc1", new FirmwareInfo
                {
                    SpritePackBase = 0x400000,
                    SizeTableOffset = 64796,
                    NumImages = 759,
                    NumCharas = 32,
                    NumFramesPerChara = 12,
                    CharasStartIndex = 240,
                    NumJogressCharas = 0,
                }
            },
            { "penc2", new FirmwareInfo
                {
                    SpritePackBase = 0x400000,
                    SizeTableOffset = 64730,
                    NumImages = 759,
                    NumCharas = 32,
                    NumFramesPerChara = 12,
                    CharasStartIndex = 240,
                    NumJogressCharas = 0,
                }
            },
            { "penc3", new FirmwareInfo
                {
                    SpritePackBase = 0x400000,
                    SizeTableOffset = 64736,
                    NumImages = 759,
                    NumCharas = 32,
                    NumFramesPerChara = 12,
                    CharasStartIndex = 240,
                    NumJogressCharas = 0,
                }
            },
        };
    }
}
