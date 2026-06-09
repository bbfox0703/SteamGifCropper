# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SteamGifCropper is a .NET 10 Windows Forms application designed to process GIF files for Steam Workshop Personal Showcase. It provides extensive GIF manipulation capabilities including cropping, resizing, merging, concatenating, and applying effects.

**Target Platform:** Windows 10 1904+ with .NET 10 runtime (x64)
**Primary Language:** C# (ImplicitUsings disabled, Nullable disabled)
**Root Namespace:** `GifProcessorApp` (legacy — the assembly name is `SteamGifCropper` but all source files declare `namespace GifProcessorApp`; `ImageInputValidator.cs` is the lone exception, declaring `namespace SteamGifCropper`)
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
```powershell
# Build the test project
dotnet build SteamGifCropper.Tests/SteamGifCropper.Tests.csproj

# Run all tests via the xUnit v3 embedded runner
& .\SteamGifCropper.Tests\bin\Debug\net10.0-windows\SteamGifCropper.Tests.exe

# Filter by class or method (xUnit v3 runner syntax)
& .\SteamGifCropper.Tests\bin\Debug\net10.0-windows\SteamGifCropper.Tests.exe -class GifProcessorTests

# Machine-readable output (useful from scripts/CI)
& .\SteamGifCropper.Tests\bin\Debug\net10.0-windows\SteamGifCropper.Tests.exe -automated
```

> **Note:** `dotnet test` against this project on the .NET 10 SDK currently emits
> `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`.
> Invoking the produced `.exe` directly is the supported path until the test runner config is migrated.

**Test Framework:** xUnit v3 on Microsoft.Testing.Platform
**Test Project:** `SteamGifCropper.Tests/` uses file linking (`<Compile Include="..\src\Core\*.cs">`) to include classes under test

## Architecture Overview

### Entry Point and Initialization (`src/Program.cs`)

The application follows a specific initialization sequence:

1. **Resource Limits Configuration** - Configures ImageMagick memory/disk limits from `App.config` or CLI args
   - Default: 4096 MB memory, 8192 MB disk
   - Override via `--memory-limit=<MB>` and `--disk-limit=<MB>` command-line arguments

2. **OpenCL GPU Acceleration** - Tests and enables OpenCL for GPU-accelerated image processing
   - Automatic device benchmarking on first use
   - Graceful fallback if unavailable

3. **Localization** - Auto-detects OS language and sets `CultureInfo`
   - Supported: English (default), Traditional Chinese (zh-TW), Japanese (ja)

4. **Modern UI Setup** - Configures .NET 10 Windows Forms high DPI support and theming (DPI mode set via `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` in the csproj, not in `app.manifest`)

5. **Launch Main Form** - `GifToolMainForm` is the central UI hub

### Core Components

#### GifProcessor (Static Processing Engine)
- **Location:** `src/Core/GifProcessor.cs` (~3200+ lines)
- **Pattern:** Static class - all methods accept `GifToolMainForm` parameter for UI updates
- **Responsibilities:**
  - All GIF manipulation operations (crop, resize, merge, concatenate, overlay, scroll, etc.)
  - Frame-by-frame processing with progress reporting
  - Memory management and resource limit enforcement
  - Integration with Magick.NET (ImageMagick)
- **Key Operations:**
  - Steam-specific splitting: 766px/774px wide GIFs → 5 parts with 100px height extension
  - Tail byte modification: Changes last byte from `0x3B` to `0x21` for Steam compatibility
  - Dynamic (overlap) transition effects for concatenation: fade, cross-fade, slide, zoom, iris, wipe, dip-to-black, blur dissolve, dissolve, ripple (both clips keep playing through the transition; the overlap shortens the join)
  - Palette optimization and quantization

#### GifToolMainForm (Main UI)
- **Location:** `src/Forms/GTMainForm.cs`
- **Responsibilities:**
  - Central hub for all GIF processing operations
  - Manages UI state, progress bars, status text
  - Dynamic theme switching (Windows dark/light mode detection)
  - Multi-language UI updates
  - Provides buttons for 20+ different GIF processing operations

#### Specialized Dialogs
Each dialog handles one specific operation type:
- `Mp4ToGifDialog` - MP4 to GIF conversion with time controls
- `MergeGifsDialog` - Merge 2-5 GIFs side-by-side
- `MergeFiveGifsDialog` - Merge and split 5 GIFs (Steam showcase format)
- `ConcatenateGifsDialog` - Concatenate GIFs with transition effects (transition type chosen via a single ComboBox; localized family names fetched via `ResourceManager.GetString`, so no Designer entries are needed for them)
- `OverlayGifDialog` - Overlay one GIF onto another
- `ResizeNfpsGifDialog` - Resize and change FPS
- `ScrollStaticImageDialog` - Create scrolling animations
- `GridMosaicDialog` - Slot-aligned grid/mosaic overlay (766px)
- `SlotMachineDialog` - 5-reel slot machine (image / GIF)
- `QuicksandDialog` - Horizontal/vertical viscous band flow (image / GIF)
- `RippleDialog` - Water ripple: up to 3 interfering drops (image / GIF)
- `RippleDropPickerForm` - Click-to-pick a ripple drop position on frame 0
- `WindDialog` - Wind sway (風吹麥田): travelling-wave displacement, normal/nuclear modes (image / GIF)
- `RainDialog` - Rain overlay: translucent slanted streaks, wind + "rain stops" fade-out (image / GIF)
- `MorphTransitionDialog` - A→B morph transition (raindrop reveal / tile flip / spotlight / jigsaw) with a pre-roll + remaining-B timeline

