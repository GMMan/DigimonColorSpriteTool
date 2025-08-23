Digimon Color Sprite Import/Export Tool
=======================================

This tool allows you to easily extract and reimport sprites for Digimon Color
and Pendulum Color. It also supports exporting/importing as sprite sheets for
character sprites, and also accepts sprites with different dimensions than the
original.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/C0C81P4PX)

Usage
-----

### Export sprites

```
DigimonColorSpriteTool.exe export-preset [options] <romPath> <version> <outDir>
```

Where:
- `romPath`: Path to flash dump
- `version`: Flash dump version, one of `dmc1`, `dmc2`, `dmc3`, `dmc4`, `dmc5`, `dmcmh`, `dmcgz`
  for Digimon Color versions, and `penc1`, `penc2`, `penc3`, `penc4`, `penc5`,
  `penc0` for Pendulum Color versions
- `outDir`: The directory to export into

Files are exported as PNG files into `outDir` with transparent background.
You can export with green background by using the `-g` option, and to BMP with
the `--bmp` option.

### Import sprites

```
DigimonColorSpriteTool.exe import-preset [options] <romPath> <version> <inDir> [<outFile>]
```

`inDir` is a path to the folder that contains the sprites to import. You
only need to provide the sprites you want to overwrite, named with the correct
index. `outFile` is the path to the repacked ROM file. If omitted, the
original file will be overwritten. Use the `-g` option if your backgrounds are
green. Otherwise, any pure green pixels will be slightly adjusted so they show
up as green on-device. BMPs will be imported with the `-g` option applied.

### Export sprite sheet

```
DigimonColorSpriteTool.exe export-sheets-preset [options] <romPath> <version> <outDir>
```

Sprite sheets consist of a vertical column of character sprites. You can edit
and reimport sheets. Same options as previous apply. If the characters have name
sprites, it will be stored under the same name and extension as the sprite sheet
except with `_name` appended to the name part, i.e. `4.png` would have a name
counterpart named `4_name.png`.


### Import sprite sheet

```
DigimonColorSpriteTool.exe import-sheets-preset [options] <romPath> <version> <inDir> [<outFile>]
```

Additional options:
- `-g`: Same as before
- `-sr`: Number of rows in sprite sheet
- `-sc`: Number of columns in sprite sheet
- `--tortoiseshel`: Sets columns and rows to 3x4

When importing sprite sheets, by default it uses 1 column and the same number of
rows as there are sprite frames per character. You can change this with `-sr`
and `-sc` options. Note that there needs to be enough frames for each character,
and each frame must be smaller or equal to 48x48, and can be scaled up to 48x48
by whole multiples. For example, 16x16 frames can be scaled up, but 20x20 frames
cannot be, and 64x64 frames are too large. All sheets in the folder must use the
same layout. You can omit sheets for characters that you do not want to replace.
If name sprites are supported, make sure they are named in the way described in
the previous section. If the name sprite is omitted or improperly named, it will
not be updated in the resulting file.

### Special characters

Certain Digimon Color variants may have characters with additional sprites.
These are designated as "special characters" and their indexes may be specified
with one or more `-sp` options followed by the index. For example, if index 0,
2, and 9 are special, you would add the options `-sp 0 -sp 2 -sp 9`. This option
is available for sprite sheet related commands: `export-sheets-preset`,
`import-sheets-preset`, `export-sheets`, `import-sheets`.

### Cut-ins

For devices that support cut-ins, they are placed after regular sprites and
also after special sprites. If the character is not special, the exported name
for sprite sheets will have the character index plus `_cutin` appended for the
name. If the character is special, the above applies, plus an additional
0-based index is added to the name, which corresponds to its location within
the sprite order.

Example: for a special character with index `0`, you will get `0.png`,
`0_cutin0.png`, and `0_cutin1.png`.

When counting number of sprites for specifying on the command line, do not
include cut-ins or names in the count.

### Non-preset commands

Each command is also available in non-preset versions. See usage help in-program
for the commands and their arguments. An explanation of the arguments is as
follows:

- `spritePackBase`: Flash offset of the beginning of the sprite data. It is
  `524288` for DMC, and `4194304` for PenC.
- `sizeTableOffset`: Flash offset of the sprite sizes table
- `numImages`: Total number of images in sprite data
- `numCharas`: Number of characters in sprite data. This is the number of full
  sprite frame sets, not including single-frame jogress characters
- `numFramesPerChara`: Number of frames per character. `15` for DMC, `12` for
  PenC
- `charaStartIndex`: Start index of character sprites. `210` for DMC, `240` for
  PenC
- `-j`: Number of jogress characters. If jogress character images are
  interspersed before full set characters, indicate the number of such jogress
  characters. Do not include jogress characters after full set characters
- `--has-name`: Each character has a name sprite following the character frames
- `-nfs`: Number of frames for special characters
- `-sp`: Specifies the index of a special character. Can be repeated multiple
  times

You can print the values for those arguments from presets using the
`show-preset` command:

```
DigimonColorSpriteTool.exe show-preset <version>
```

For special character indexes, specify each separately using the `-sp` option.
