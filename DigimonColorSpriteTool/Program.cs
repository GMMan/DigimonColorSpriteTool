// See https://aka.ms/new-console-template for more information

using DigimonColorSpriteTool;
using System.CommandLine;

var rootCommand = new RootCommand("Digimon Color Sprite Import/Export Tool");

// Options
var useGreenAsAlphaOption = new Option<bool>("--green-transparency", "Use full green pixel as transparency");
useGreenAsAlphaOption.AddAlias("-g");
var useBmpOption = new Option<bool>("--bmp", "Output as BMP");
var numJogressOption = new Option<uint>("--num-jogress", () => 0, "Number of jogresses");
numJogressOption.AddAlias("-j");

// Arguments
var presetNameArgument = new Argument<string>("presetName", "Device firmware preset name")
        .FromAmong(FirmwareInfo.Presets.Keys.ToArray());
var spritePackBaseArgument = new Argument<uint>("spritePackBase", "Sprite pack offset in flash");
var sizeTableOffsetArgument = new Argument<uint>("sizeTableOffset", "Size table offset");
var numImagesArgument = new Argument<uint>("numImages", "Number of images in ROM");
var numCharasArgument = new Argument<uint>("numCharas", "Number of characters in ROM");
var numFramesPerCharaArgument = new Argument<uint>("numFramesPerChara", "Number of frames per character");
var charaStartIndexArgument = new Argument<uint>("charaStartIndex", "Index of first character sprite");

var romPathArgument = new Argument<FileInfo>("romPath", "Path to flash dump").ExistingOnly();
var outDirArgument = new Argument<DirectoryInfo>("outDir", "Output directory").LegalFilePathsOnly();
var outFileArgument = new Argument<FileInfo?>("outFile", "Output file").LegalFilePathsOnly();
outFileArgument.Arity = ArgumentArity.ZeroOrOne;
var inDirArgument = new Argument<DirectoryInfo>("inDir", "Input directory").ExistingOnly();

#region Command handlers
void DoExport(FileInfo romFile, FirmwareInfo fwInfo, DirectoryInfo outDir, bool useBmp, bool useGreenAsAlpha)
{
    if (useBmp) useGreenAsAlpha = true;
    using var impExp = new ImageImportExport(romFile.OpenRead(), fwInfo);
    outDir.Create();
    impExp.ExportAllImages(outDir.FullName, useBmp ? ".bmp" : ".png", useGreenAsAlpha);
}

void DoImport(FileInfo romFile, FirmwareInfo fwInfo, DirectoryInfo inDir, FileInfo? outFile, bool useGreenAsAlpha)
{
    bool needCopyOverSrc = outFile == null || outFile.FullName == romFile.FullName; // Not foolproof
    if (needCopyOverSrc) outFile = new FileInfo(Path.GetTempFileName());
    using (var impExp = new ImageImportExport(romFile.OpenRead(), fwInfo))
    {
        impExp.SetOverridesByFolder(inDir.FullName);
        impExp.Rebuild(outFile.FullName, useGreenAsAlpha);
    }
    if (needCopyOverSrc)
    {
        outFile.CopyTo(romFile.FullName, true);
        outFile.Delete();
    }
}

void DoExportSheets(FileInfo romFile, FirmwareInfo fwInfo, DirectoryInfo outDir, bool useGreenAsAlpha, bool useBmp)
{
    if (useBmp) useGreenAsAlpha = true;
    using var impExp = new ImageImportExport(romFile.OpenRead(), fwInfo);
    outDir.Create();
    impExp.ExportSpriteSheetFolder(outDir.FullName, useBmp ? ".bmp" : ".png", useGreenAsAlpha);
}

void DoImportSheets(FileInfo romFile, FirmwareInfo fwInfo, DirectoryInfo inDir, FileInfo? outFile, bool useGreenAsAlpha)
{
    bool needCopyOverSrc = outFile == null || outFile.FullName == romFile.FullName; // Not foolproof
    if (needCopyOverSrc) outFile = new FileInfo(Path.GetTempFileName());
    using (var impExp = new ImageImportExport(romFile.OpenRead(), fwInfo))
    {
        impExp.ImportSpriteSheetFolder(inDir.FullName, useGreenAsAlpha);
        impExp.Rebuild(outFile.FullName, useGreenAsAlpha);
    }
    if (needCopyOverSrc)
    {
        outFile.CopyTo(romFile.FullName, true);
        outFile.Delete();
    }
}
#endregion

