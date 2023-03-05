Digimon Color Sprite Import/Export Tool
=======================================

This tool allows you to easily extract and reimport sprites for Digimon Color.
It also supports exporting/importing as sprite sheets for character sprites,
and also accepts sprites with different dimensions than the original.

Usage
-----

### Export sprites

- For V1: `DigimonColorSpriteTool.exe export v1_dump.bin 38296 597 export_dir`
- For V2: `DigimonColorSpriteTool.exe export v2_dump.bin 40346 597 export_dir`

Files are exported as PNG files into `export_dir` with transparent background.
You can export with green background by using the `-g` option, and to BMP with
the `--bmp` option.

### Inport sprites

- For V1: `DigimonColorSpriteTool.exe import v1_dump.bin 38296 597 import_dir [new_dump_path]`
- For V2: `DigimonColorSpriteTool.exe import v2_dump.bin 40346 597 import_dir [new_dump_path]`

`import_dir` is a path to the folder that contains the sprites to import. You
only need to provide the sprites you want to overwrite, named with the correct
index. `new_dump_path` is the path to the repacked ROM file. If omitted, the
original file will be overwritten. Use the `-g` option if your backgrounds are
green. Otherwise, any pure green pixels will be slightly adjusted so they show
up as green on-device. BMPs will be imported with the `-g` option applied.

### Export sprite sheet

Sprite sheets consist of a vertical column of character sprites. You can edit
and reimport sheets. Do not change the dimension of the sheet or it will not
reimport. Same options as previous apply.

- For V1: `DigimonColorSpriteTool.exe export-sheets v1_dump.bin 38296 597 210 export_dir`
- For V2: `DigimonColorSpriteTool.exe export-sheets v2_dump.bin 40346 597 211 export_dir`

### Import sprite sheet

- For V1: `DigimonColorSpriteTool.exe import-sheets v1_dump.bin 38296 597 210 import_dir [new_dump_path]`
- For V2: `DigimonColorSpriteTool.exe import-sheets v2_dump.bin 40346 597 211 import_dir [new_dump_path]`