> **Creative-effects detail:** these "766px single-output, chainable" effects (grid mosaic, slot
> machine, quicksand, ripple, wind, rain, A→B morph) — their ideas and completion status live in
> `docs/CreativeFeatureIdeas.md`; the detailed implementation/handoff dev log (file lists, commits,
> semantics, gotchas) lives in `docs/CreativeEffectsDevLog.md`.
> Their math is extracted into pure, dependency-free files for unit testing — `SlotMachineGeometry.cs`,
> `QuicksandGeometry.cs`, `GifEffectWindow.cs`, `GridMosaicGeometry.cs`, `RippleField.cs`,
> `WindField.cs`, `RainField.cs`, `RaindropRevealField.cs`, `TileFlipGeometry.cs`, `SpotlightField.cs`,
> `JigsawGeometry.cs`, `MorphSettings.cs` (`MorphTimeline`) (+ the Magick-side `RippleRenderer.cs`,
> `GridMosaicRenderer.cs`, `RaindropRevealRenderer.cs`, `TileFlipRenderer.cs`, `SpotlightRenderer.cs`,
> `JigsawRenderer.cs`) — linked into the test project (pure files only).
> The A→B morph uses its own `pre-roll + morph window + remaining-B` timeline (total = pre-roll + B
> duration), distinct from the concat overlap model; it lives in `GifProcessor.Morph.cs` /
> `MorphTransitionDialog`, not in `TransitionGenerator`.
>
> **Invariant — effects/transitions never truncate the source footage.** After any effect or transition
> window ends, the remaining unplayed footage must still play to the end. Play-along effects mix only
> inside `[start, start+duration)` and pass the rest through (output length = full GIF length);
> frozen-then-play runs the effect, then plays the **whole** GIF; the A→B morph plays A's pre-roll, the
> morph, then **B's remaining** footage (A's leftover is the lone exception — it has already gone fully
> transparent). Any new effect must preserve this (see `docs/CreativeEffectsDevLog.md` rule #9).

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

**Wrapper:** `src/Core/GifsicleWrapper.cs` - Clean, testable wrapper with dependency injection

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

**DPI Settings:** Set via `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` in `SteamGifCropper.csproj`. Do NOT add `<dpiAware>`/`<dpiAwareness>` back into `app.manifest` — duplicating DPI settings triggers warning `WFO0003`.

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

**Implementation:** `src/Platform/WindowsThemeManager.cs`

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
├── src/
│   ├── Program.cs                          # Entry point & initialization
│   ├── Core/                               # Processing engine & settings types
│   │   ├── GifProcessor.cs                 # Core processing engine (5000+ lines; all effects)
│   │   ├── GifsicleWrapper.cs              # Gifsicle integration
│   │   ├── GifSizeFitter.cs                # Auto-fit-to-≤5MB pipe for gifsicle
│   │   ├── GifWriteDefines.cs              # Custom GIF write settings
│   │   ├── GifConcatenationSettings.cs
│   │   ├── TransitionGenerator.cs          # Dynamic (running-frame) transitions; pure GetFrameCount + per-frame renderers (linked into the test project)
│   │   ├── ImageInputValidator.cs
│   │   ├── ScrollDirection.cs
│   │   ├── GifEffectWindow.cs              # Pure [start,duration] frame-window math (slot/quicksand)
│   │   ├── GridMosaicSettings/Geometry/Renderer.cs   # Grid mosaic (geometry pure, renderer Magick)
│   │   ├── SlotMachineSettings/Geometry.cs           # Slot machine (geometry pure)
│   │   ├── QuicksandSettings/Geometry.cs             # Quicksand flow (geometry pure)
│   │   ├── RippleSettings/Field/Renderer.cs          # Water ripple (Field pure, Renderer Magick)
│   │   ├── WindSettings/Field/Renderer.cs            # Wind sway (Field pure, Renderer Magick)
│   │   ├── RainSettings/Field/Renderer.cs            # Rain overlay (Field pure, Renderer draws streaks into RGBA buffer)
│   │   ├── MorphSettings.cs                          # A→B morph settings + MorphTimeline (pure)
│   │   ├── RaindropRevealField/Renderer.cs           # Morph raindrop reveal (Field pure, Renderer Magick)
│   │   ├── TileFlipGeometry/Renderer.cs              # Morph tile flip (Geometry pure, Renderer Magick)
│   │   ├── SpotlightField/Renderer.cs               # Morph spotlight (Field pure, Renderer Magick)
│   │   ├── JigsawGeometry/Renderer.cs              # Morph jigsaw (Geometry pure, Renderer Magick)
│   │   └── GifProcessor.{Rain,Morph,Wind,...}.cs     # Per-effect engine partials (RunXxx + Build*)
│   ├── Forms/
│   │   ├── GTMainForm.cs                   # Main UI form
│   │   ├── GTMainForm.Designer.cs
│   │   └── GTMainForm.resx
│   ├── Dialogs/                            # operation dialogs (inline forms; most have no .Designer/.resx)
│   │   ├── ConcatenateGifsDialog.*
│   │   ├── MergeGifsDialog.*
│   │   ├── MergeFiveGifsDialog.*
│   │   ├── Mp4ToGifDialog.*
│   │   ├── OverlayGifDialog.*              # has .ja.resx and .zh-TW.resx
│   │   ├── ResizeNfpsGifDialog.*
│   │   ├── ScrollStaticImageDialog.*
│   │   ├── GridMosaicDialog.cs
│   │   ├── SlotMachineDialog.cs
│   │   ├── QuicksandDialog.cs
│   │   ├── RippleDialog.cs
│   │   ├── RippleDropPickerForm.cs         # Click-to-pick ripple drop position
│   │   ├── WindDialog.cs
│   │   ├── RainDialog.cs                   # Rain overlay (translucent streaks)
│   │   └── MorphTransitionDialog.cs        # A→B morph (raindrop reveal / tile flip)
│   └── Platform/                           # Windows integration
│       ├── WindowsThemeManager.cs
│       └── RegistryProvider.cs
├── Properties/                             # Default-culture resources
│   ├── Resources.resx                      # English strings
│   ├── Resources.zh-TW.resx                # Traditional Chinese
│   └── Resources.ja.resx                   # Japanese
├── Resources/                              # ResizeNfpsGifDialog localization resx
├── docs/                                   # Internal docs (e.g. LargeGifMemoryUsage.md)
├── res/                                    # Sample/screenshot assets for README
├── App.config                              # Application configuration
├── app.manifest                            # Win32 manifest (no DPI here — see csproj)
├── SteamGifCropper.csproj
├── SteamGifCropper.sln
└── SteamGifCropper.Tests/                  # xUnit v3 test project
    ├── GifProcessorTests.cs
    ├── GifsicleWrapperTests.cs
    └── TestData/                           # Test GIF files
```

**Note on resource manifest names:** because most form .cs files still declare `namespace GifProcessorApp`, MSBuild's default resource-name computation produces names like `GifProcessorApp.MergeGifsDialog.resources` (it reads the namespace from the `DependentUpon` .cs file). The orphan `src/Dialogs/OverlayPositionDialog.resx` has no companion .cs, so the csproj pins its manifest name explicitly via a `<LogicalName>` override to preserve the pre-reorg name (`SteamGifCropper.OverlayPositionDialog.resources`). Don't rename or delete that file without also removing the override.

## Development Notes

### Platform Constraints
- **Windows-only** - Uses Windows Forms, Windows API for theming, and Windows registry
- **x64 architecture** - Project configured for x64 platform
- **No implicit usings** - All namespaces must be explicitly declared
- **Nullable disabled** - No nullable reference type annotations

### Code Style
- Static `GifProcessor` class for all processing operations
- Flat namespace structure: virtually every file declares `namespace GifProcessorApp` (legacy — the assembly is named `SteamGifCropper`, and `Properties/Resources.Designer.cs` lives in `namespace SteamGifCropper.Properties`, which is why resource access uses `SteamGifCropper.Properties.Resources.{Name}`)
- Extensive use of `async`/`await` for UI responsiveness
- Resource strings for all user-facing text (localization)
- `<Nullable>` is **disabled** in the csproj — do not introduce `T?` nullable reference annotations; they trigger CS8632

### Testing Approach
- xUnit v3 framework, running on Microsoft.Testing.Platform (`xunit.v3` package)
- Test data in `SteamGifCropper.Tests/TestData/`
- Stubs for main components (`GifProcessor.Stub.cs`, `GifToolMainForm.Stub.cs`)
- File linking from main project to avoid duplication — `<Compile Include="..\src\Core\*.cs">` etc. If you move a source file, update the test csproj include paths to match.
- `dotnet test` against this project on .NET 10 SDK fails with the legacy VSTest target. Build the test assembly with `dotnet build` and invoke the produced `SteamGifCropper.Tests.exe` directly (it embeds an xUnit v3 runner — pass `-automated` for machine-readable output).

### External Dependencies
- **Required:** Magick.NET-Q8 (included in release)
- **Optional:** FFmpeg (user must install via `winget install ffmpeg`)
- **Optional:** gifsicle for Windows (user must download and add to PATH)
