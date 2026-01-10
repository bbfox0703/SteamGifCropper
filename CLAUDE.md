# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SteamGifCropper is a .NET 8 Windows Forms application designed to process GIF files for Steam Workshop Personal Showcase. It provides extensive GIF manipulation capabilities including cropping, resizing, merging, concatenating, and applying effects.

**Target Platform:** Windows 10 1904+ with .NET 8 runtime
**Primary Language:** C# (ImplicitUsings disabled, Nullable disabled)
**Main Dependencies:** Magick.NET-Q8 (ImageMagick), FFMpegCore
**External Tools:** FFmpeg (optional, must be in PATH), gifsicle (optional, must be in PATH)

## Build and Development Commands

### Build the Solution
```bash
# Build in Debug mode (default)
dotnet build SteamGifCropper.sln

# Build in Release mode for x64
dotnet build SteamGifCropper.sln -c Release -p:Platform=x64

# Clean and rebuild
dotnet clean SteamGifCropper.sln
dotnet build SteamGifCropper.sln
```

### Run the Application
```bash
# Run from project directory
dotnet run --project SteamGifCropper.csproj

# Run with resource limit overrides
dotnet run --project SteamGifCropper.csproj -- --memory-limit=2048 --disk-limit=4096
```

### Testing
```bash
# Run all tests
dotnet test SteamGifCropper.Tests/SteamGifCropper.Tests.csproj

# Run tests with detailed output
dotnet test SteamGifCropper.Tests/SteamGifCropper.Tests.csproj -v n

# Run a specific test class
dotnet test --filter FullyQualifiedName~GifProcessorTests

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

**Test Framework:** xUnit
**Test Project:** `SteamGifCropper.Tests/` uses file linking to include classes under test

## Architecture Overview

### Entry Point and Initialization (`Program.cs`)

The application follows a specific initialization sequence:

1. **Resource Limits Configuration** - Configures ImageMagick memory/disk limits from `App.config` or CLI args
   - Default: 4096 MB memory, 8192 MB disk
   - Override via `--memory-limit=<MB>` and `--disk-limit=<MB>` command-line arguments

2. **OpenCL GPU Acceleration** - Tests and enables OpenCL for GPU-accelerated image processing
   - Automatic device benchmarking on first use
   - Graceful fallback if unavailable

3. **Localization** - Auto-detects OS language and sets `CultureInfo`
   - Supported: English (default), Traditional Chinese (zh-TW), Japanese (ja)

4. **Modern UI Setup** - Configures .NET 8 Windows Forms high DPI support and theming

5. **Launch Main Form** - `GifToolMainForm` is the central UI hub

### Core Components

#### GifProcessor (Static Processing Engine)
- **Location:** `GifProcessor.cs` (~3200+ lines)
- **Pattern:** Static class - all methods accept `GifToolMainForm` parameter for UI updates
- **Responsibilities:**
  - All GIF manipulation operations (crop, resize, merge, concatenate, overlay, scroll, etc.)
  - Frame-by-frame processing with progress reporting
  - Memory management and resource limit enforcement
  - Integration with Magick.NET (ImageMagick)
- **Key Operations:**
  - Steam-specific splitting: 766px/774px wide GIFs → 5 parts with 100px height extension
  - Tail byte modification: Changes last byte from `0x3B` to `0x21` for Steam compatibility
  - Transition effects for concatenation (fade, slide, zoom, dissolve)
  - Palette optimization and quantization

#### GifToolMainForm (Main UI)
- **Location:** `GTMainForm.cs`
- **Responsibilities:**
  - Central hub for all GIF processing operations
  - Manages UI state, progress bars, status text
  - Dynamic theme switching (Windows dark/light mode detection)
  - Multi-language UI updates
  - Provides buttons for 14+ different GIF processing operations

#### Specialized Dialogs (7 dialogs)
Each dialog handles one specific operation type:
- `Mp4ToGifDialog` - MP4 to GIF conversion with time controls
- `MergeGifsDialog` - Merge 2-5 GIFs side-by-side
- `MergeFiveGifsDialog` - Merge and split 5 GIFs (Steam showcase format)
- `ConcatenateGifsDialog` - Concatenate GIFs with transition effects
- `OverlayGifDialog` - Overlay one GIF onto another
- `ResizeNfpsGifDialog` - Resize and change FPS
- `ScrollStaticImageDialog` - Create scrolling animations

### Magick.NET (ImageMagick) Integration

**Core Pattern:**
```
GifProcessor → MagickImageCollection → MagickImage (per frame)
```

**Key Operations:**
- **Loading:** `MagickImageCollection` loads animated GIFs, `.Coalesce()` normalizes frames
- **Frame Manipulation:** Crop via `MagickGeometry`, resize, composite with `CompositeOperator.Over`
- **Scrolling:** `.Roll(offsetX, offsetY)` for wrap-around scrolling effects
- **Palette:** Quantization to 256 colors, dithering options (ro64, o8, default), `.Optimize()`
- **GIF Writing:** LZW compression, transparency optimization, custom defines via `GifWriteDefines`
- **Animation Timing:** Uses `AnimationDelay` and `AnimationTicksPerSecond` properties

**Resource Management:**
- Configurable memory/disk limits via `ResourceLimits` class
- OpenCL GPU acceleration when available
- Progress reporting throttled to every 10 frames
- Explicit disposal patterns with `using` statements

### FFmpeg Integration

**Wrapper:** FFMpegCore NuGet package (v5.4.0)

**Configuration:** Via `App.config`:
- `FFmpeg.TimeoutSeconds` - Default: 300 seconds
- `FFmpeg.Threads` - Default: 0 (auto-detect)

**Use Cases:**
1. **MP4 to GIF Conversion** - Extracts time segments with start/duration controls
2. **GIF Reversal** - Uses `-vf reverse` filter (fallback to ImageMagick if unavailable)

**Requirements:** FFmpeg must be installed and available in system PATH

**Error Handling:** Detailed error messages with FFmpeg stderr output saved to `ffmpeg_error.log`

### Gifsicle Integration

**Wrapper:** `GifsicleWrapper.cs` - Clean, testable wrapper with dependency injection

**Features:**
- Colors: 1-256 palette reduction
- Lossy compression: 0-200 factor
- Optimization levels: 1-3
- Dithering: None, ro64, o8, or default
- Timeout: 30 seconds with cancellation support

**Requirements:** `gifsicle.exe` must be in system PATH

**Usage:** Optional post-processing step after ImageMagick operations

### Configuration System

**File:** `App.config`

**Key Settings:**
```xml
<appSettings>
  <add key="ResourceLimits.MemoryMB" value="4096" />
  <add key="ResourceLimits.DiskMB" value="8192" />
  <add key="FFmpeg.TimeoutSeconds" value="300" />
  <add key="FFmpeg.Threads" value="0" />