#region Commands with presets
var exportPresetCmd = new Command("export-preset", "Export all sprites using firmware preset")
{
    useGreenAsAlphaOption, useBmpOption, romPathArgument, presetNameArgument, outDirArgument
};
rootCommand.AddCommand(exportPresetCmd);

exportPresetCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var useBmp = pr.GetValueForOption(useBmpOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var presetName = pr.GetValueForArgument(presetNameArgument);
    var outDir = pr.GetValueForArgument(outDirArgument);

    var fwInfo = FirmwareInfo.Presets[presetName];

    DoExport(romPath, fwInfo, outDir, useBmp, useGreenAsAlpha);
});

var importPresetCmd = new Command("import-preset", "Import sprites using firmware preset")
{
    useGreenAsAlphaOption, romPathArgument, presetNameArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importPresetCmd);

importPresetCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var presetName = pr.GetValueForArgument(presetNameArgument);
    var inDir = pr.GetValueForArgument(inDirArgument);
    var outFile = pr.GetValueForArgument(outFileArgument);

    var fwInfo = FirmwareInfo.Presets[presetName];

    DoImport(romPath, fwInfo, inDir, outFile, useGreenAsAlpha);
});

var exportSheetsPresetCmd = new Command("export-sheets-preset", "Export all character sprite sheets using firmware preset")
{
    useGreenAsAlphaOption, useBmpOption, romPathArgument, presetNameArgument, outDirArgument
};
rootCommand.Add(exportSheetsPresetCmd);

exportSheetsPresetCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var useBmp = pr.GetValueForOption(useBmpOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var presetName = pr.GetValueForArgument(presetNameArgument);
    var outDir = pr.GetValueForArgument(outDirArgument);

    var fwInfo = FirmwareInfo.Presets[presetName];

    DoExportSheets(romPath, fwInfo, outDir, useGreenAsAlpha, useBmp);
});

var importSheetsPresetCmd = new Command("import-sheets-preset", "Import character sprite sheets using firmware preset")
{
    useGreenAsAlphaOption, romPathArgument, presetNameArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importSheetsPresetCmd);

importSheetsPresetCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var presetName = pr.GetValueForArgument(presetNameArgument);
    var inDir = pr.GetValueForArgument(inDirArgument);
    var outFile = pr.GetValueForArgument(outFileArgument);

    var fwInfo = FirmwareInfo.Presets[presetName];

    DoImportSheets(romPath, fwInfo, inDir, outFile, useGreenAsAlpha);
});

var showPresetCmd = new Command("show-preset", "Show the parameters of a preset")
{
    presetNameArgument
};
rootCommand.Add(showPresetCmd);

showPresetCmd.SetHandler(presetName =>
{
    var fwInfo = FirmwareInfo.Presets[presetName];
    Console.WriteLine($"Name: {presetName}");
    Console.WriteLine($"{nameof(fwInfo.SpritePackBase)}: {fwInfo.SpritePackBase}");
    Console.WriteLine($"{nameof(fwInfo.CharaSpriteWidth)}: {fwInfo.CharaSpriteWidth}");
    Console.WriteLine($"{nameof(fwInfo.CharaSpriteHeight)}: {fwInfo.CharaSpriteHeight}");
    Console.WriteLine($"{nameof(fwInfo.SizeTableOffset)}: {fwInfo.SizeTableOffset}");
    Console.WriteLine($"{nameof(fwInfo.NumImages)}: {fwInfo.NumImages}");
    Console.WriteLine($"{nameof(fwInfo.NumCharas)}: {fwInfo.NumCharas}");
    Console.WriteLine($"{nameof(fwInfo.NumFramesPerChara)}: {fwInfo.NumFramesPerChara}");
    Console.WriteLine($"{nameof(fwInfo.CharasStartIndex)}: {fwInfo.CharasStartIndex}");
    Console.WriteLine($"{nameof(fwInfo.NumJogressCharas)}: {fwInfo.NumJogressCharas}");
}, presetNameArgument);
#endregion

#region Commands without presets
var exportCmd = new Command("export", "Export all sprites")
{
    useGreenAsAlphaOption, useBmpOption, romPathArgument, spritePackBaseArgument, sizeTableOffsetArgument,
    numImagesArgument, outDirArgument
};
rootCommand.AddCommand(exportCmd);

exportCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var useBmp = pr.GetValueForOption(useBmpOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var spritePackBase = pr.GetValueForArgument(spritePackBaseArgument);
    var sizeTableOffset = pr.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = pr.GetValueForArgument(numImagesArgument);
    var outDir = pr.GetValueForArgument(outDirArgument);

    var fwInfo = new FirmwareInfo
    {
        SpritePackBase = spritePackBase,
        SizeTableOffset = sizeTableOffset,
        NumImages = numImages,
    };

    DoExport(romPath, fwInfo, outDir, useBmp, useGreenAsAlpha);
});

var importCmd = new Command("import", "Import sprites")
{
    useGreenAsAlphaOption, romPathArgument, spritePackBaseArgument, sizeTableOffsetArgument,
    numImagesArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importCmd);

importCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var spritePackBase = pr.GetValueForArgument(spritePackBaseArgument);
    var sizeTableOffset = pr.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = pr.GetValueForArgument(numImagesArgument);
    var inDir = pr.GetValueForArgument(inDirArgument);
    var outFile = pr.GetValueForArgument(outFileArgument);

    var fwInfo = new FirmwareInfo
    {
        SpritePackBase = spritePackBase,
        SizeTableOffset = sizeTableOffset,
        NumImages = numImages,
    };

    DoImport(romPath, fwInfo, inDir, outFile, useGreenAsAlpha);
});

var exportSheetsCmd = new Command("export-sheets", "Export all character sprite sheets")
{
    useGreenAsAlphaOption, useBmpOption, numJogressOption, romPathArgument, outDirArgument,
    spritePackBaseArgument, sizeTableOffsetArgument, numImagesArgument, numCharasArgument,
    numFramesPerCharaArgument, charaStartIndexArgument
};
rootCommand.Add(exportSheetsCmd);

exportSheetsCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var useBmp = pr.GetValueForOption(useBmpOption);
    var numJogresses = pr.GetValueForOption(numJogressOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var outDir = pr.GetValueForArgument(outDirArgument);
    var spritePackBase = pr.GetValueForArgument(spritePackBaseArgument);
    var sizeTableOffset = pr.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = pr.GetValueForArgument(numImagesArgument);
    var numCharas = pr.GetValueForArgument(numCharasArgument);
    var numFramesPerChara = pr.GetValueForArgument(numFramesPerCharaArgument);
    var charaStartIndex = pr.GetValueForArgument(charaStartIndexArgument);

    var fwInfo = new FirmwareInfo
    {
        SpritePackBase = spritePackBase,
        SizeTableOffset = sizeTableOffset,
        NumImages = numImages,
        NumCharas = numCharas,
        NumFramesPerChara = numFramesPerChara,
        CharasStartIndex = charaStartIndex,
        NumJogressCharas = numJogresses
    };

    DoExportSheets(romPath, fwInfo, outDir, useGreenAsAlpha, useBmp);
});

var importSheetsCmd = new Command("import-sheets", "Import character sprite sheets")
{
    useGreenAsAlphaOption, numJogressOption, romPathArgument, spritePackBaseArgument,
    sizeTableOffsetArgument, numImagesArgument, numCharasArgument, numFramesPerCharaArgument,
    charaStartIndexArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importSheetsCmd);

importSheetsCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var numJogresses = pr.GetValueForOption(numJogressOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var spritePackBase = pr.GetValueForArgument(spritePackBaseArgument);
    var sizeTableOffset = pr.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = pr.GetValueForArgument(numImagesArgument);
    var numCharas = pr.GetValueForArgument(numCharasArgument);
    var numFramesPerChara = pr.GetValueForArgument(numFramesPerCharaArgument);
    var charaStartIndex = pr.GetValueForArgument(charaStartIndexArgument);
    var inDir = pr.GetValueForArgument(inDirArgument);
    var outFile = pr.GetValueForArgument(outFileArgument);

    var fwInfo = new FirmwareInfo
    {
        SpritePackBase = spritePackBase,
        SizeTableOffset = sizeTableOffset,
        NumImages = numImages,
        NumCharas = numCharas,
        NumFramesPerChara = numFramesPerChara,
        CharasStartIndex = charaStartIndex,
        NumJogressCharas = numJogresses
    };

    DoImportSheets(romPath, fwInfo, inDir, outFile, useGreenAsAlpha);
});
#endregion

return rootCommand.Invoke(args);
