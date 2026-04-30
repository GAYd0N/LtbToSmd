# LtbToSmd
A tool for CrossFire — LTB to SMD model conversion + DTX to PNG/BMP/TGA texture conversion.

## 📦 Download
Grab the latest build from [Releases](https://github.com/GAYd0N/LtbToSmd/releases).  
Requires [.NET 8.0 Runtime](https://aka.ms/get-dotnet-8) (or use the self-contained single-file release).

## 🚀 Features

### LTB2SMD — Model Conversion
- Convert `.ltb` model files to `.smd` (StudioMDL) format
- Split arm meshes into separate files
- Split large SMD files by vertex count
- Extract bone animations as separate `.smd` files
- Generate `.qc` compilation config
- Batch convert entire folders

### DTX2PNG — Texture Conversion
- Convert `.dtx` texture files to **PNG**, **BMP** or **TGA**
- **Indexed BMP** — quantize to 256 colors (8-bit BMP)
- **Auto scaling** — downscale oversized textures to a configurable max edge (default 1024px)
- **Batch convert** — process folders of `.dtx` files
- Supports multiple DTX versions (-2, -3, -5) and pixel formats:
  - 8-bit palette
  - 32-bit BGRA / RGBA
  - DXT1 (BC1), DXT3 (BC2), DXT5 (BC3)

### General
- Drag & drop files/folders onto the window — auto-detects `.ltb` / `.dtx`
- **Auto-scroll** log with toggle
- **Cancel** long-running conversions
- **Language switch** — 中文 / English
- **Auto-create** output folder per session

## 🏗️ Roadmap
| Feature      | Status |
| ------------ | ------ |
| LtbToSmd     | ✅     |
| DtxToPng     | ✅     |
| RezExplorer  | ❓     |

## 📋 Version History

See the [Releases page](https://github.com/GAYd0N/LtbToSmd/releases) for the full changelog.

The current version is defined in `LtbToSmd.csproj` (`<Version>` property) and displayed in the application title bar and About page.

### Release workflow

1. Update the `<Version>` in `LtbToSmd/LtbToSmd.csproj`
2. Run `.\build\publish.ps1` to build and output to `publish/v{version}/`
3. Create a git tag: `git tag v{version}` && `git push origin v{version}`
4. Create a GitHub Release from the tag and upload the published artifacts

## ℹ️ Acknowledgements
This project uses code from:
1. [AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) — cross-platform UI framework
2. [LTB2SMD](https://github.com/giaynhap/LTB2SMD) — original LTB format reference
3. [dtx2png](https://github.com/rholdorf/dtx2png) — DTX parsing reference