## Cewe2pdf

This commandline program converts **CEWE FOTOWELT** `.mcfx` photobook project files to high quality `.pdf` documents.
It's still an early version with probably many missing features, see the [Known Issues](#known-issues) section below, but for photobooks containing images and text it gets the job done.

If you encounter a bug or the generated pdf contains errors please [report an Issue](https://github.com/stfnk/Cewe2pdf/issues). Describe the problem and attach the `cewe2pdf.log` file, located next to the binary.

### `.mcfx` support, 2025
Recently Cewe updated the file format of the Designer Software - now ending in `.mcfx` instead of the old `.mcf`. 
From version 0.4.0 onwards, `Cewe2pdf` only supports these new files. For old, regular `.mcf` files, use [previous releases](https://github.com/stfnk/Cewe2pdf/releases).

#### Some findings on the new format
Opposed to the old version, the new format actually contains the image data itself, images are no longer stored in a separate directory. An mcfx file is basically just a sqlite3 database with a custom file extension. The database consists of 3 columns: `Data`, `Filename` and `LastModified`. For the pdf conversion, only *Data* and *Filename* are relevant. All image files, as well as the old xml-format *mcf* file are stored in this database. The image paths from the xml now point to Data entries in the database and can be accessed by name. 
To support the new fileformat, the old `McfParser` class was refactored to use the in-memory version of the mcf hierarchy, extracted from the database. To access the database, a new [`Mcfx`](https://github.com/stfnk/Cewe2pdf/blob/master/Cewe2pdf/Mcfx.cs) class was introduced that wraps the sqlite commands to retrieve the required data for the conversion.

## Download & Installation

> **Note:** This fork targets **macOS (Apple Silicon / arm64)** only. For Windows, see the [original project](https://github.com/stfnk/Cewe2pdf).

### 1. Install CEWE Fotowelt

Download and install **CEWE Fotowelt** for macOS:
https://www.cewe.de/bestellsoftware/danke.html?keyAccount=24441&product=391

After installation it should be at `/Applications/CEWE Fotowelt.app`.

### 2. Install prerequisites

```bash
brew install dotnet mono-libgdiplus webp
```

### 3. Build from source

```bash
git clone https://github.com/agu5a/Cewe2pdf.git
cd Cewe2pdf/Cewe2pdf
dotnet build
```

The binary ends up at `bin/Debug/net10.0/osx-arm64/Cewe2pdf`.
A `config.txt` with the default CEWE path is copied there automatically.

### config.txt

`config.txt` is copied next to the binary on every build. Edit it if your CEWE installation is in a different location:

```text
program_path=/Applications/CEWE Fotowelt.app/Contents
```

## Usage

```bash
cd Cewe2pdf/bin/Debug/net10.0/osx-arm64
./Cewe2pdf "/path/to/photobook.mcfx"
```

Output PDF is written next to the input file as `photobook-converted.pdf`. To specify a custom output path:

```bash
./Cewe2pdf "/path/to/photobook.mcfx" "/path/to/output.pdf"
```

List all options:

```bash
./Cewe2pdf --help
```

### Known Issues
_Currently only the following features are supported:_
* Images
* Text Boxes
* Image Borders

Please report missing elements [here](https://github.com/stfnk/Cewe2pdf/issues) and attach the `cewe2pdf.log` file located next to the binary.

## Development
This program is written in C# for the .NET 10.0 runtime. It uses [iTextSharp 5](https://github.com/itext/itextsharp/) for pdf rendering, and [SkiaSharp](https://github.com/mono/SkiaSharp) for image loading, resizing and EXIF correction.
`mcfx` files are sqlite3 databases, queried using the `Microsoft.Data.Sqlite` API.
`.mcf` files are plain XML, parsed using the C# native `System.Xml` API.
Photobook backgrounds (`.webp`, `.jpg`) are loaded directly via SkiaSharp — no external WebP wrapper needed.

## Releases

**Changelog**

```text
v0.4.1-alpha2 (macOS fork)
[added] macOS arm64 support — targets osx-arm64, requires Homebrew dotnet + mono-libgdiplus + webp
[changed] replaced System.Drawing.Common with SkiaSharp for cross-platform image loading, resizing and EXIF correction
[changed] replaced System.Data.SQLite with Microsoft.Data.Sqlite (no native interop DLL required)
[changed] replaced Windows-only WebP wrapper with SkiaSharp native WebP decoding
[fixed] hardcoded Windows path separators in font/background directory lookups
[fixed] skip Windows-only ProgramData and C:\Windows\Fonts paths on macOS

v0.4.0
[added] support new .mcfx file format, old mcf files are no longer supported, use an older release for those
[fixed] improve text handling to extract properly from the embedded html - introduced in a newer Cewe version
[changed] cleaned up some internal design id handling, instead of resorting to a predefined resource list, scan installation directory on demand

v0.3.0
[added] new background system, loading color directly from .webp files
[fixed] multiline text with same color, did not inherit color from previous lines

v0.2.0
[added] support for page numbers, including font, color & size
[added] support for colored, bold, italic, underlined text
[fixed] keep images resolution consistent

v0.1.0 Initial Release.
* images
* image borders
* background colors
* basic text boxes
```

**Roadmap**
* more configuration options

## License
The Code in this repository is licensed under the MIT License, but note that `iTextSharp` uses the AGPL License.

```text
MIT License

Copyright (C) 2025 Stefan Kreller

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```