</appSettings>
```

**Access Pattern:** Uses `ConfigurationManager.AppSettings[key]` with safe parsing and fallback defaults

**DPI Settings:** Configured in `System.Windows.Forms.ApplicationConfigurationSection` for PerMonitorV2 support

### Multi-Language Support

**Architecture:** .NET Resource-based localization

**Resource Files:**
- `Properties/Resources.resx` - English (default/fallback)
- `Properties/Resources.zh-TW.resx` - Traditional Chinese
- `Properties/Resources.ja.resx` - Japanese
- Dialog-specific resource files for complex forms

**Runtime Switching:**
- Language menu in main form
- Updates `Thread.CurrentThread.CurrentUICulture`
- Refreshes all UI controls via `UpdateUIText()` method
- Uses `ComponentResourceManager` for form controls

**Access:** Strongly-typed via `SteamGifCropper.Properties.Resources.{ResourceName}`

### Theme Support

**Implementation:** `WindowsThemeManager.cs`

**Features:**
- Registry-based detection of Windows dark mode preference
- Windows API integration via P/Invoke (`DwmSetWindowAttribute`)
- Automatic theme switching on system preference changes
- Modern `TaskDialog` for theme-aware message boxes (Windows 10+)
- Fallback to standard `MessageBox` on older systems

**Pattern:** Accepts `IRegistryProvider` interface for testability

## Steam-Specific Format Details

### Supported GIF Widths
- **766px** - Standard format (150px per part, 4px gaps)
- **774px** - Alternative format (150px per part, 6px gaps)

### 766px Cropping Ranges
| Part   | X Coordinate Range |
|--------|-------------------|
| Part 1 | 0 - 149           |
| Part 2 | 153 - 303         |
| Part 3 | 307 - 457         |
| Part 4 | 461 - 611         |
| Part 5 | 615 - end         |

### 774px Cropping Ranges
| Part   | X Coordinate Range |
|--------|-------------------|
| Part 1 | 0 - 149           |
| Part 2 | 155 - 305         |
| Part 3 | 311 - 461         |
| Part 4 | 467 - 617         |
| Part 5 | 623 - end         |

### Special Processing
- Each part gets **100px transparent height extension** at bottom
- GIF tail byte modified: `0x3B` → `0x21` for Steam compatibility
- Header bytes 8-9 adjusted for height changes
- Each output file must be ≤ 5MB for Steam upload

## Important Patterns and Conventions

### Progress Reporting
All long-running operations use tuple-based progress:
```csharp
IProgress<(int current, int total, string status)>
```
- Updates throttled to every 10 frames to reduce UI overhead
- Thread-safe with `Invoke`/`BeginInvoke` patterns

### Async/Await Pattern
- All long-running operations are async to keep UI responsive
- `Application.DoEvents()` used in synchronous sections
- Cancellation token support for FFmpeg operations

### Error Handling Strategy
- Try-catch at UI boundaries (button click handlers)
- Graceful fallbacks (e.g., FFmpeg → ImageMagick for GIF reversal)
- User-friendly error messages from localized resources
- Detailed diagnostics saved to log files

### Memory Management for Large GIFs
- Resource limits configured at startup
- Frame-by-frame processing where possible
- Explicit `using` statements for disposal
- Explicit GC calls before memory-intensive operations

### Testability
- `GifsicleWrapper` uses delegate injection for process execution
- `WindowsThemeManager` accepts `IRegistryProvider` interface
- Test project uses file linking to include classes under test
- Separation of UI and processing logic

## Project Structure

```
SteamGifCropper/
├── Program.cs                     # Entry point & initialization
├── GTMainForm.cs                  # Main UI form
├── GifProcessor.cs                # Core processing engine (3200+ lines)
├── GifsicleWrapper.cs            # Gifsicle integration
├── TransitionGenerator.cs         # Transition effects
├── WindowsThemeManager.cs         # Theme support
├── GifWriteDefines.cs            # Custom GIF write settings
├── [7 Dialog Files]              # Specialized operation dialogs
├── App.config                     # Application configuration
├── Properties/                    # Resources and settings
│   ├── Resources.resx            # English strings
│   ├── Resources.zh-TW.resx      # Traditional Chinese
│   └── Resources.ja.resx         # Japanese
└── SteamGifCropper.Tests/        # xUnit test project
    ├── GifProcessorTests.cs
    ├── GifsicleWrapperTests.cs
    └── TestData/                 # Test GIF files
```

## Development Notes

### Platform Constraints
- **Windows-only** - Uses Windows Forms, Windows API for theming, and Windows registry
- **x64 architecture** - Project configured for x64 platform
- **No implicit usings** - All namespaces must be explicitly declared
- **Nullable disabled** - No nullable reference type annotations

### Code Style
- Static `GifProcessor` class for all processing operations
- Flat namespace structure (all classes in `SteamGifCropper` namespace)
- Extensive use of `async`/`await` for UI responsiveness
- Resource strings for all user-facing text (localization)

### Testing Approach
- xUnit framework
- Test data in `SteamGifCropper.Tests/TestData/`
- Stubs for main components (`GifProcessor.Stub.cs`, `GifToolMainForm.Stub.cs`)
- File linking from main project to avoid duplication

### External Dependencies
- **Required:** Magick.NET-Q8 (included in release)
- **Optional:** FFmpeg (user must install via `winget install ffmpeg`)
- **Optional:** gifsicle for Windows (user must download and add to PATH)
