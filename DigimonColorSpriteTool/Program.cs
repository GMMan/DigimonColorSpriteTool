// See https://aka.ms/new-console-template for more information

using DigimonColorSpriteTool;
using System.CommandLine;
using System.CommandLine.Parsing;

var rootCommand = new RootCommand("Digimon Color Sprite Import/Export Tool");

// Options
var useGreenAsAlphaOption = new Option<bool>("--green-transparency", "Use full green pixel as transparency");
useGreenAsAlphaOption.AddAlias("-g");
var useBmpOption = new Option<bool>("--bmp", "Output as BMP");
var numJogressOption = new Option<uint>("--num-jogress", () => 0, "Number of jogresses");
numJogressOption.AddAlias("-j");
var sheetRowsOption = new Option<uint?>("--sheet-rows", "Number of rows in sprite sheet");
sheetRowsOption.ArgumentHelpName = "row";
sheetRowsOption.AddAlias("-sr");
var sheetColsOption = new Option<uint?>("--sheet-cols", "Number of columns in sprite sheet");
sheetColsOption.ArgumentHelpName = "cols";
sheetColsOption.AddAlias("-sc");
var tortoiseshelOption = new Option<bool>("--tortoiseshel", "Use Tortoiseshel sprite sheet layout");
var hasNameOption = new Option<bool>("--has-name", "Each character has a name sprite at the end");
var hasCutinOption = new Option<bool>("--has-cutin", "Each character has a cut-in");
var omitSpecialCutinOption = new Option<bool>("--omit-special-cutin", "Omit cut-in after special character frames");
var numFramesPerSpecialCharaOption = new Option<uint>("--num-frames-per-special-chara", () => 0, "Number of frames for special characters");
numFramesPerSpecialCharaOption.ArgumentHelpName = "frames";
numFramesPerSpecialCharaOption.AddAlias("-nfs");
var specialCharaIndexesOption = new Option<uint[]>("--special-chara", "List of special character indexes");
specialCharaIndexesOption.ArgumentHelpName = "index";
specialCharaIndexesOption.AddAlias("-sp");
var pendulumNameStartOption = new Option<uint>("--pendulum-name-start", "First name sprite index on Pendulum Color that supports names in album");
pendulumNameStartOption.ArgumentHelpName = "index";
pendulumNameStartOption.AddAlias("-pns");

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

void DoImportSheets(FileInfo romFile, FirmwareInfo fwInfo, DirectoryInfo inDir, FileInfo? outFile, bool useGreenAsAlpha, uint? rows, uint? cols)
{
    bool needCopyOverSrc = outFile == null || outFile.FullName == romFile.FullName; // Not foolproof
    if (needCopyOverSrc) outFile = new FileInfo(Path.GetTempFileName());
    using (var impExp = new ImageImportExport(romFile.OpenRead(), fwInfo))
    {
        impExp.ImportSpriteSheetFolder(inDir.FullName, useGreenAsAlpha, rows, cols);
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
    useGreenAsAlphaOption, useBmpOption, specialCharaIndexesOption, romPathArgument, presetNameArgument, outDirArgument
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
    if (pr.HasOption(specialCharaIndexesOption))
    {
        fwInfo.SpecialCharaIndexes = pr.GetValueForOption(specialCharaIndexesOption)!;
    }

    DoExportSheets(romPath, fwInfo, outDir, useGreenAsAlpha, useBmp);
});

var importSheetsPresetCmd = new Command("import-sheets-preset", "Import character sprite sheets using firmware preset")
{
    useGreenAsAlphaOption, specialCharaIndexesOption, sheetRowsOption, sheetColsOption, tortoiseshelOption,
    romPathArgument, presetNameArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importSheetsPresetCmd);

importSheetsPresetCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var sheetRows = pr.GetValueForOption(sheetRowsOption);
    var sheetCols = pr.GetValueForOption(sheetColsOption);
    var isTortoiseshel = pr.GetValueForOption(tortoiseshelOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var presetName = pr.GetValueForArgument(presetNameArgument);
    var inDir = pr.GetValueForArgument(inDirArgument);
    var outFile = pr.GetValueForArgument(outFileArgument);

    var fwInfo = FirmwareInfo.Presets[presetName];
    if (pr.HasOption(specialCharaIndexesOption))
    {
        fwInfo.SpecialCharaIndexes = pr.GetValueForOption(specialCharaIndexesOption)!;
    }
    if (isTortoiseshel)
    {
        sheetRows = 4;
        sheetCols = 3;
    }

    DoImportSheets(romPath, fwInfo, inDir, outFile, useGreenAsAlpha, sheetRows, sheetCols);
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
    Console.WriteLine($"{nameof(fwInfo.HasName)}: {fwInfo.HasName}");
    Console.WriteLine($"{nameof(fwInfo.HasCutin)}: {fwInfo.HasCutin}");
    Console.WriteLine($"{nameof(fwInfo.OmitSpecialCutin)}: {fwInfo.OmitSpecialCutin}");
    Console.WriteLine($"{nameof(fwInfo.NumFramesPerSpecialChara)}: {fwInfo.NumFramesPerSpecialChara}");
    Console.WriteLine($"{nameof(fwInfo.SpecialCharaIndexes)}: [{string.Join(", ", fwInfo.SpecialCharaIndexes)}]");
    Console.WriteLine($"{nameof(fwInfo.PendulumNameStart)}: {fwInfo.PendulumNameStart}");
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
    useGreenAsAlphaOption, useBmpOption, numJogressOption, hasNameOption, hasCutinOption, omitSpecialCutinOption, numFramesPerSpecialCharaOption,
    specialCharaIndexesOption, pendulumNameStartOption, romPathArgument, outDirArgument, spritePackBaseArgument,
    sizeTableOffsetArgument, numImagesArgument, numCharasArgument, numFramesPerCharaArgument,
    charaStartIndexArgument
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
    var hasName = pr.GetValueForOption(hasNameOption);
    var hasCutin = pr.GetValueForOption(hasCutinOption);
    var omitSpecialCutin = pr.GetValueForOption(omitSpecialCutinOption);
    var numFramesPerSpecialChara = pr.GetValueForOption(numFramesPerSpecialCharaOption);
    var specialCharaIndexes = pr.GetValueForOption(specialCharaIndexesOption);

    var fwInfo = new FirmwareInfo
    {
        SpritePackBase = spritePackBase,
        SizeTableOffset = sizeTableOffset,
        NumImages = numImages,
        NumCharas = numCharas,
        NumFramesPerChara = numFramesPerChara,
        CharasStartIndex = charaStartIndex,
        NumJogressCharas = numJogresses,
        HasName = hasName,
        HasCutin = hasCutin,
        OmitSpecialCutin = omitSpecialCutin,
        NumFramesPerSpecialChara = numFramesPerSpecialChara,
        SpecialCharaIndexes = specialCharaIndexes ?? [],
    };

    if (pr.HasOption(pendulumNameStartOption))
    {
        fwInfo.PendulumNameStart = pr.GetValueForOption(pendulumNameStartOption);
    }

    DoExportSheets(romPath, fwInfo, outDir, useGreenAsAlpha, useBmp);
});

var importSheetsCmd = new Command("import-sheets", "Import character sprite sheets")
{
    useGreenAsAlphaOption, numJogressOption, hasNameOption, hasCutinOption, omitSpecialCutinOption, numFramesPerSpecialCharaOption,
    specialCharaIndexesOption, pendulumNameStartOption, sheetRowsOption, sheetColsOption, tortoiseshelOption,
    romPathArgument, spritePackBaseArgument, sizeTableOffsetArgument, numImagesArgument,
    numCharasArgument, numFramesPerCharaArgument, charaStartIndexArgument, inDirArgument,
    outFileArgument
};
rootCommand.AddCommand(importSheetsCmd);

importSheetsCmd.SetHandler(context =>
{
    var pr = context.ParseResult;
    var useGreenAsAlpha = pr.GetValueForOption(useGreenAsAlphaOption);
    var numJogresses = pr.GetValueForOption(numJogressOption);
    var sheetRows = pr.GetValueForOption(sheetRowsOption);
    var sheetCols = pr.GetValueForOption(sheetColsOption);
    var isTortoiseshel = pr.GetValueForOption(tortoiseshelOption);
    var romPath = pr.GetValueForArgument(romPathArgument);
    var spritePackBase = pr.GetValueForArgument(spritePackBaseArgument);
    var sizeTableOffset = pr.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = pr.GetValueForArgument(numImagesArgument);
    var numCharas = pr.GetValueForArgument(numCharasArgument);
    var numFramesPerChara = pr.GetValueForArgument(numFramesPerCharaArgument);
    var charaStartIndex = pr.GetValueForArgument(charaStartIndexArgument);
    var inDir = pr.GetValueForArgument(inDirArgument);
    var outFile = pr.GetValueForArgument(outFileArgument);
    var hasName = pr.GetValueForOption(hasNameOption);
    var hasCutin = pr.GetValueForOption(hasCutinOption);
    var omitSpecialCutin = pr.GetValueForOption(omitSpecialCutinOption);
    var numFramesPerSpecialChara = pr.GetValueForOption(numFramesPerSpecialCharaOption);
    var specialCharaIndexes = pr.GetValueForOption(specialCharaIndexesOption);

    var fwInfo = new FirmwareInfo
    {
        SpritePackBase = spritePackBase,
        SizeTableOffset = sizeTableOffset,
        NumImages = numImages,
        NumCharas = numCharas,
        NumFramesPerChara = numFramesPerChara,
        CharasStartIndex = charaStartIndex,
        NumJogressCharas = numJogresses,
        HasName = hasName,
        HasCutin = hasCutin,
        OmitSpecialCutin = omitSpecialCutin,
        NumFramesPerSpecialChara = numFramesPerSpecialChara,
        SpecialCharaIndexes = specialCharaIndexes ?? [],
    };

    if (pr.HasOption(pendulumNameStartOption))
    {
        fwInfo.PendulumNameStart = pr.GetValueForOption(pendulumNameStartOption);
    }

    if (isTortoiseshel)
    {
        sheetRows = 4;
        sheetCols = 3;
    }

    DoImportSheets(romPath, fwInfo, inDir, outFile, useGreenAsAlpha, sheetRows, sheetCols);
});
#endregion

return rootCommand.Invoke(args);
