// See https://aka.ms/new-console-template for more information

using DigimonColorSpriteTool;
using System.CommandLine;

const int DEFAULT_NUM_CHARAS = 18;

var rootCommand = new RootCommand("Digimon Color Sprite Import/Export Tool");

// Options
var useGreenAsAlphaOption = new Option<bool>("--green-transparency", "Use full green pixel as transparency");
useGreenAsAlphaOption.AddAlias("-g");
var useBmpOption = new Option<bool>("--bmp", "Output as BMP");
var numCharasOption = new Option<int>("--num-charas", () => DEFAULT_NUM_CHARAS, "Number of characters");
numCharasOption.AddAlias("-c");
var numJogressOption = new Option<int>("--num-jogress", () => 0, "Number of jogresses");
numJogressOption.AddAlias("-j");

// Arguments
var romPathArgument = new Argument<FileInfo>("romPath", "Path to flash dump").ExistingOnly();
var sizeTableOffsetArgument = new Argument<int>("sizeTableOffset", "Size table offset");
var numImagesArgument = new Argument<int>("numImages", "Number of images in ROM");
var outDirArgument = new Argument<DirectoryInfo>("outDir", "Output directory").LegalFilePathsOnly();
var outFileArgument = new Argument<FileInfo?>("outFile", () => null, "Output file").LegalFilePathsOnly();
var inDirArgument = new Argument<DirectoryInfo>("inDir", "Input directory").ExistingOnly();
var charaStartIndexArgument = new Argument<int>("charaStartIndex", "Index of first character sprite");

var exportCmd = new Command("export", "Export all sprites")
{
    useGreenAsAlphaOption, useBmpOption, romPathArgument,  sizeTableOffsetArgument, numImagesArgument, outDirArgument
};
rootCommand.AddCommand(exportCmd);

exportCmd.SetHandler((useGreenAsAlpha, useBmp, romPath, sizeTableOffset, numImages, outDir) =>
{
    if (useBmp) useGreenAsAlpha = true;
    using var impExp = new ImageImportExport(romPath.OpenRead(), sizeTableOffset, numImages);
    outDir.Create();
    impExp.ExportAllImages(outDir.FullName, useBmp ? ".bmp" : ".png", useGreenAsAlpha);
}, useGreenAsAlphaOption, useBmpOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, outDirArgument);

var importCmd = new Command("import", "Import sprites")
{
    useGreenAsAlphaOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importCmd);

importCmd.SetHandler((useGreenAsAlpha, romPath, sizeTableOffset, numImages, inDir, outFile) =>
{
    bool needCopyOverSrc = outFile == null || outFile.FullName == romPath.FullName; // Not foolproof
    if (needCopyOverSrc) outFile = new FileInfo(Path.GetTempFileName());
    using (var impExp = new ImageImportExport(romPath.OpenRead(), sizeTableOffset, numImages))
    {
        impExp.SetOverridesByFolder(inDir.FullName);
        impExp.Rebuild(outFile.FullName, useGreenAsAlpha);
    }
    if (needCopyOverSrc)
    {
        outFile.CopyTo(romPath.FullName, true);
        outFile.Delete();
    }
}, useGreenAsAlphaOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, inDirArgument, outFileArgument);

var exportSheetsCmd = new Command("export-sheets", "Export all character sprite sheets")
{
    useGreenAsAlphaOption, useBmpOption, numCharasOption, numJogressOption, romPathArgument,  sizeTableOffsetArgument, numImagesArgument, charaStartIndexArgument, outDirArgument
};
rootCommand.Add(exportSheetsCmd);

exportSheetsCmd.SetHandler(context =>
{
    var useGreenAsAlpha = context.ParseResult.GetValueForOption(useGreenAsAlphaOption);
    var useBmp = context.ParseResult.GetValueForOption(useBmpOption);
    var numCharas = context.ParseResult.GetValueForOption(numCharasOption);
    var numJogresses = context.ParseResult.GetValueForOption(numJogressOption);
    var romPath = context.ParseResult.GetValueForArgument(romPathArgument);
    var sizeTableOffset = context.ParseResult.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = context.ParseResult.GetValueForArgument(numImagesArgument);
    var charaStartIndex = context.ParseResult.GetValueForArgument(charaStartIndexArgument);
    var outDir = context.ParseResult.GetValueForArgument(outDirArgument);

    if (useBmp) useGreenAsAlpha = true;
    using var impExp = new ImageImportExport(romPath.OpenRead(), sizeTableOffset, numImages);
    outDir.Create();
    impExp.ExportSpriteSheetFolder(outDir.FullName, useBmp ? ".bmp" : ".png", charaStartIndex, numCharas, useGreenAsAlpha, numJogresses);
});

var importSheetsCmd = new Command("import-sheets", "Import character sprite sheets")
{
    useGreenAsAlphaOption, numCharasOption, numJogressOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, charaStartIndexArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importSheetsCmd);

importSheetsCmd.SetHandler(context =>
{
    var useGreenAsAlpha = context.ParseResult.GetValueForOption(useGreenAsAlphaOption);
    var useBmp = context.ParseResult.GetValueForOption(useBmpOption);
    var numCharas = context.ParseResult.GetValueForOption(numCharasOption);
    var numJogresses = context.ParseResult.GetValueForOption(numJogressOption);
    var romPath = context.ParseResult.GetValueForArgument(romPathArgument);
    var sizeTableOffset = context.ParseResult.GetValueForArgument(sizeTableOffsetArgument);
    var numImages = context.ParseResult.GetValueForArgument(numImagesArgument);
    var charaStartIndex = context.ParseResult.GetValueForArgument(charaStartIndexArgument);
    var inDir = context.ParseResult.GetValueForArgument(inDirArgument);
    var outFile = context.ParseResult.GetValueForArgument(outFileArgument);

    bool needCopyOverSrc = outFile == null || outFile.FullName == romPath.FullName; // Not foolproof
    if (needCopyOverSrc) outFile = new FileInfo(Path.GetTempFileName());
    using (var impExp = new ImageImportExport(romPath.OpenRead(), sizeTableOffset, numImages))
    {
        impExp.ImportSpriteSheetFolder(inDir.FullName, charaStartIndex, numCharas, useGreenAsAlpha, numJogresses);
        impExp.Rebuild(outFile.FullName, useGreenAsAlpha);
    }
    if (needCopyOverSrc)
    {
        outFile.CopyTo(romPath.FullName, true);
        outFile.Delete();
    }
});

return rootCommand.Invoke(args);
