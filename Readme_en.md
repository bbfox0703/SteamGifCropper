# SteamGifCropper

[繁體中文](./Readme.md) | [日本語](./Readme_ja.md)

SteamGifCropper is a small tool made for the **Steam Workshop showcase**. It crops and adjusts GIF images so that they can be displayed as a seamless row on your Steam profile. It also provides a few helper utilities for working with GIF files.

---

## Features

- **Check GIF width** – accepts source GIFs with a width of **766px** (preferred) or **774px**.
- **Automatic slicing** – splits a GIF into five parts based on preset X ranges.
- **Height extension** – adds **100px** transparent space to the bottom of each slice.
- **Transparent color** – uses the bottom pixel as the transparent color for the added area.
- **Preserve frame delay** – keeps the original animation speed.
- **Modify GIF bytes** – adjusts the last byte and restores original height values.
- **Scale to 766px width** – optional resizing utility.
- **Toggle last byte 0x3B / 0x21** – quick byte modification helpers.
- **Merge five GIFs into one** – scales to ~153px width and stitches them together.
- **Merge 2–5 GIFs into one** – merges without scaling; width equals the sum of inputs. Slow for large files.
- **Reverse playback** – creates a reversed version of a GIF.
- **Simple MP4 → GIF** – specify source file, start time and duration.
- **gifsicle support** – calls `gifsicle.exe` for post processing/optimization.

---

## System Requirements

- **OS**: Windows 10 1904 or later
- **Runtime**: .NET 8 runtime
- **Library**: Magick.NET (included with the zip)
- **FFMPEG**: must be installed and on the `PATH` when using MP4 features.
- **gifsicle.exe**: download separately and ensure it is on the `PATH`.

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

### Merging 2–5 GIFs
A basic merging function that keeps the original width. It is not memory efficient and may be slow.

### Merging five GIFs into one 766px GIF
Scales each GIF to ~153px width before merging. Intended for preview purposes.

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

