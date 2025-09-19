# SteamGifCropper
[繁體中文](./Readme.md) | [日本語](./Readme_ja.md)

<div style="display: flex; flex-wrap: wrap; gap: 10px;">
  <img src="./res/screenshots/MainWindowEn.png" style="width: 75%; height: auto;">
</div>

SteamGifCropper is a small tool designed for the **Steam Workshop Personal Showcase**. It crops and processes GIF files to split wide GIFs (766px or 774px width) into 5 parts, resize GIFs to 766px width, and modify GIF byte data for Steam compatibility. Supports gifsicle post-processing.

---
The images below are five GIF files split using SteamGifCropper v0.2.1  
Due to loading time differences, the five GIF animations might appear slightly out of sync when viewed here. You can refresh the page to re-sync (press F5 on PC browsers)  

<div style="display: flex; flex-wrap: wrap; gap: 10px;">
  <img src="./res/new_shiny1_766px_Part1.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part2.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part3.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part4.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part5.gif" style="flex: 1 1 18%; height: auto;">
</div>


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

- **Operating System**: Windows 10 1904 or higher
- **Runtime**: .NET 8 runtime
- **Dependencies**: Magick.NET (based on ImageMagick) -- already included in zip file
- **FFMPEG**: For features using FFMPEG functionality, the system must have FFMPEG installed and set in the OS system environment variable **PATH**, otherwise it cannot be called. You can directly install using PowerShell 7 command: `winget install ffmpeg`.
- **gifsicle.exe external program**: Search for and download using keywords like "gifsicle for Windows" and configure; gifsicle.exe location must be included in the OS system environment variable **PATH**, otherwise it cannot be called.
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


```
SteamGifCropper.exe --memory-limit=2048 --disk-limit=8192
```

You can also adjust FFmpeg behavior through `SteamGifCropper.dll.config`, `App.config`:

- `FFmpeg.TimeoutSeconds`: Set timeout seconds for each FFmpeg execution (default 300 seconds).
- `FFmpeg.Threads`: Limit the number of threads FFmpeg uses, `0` means use default value.

---

## Installation & Usage

### Viewing GIF Split Results
- After split processing is complete, five cropped files will be saved to the specified folder with the following filename format:
  ```
  [OriginalFileName]_Part1.gif
  [OriginalFileName]_Part2.gif
  [OriginalFileName]_Part3.gif
  [OriginalFileName]_Part4.gif
  [OriginalFileName]_Part5.gif
  ```
Single files must not exceed 5MB, otherwise they cannot be uploaded to Steam. If a single file exceeds 5MB, you can adjust the source GIF or use other tools like EZGif to individually adjust that split file, but remember to modify the file tail byte at the end.


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

1. **Source GIF width limitation for split files**: GIF files with width of **766px** / **774px**.
1. **Output file format**: The program only supports outputting GIF files, and split ranges and image height both have default values that cannot be customized.
1. **Steam Personal Showcase**: Please ensure your GIF files comply with Steam showcase requirements; cropped files can be used for display on Steam personal pages.
1. **May consume significant memory during execution**: Depends on GIF file size.
1. **Only tested with GIFs of dimensions 766px × 432px (16:9) and 766px × 353px (iPhone 14 Pro video)**

## Known Issues
1. **Not all GIFs can be processed successfully**: After all, it's impossible to test with all related tools.
1. **Cannot confirm GIF creation program compatibility**: Tested normally with Filmora and EZGif.
1. **Split images may have black lines at edges**: Too lazy to fix, and don't know if it's an issue with video creation tools or the program?

## Reference: Creative Workshop Conversion Method
1. Find the desired video source or create your own.
1. Find a way to convert to GIF animation format, you can use [EZGif](https://ezgif.com/) for some processing.
1. Adjust the original GIF to **766px** width.
1. Use this program to split the **766px** GIF into five equal parts (150×5 files, plus 4px gap for each file, total 4×4=16).
1. You can use the included arrange.html to test if the split files have any problems.
1. Individual files must not exceed 5MB.
1. Use Chrome / Brave browser to upload files, showcase upload address: https://steamcommunity.com/sharedfiles/edititem/767/3/
1. First input in Browser console (after pressing F12, in console page): $J('#ConsumerAppID').val(480),$J('[name=file_type]').val(0),$J('[name=visibility]').val(0);
1. Some browsers have security measures, for example, you need to type "allow paste" first before executing the above action.
1. After input, upload files, remember to number the filenames for easier subsequent processing.
1. Repeat upload action, if no problems the files will be uploaded to the workshop.
1. In Steam personal page, add workshop showcase section, arrange the uploaded GIFs in order and you're done.

## Reference: Artwork Upload / Artwork Showcase
1. After uploading images:

var num= document.getElementsByName("image_width")[0].value;
document.getElementsByName("image_height")[0].value = num-(num-1);document.getElementsByName("image_width")[0].value= num*100;

## Reference: Screenshot Showcase
document.getElementsByName("file_type")[0].value= 5;
var num= document.getElementsByName("image_width")[0].value;
document.getElementsByName("image_height")[0].value = num-(num-1);
document.getElementsByName("image_width")[0].value= num*100;


---

## Reference: 766px aspect ratios
| Aspect | Resulting size (px) |
|--------|--------------------|
| 4:3    | 766 × 575 |
| 16:9   | 766 × 431 |
| 16:10  | 766 × 479 |
| 19.5:9 | 766 × 353 |
| 21:9   | 766 × 329 |

