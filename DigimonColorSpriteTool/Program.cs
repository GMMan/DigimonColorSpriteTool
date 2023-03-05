// See https://aka.ms/new-console-template for more information

using DigimonColorSpriteTool;
using System.CommandLine;

const int NUM_CHARAS = 18;

var rootCommand = new RootCommand("Digimon Color Sprite Import/Export Tool");

// Options
var useGreenAsAlphaOption = new Option<bool>("--green-transparency", "Use full green pixel as transparency");
useGreenAsAlphaOption.AddAlias("-g");
var useBmpOption = new Option<bool>("--bmp", "Output as BMP");

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
    useGreenAsAlphaOption, useBmpOption, romPathArgument,  sizeTableOffsetArgument, numImagesArgument, charaStartIndexArgument, outDirArgument
};
rootCommand.Add(exportSheetsCmd);

exportSheetsCmd.SetHandler((useGreenAsAlpha, useBmp, romPath, sizeTableOffset, numImages, charaStartIndex, outDir) =>
{
    if (useBmp) useGreenAsAlpha = true;
    using var impExp = new ImageImportExport(romPath.OpenRead(), sizeTableOffset, numImages);
    outDir.Create();
    impExp.ExportSpriteSheetFolder(outDir.FullName, useBmp ? ".bmp" : ".png", charaStartIndex, NUM_CHARAS, useGreenAsAlpha);
}, useGreenAsAlphaOption, useBmpOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, charaStartIndexArgument, outDirArgument);

var importSheetsCmd = new Command("import-sheets", "Import character sprite sheets")
{
    useGreenAsAlphaOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, charaStartIndexArgument, inDirArgument, outFileArgument
};
rootCommand.AddCommand(importSheetsCmd);

importSheetsCmd.SetHandler((useGreenAsAlpha, romPath, sizeTableOffset, numImages, charaStartIndex, inDir, outFile) =>
{
    bool needCopyOverSrc = outFile == null || outFile.FullName == romPath.FullName; // Not foolproof
    if (needCopyOverSrc) outFile = new FileInfo(Path.GetTempFileName());
    using (var impExp = new ImageImportExport(romPath.OpenRead(), sizeTableOffset, numImages))
    {
        impExp.ImportSpriteSheetFolder(inDir.FullName, charaStartIndex, NUM_CHARAS, useGreenAsAlpha);
        impExp.Rebuild(outFile.FullName, useGreenAsAlpha);
    }
    if (needCopyOverSrc)
    {
        outFile.CopyTo(romPath.FullName, true);
        outFile.Delete();
    }
}, useGreenAsAlphaOption, romPathArgument, sizeTableOffsetArgument, numImagesArgument, charaStartIndexArgument, inDirArgument, outFileArgument);

return rootCommand.Invoke(args);
