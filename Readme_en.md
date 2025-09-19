# SteamGifCropper

[繁體中文](./Readme.md) | [日本語](./Readme_ja.md)

SteamGifCropper is a small tool made for the **Steam Workshop showcase**. It crops and adjusts GIF images so that they can be displayed as a seamless row on your Steam profile. It also provides a few helper utilities for working with GIF files.

---

## Features

- **Check GIF width** – accepts source GIFs with a width of **766px** (preferred) or **774px**.
- **Automatic slicing** – splits a GIF into five parts and extends each slice with **100px** of transparent space while keeping frame delays intact.
- **Transparent adjustments** – sets the added strip to the same transparent color and restores the original height bytes.
- **Scale to 766px width** – resize any GIF to 766px wide with progress feedback.
- **Tail byte utilities** – batch toggle the final byte between `0x3B`/`0x21` for multiple GIF files.
- **Merge & split five GIFs** – resize inputs (~153px each), sync durations, build a shared palette, merge to 766px, then reslice into five showcase-ready parts.
- **Merge 2–5 GIFs side by side** – compose inputs without resizing, with shared palette options and FPS mismatch warnings.
- **Concatenate GIFs with transitions** – combine multiple GIFs into a single animation, unify FPS/dimensions/palette, and add optional fade/slide/zoom/dissolve transitions.
- **Reverse playback** – generate a reversed copy of a GIF.
- **MP4 → GIF conversion** – uses FFmpeg to convert a segment (custom start time and duration) into GIF format.
- **Overlay GIFs** – position one GIF atop another to create composite animations.
- **Scrolling animations** – create scrolling GIFs from still images or existing GIFs with direction, step size, auto-duration and loop options.
- **Resize & change frame rate** – adjust width, height and FPS (FFmpeg-based when available) with optional aspect ratio lock.
- **gifsicle support** – call `gifsicle.exe` for palette optimization, lossy compression and dithering.
- **Resource limit awareness** – enforces Magick.NET memory/disk limits to avoid exhausting system resources.
- **Multi-language & theming** – Traditional Chinese, English and Japanese UI with automatic light/dark theme support.

---

## System Requirements

- **OS**: Windows 10 1904 or later
- **Runtime**: .NET 8 runtime
- **Library**: Magick.NET (included with the zip)
- **FFMPEG**: must be installed and on the `PATH` when using MP4 features.
- **gifsicle.exe**: download separately and ensure it is on the `PATH`.

---

## Resource Limits & FFmpeg Configuration

To avoid exhausting system resources the app applies conservative Magick.NET limits by default:

- Memory: **1024 MB**
- Disk cache: **4096 MB**

You can override these values in two ways:

1. **Edit `App.config`** – set `ResourceLimits.MemoryMB` and `ResourceLimits.DiskMB` under `<appSettings>`.
2. **Command-line arguments** – launch with `--memory-limit=<MB>` and/or `--disk-limit=<MB>`.

Example:

```
SteamGifCropper.exe --memory-limit=2048 --disk-limit=8192
```

Additional FFmpeg behaviour can also be tuned via `App.config`:

- `FFmpeg.TimeoutSeconds` – per-run timeout in seconds (default: 300).
- `FFmpeg.Threads` – force a thread count (`0` = FFmpeg default).

---

## Installation & Usage

### Output files
After slicing, five GIFs are saved with the following names:
```
[OriginalName]_Part1.gif
[OriginalName]_Part2.gif
[OriginalName]_Part3.gif
[OriginalName]_Part4.gif
[OriginalName]_Part5.gif
```
Each file must be below **5MB** to upload to Steam. Adjust or optimize if necessary.

### Scaling
The scaling feature is provided for convenience. Large GIFs may require significant memory and time.

### Overlay GIF
1. Click **Overlay GIF** and select the base GIF.
2. Choose the GIF to overlay and set the X/Y offset.
3. Confirm to create a new GIF with the overlay applied.

The dialog is localized (English, Traditional Chinese, Japanese) and works with both light and dark themes.
**Note:** Overlaying large or high-resolution GIFs can consume a lot of memory.

### Merging 2–5 GIFs
A basic merging function that keeps the original width. It builds a shared palette (with an optional faster mode) and warns when FPS differs noticeably between sources.

### Merging five GIFs into one 766px GIF
Resizes each GIF to ~153px, synchronizes duration, merges to a 766px preview GIF, and splits the result back into five showcase-ready slices in the source folder.

### Concatenating GIFs with transitions
1. Click **Concatenate GIFs** and pick at least two GIF files.
2. Choose how to unify FPS, dimensions and palette (auto, reference GIF, or custom options).
3. Select a transition style (none, fade, slide, zoom or dissolve), direction/type, and duration.
4. Decide whether to use the faster palette builder or run gifsicle optimization after export.

The tool creates a single GIF stitched in sequence and honours the configured resource limits.

---

### Scrolling GIFs
- **Scroll static image** – turn a still image (PNG, JPG, etc.) into a scrolling animation with custom direction, step size, loop count and optional full-cycle padding.
- **Scroll animated GIF** – reuse the same options, allow GIF inputs, and automatically estimate a full-cycle duration when enabled.

Both options can run gifsicle optimization when the main window checkbox is enabled.

---

## Slice Ranges – **766px**
**150px** each with a **4px** gap

| Part | X range |
|------|---------|
| Part 1 | 0 – 149 |
| Part 2 | 153 – 303 |
| Part 3 | 307 – 457 |
| Part 4 | 461 – 611 |
| Part 5 | 615 – end |

## Slice Ranges – **774px**
**150px** each with a **6px** gap

| Part | X range |
|------|---------|
| Part 1 | 0 – 149 |
| Part 2 | 155 – 305 |
| Part 3 | 311 – 461 |
| Part 4 | 467 – 617 |
| Part 5 | 623 – end |

---

## Notes

1. Source GIF width must be **766px** or **774px**.
2. Output format is GIF only; slice ranges and height are fixed.
3. Ensure your files meet Steam showcase requirements and stay below 5MB per file.
4. Processing large GIFs can consume a lot of memory.
5. Tested mainly with GIFs sized 766×432 and 766×353.
6. FFmpeg timeout and thread usage can be adjusted in `App.config` via `FFmpeg.TimeoutSeconds` and `FFmpeg.Threads`.

## Known Issues

- Not every GIF can be processed successfully.
- Compatibility with all GIF creation tools is not guaranteed.
- Sliced images may show a thin black border depending on the source.

---

## Reference: 766px aspect ratios
| Aspect | Resulting size (px) |
|--------|--------------------|
| 4:3    | 766 × 575 |
| 16:9   | 766 × 431 |
| 16:10  | 766 × 479 |
| 19.5:9 | 766 × 353 |
| 21:9   | 766 × 329 |

