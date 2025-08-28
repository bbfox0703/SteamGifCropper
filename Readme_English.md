# SteamGifCropper

SteamGifCropper is a utility designed for the **Steam Workshop showcase** to crop and process GIF files. The program can split a GIF with a width of 766 or 774 pixels into multiple parts, resize the GIF width to **766px**, or change the last byte of the GIF from 0x3B to 0x21. It supports gifsicle for post-processing.

---

The images below show five GIFs generated with SteamGifCropper v0.2.1.
Because of loading time differences, the animations may appear slightly out of sync. Refresh the page if needed (press F5 in desktop browsers).

<div style="display: flex; flex-wrap: wrap; gap: 10px;">
  <img src="./res/new_shiny1_766px_Part1.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part2.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part3.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part4.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part5.gif" style="flex: 1 1 18%; height: auto;">
</div>

---

## Features

- **Check GIF width**: Ideally use a source GIF that is **766px** wide (but **774px** is also accepted).
- **Automatic cropping**: Split the animation into five parts using preset X-coordinate ranges.
- **Height expansion**: Increase each part's height by **100px** with a transparent background.
- **Set transparency color**: The bottom pixel of the new area becomes the transparent color to ensure consistency.
- **Preserve animation speed**: Keep frame delays so the output plays at the original speed.
- **Automatic GIF data modification**: Change the last byte to 0x21 and update bytes 08 & 09 to the value before adding 100px height.
- **Scale GIF file to 766px width**.
- **Change last byte from 0x3B to 0x21**: Only if the last byte is originally 0x3B.
- **Change last byte from 0x21 to 0x3B**: Only if the last byte is originally 0x21.
- **Merge five GIFs into one 766px-wide GIF**: Scale each to about 153px wide and merge.
- **Simple MP4 to GIF conversion**: Provide the source file, start time, and length; no additional options.
- **gifsicle post-processing support**: Calls gifsicle.exe to optimize the cropped GIFs.

---

## System Requirements

- **Operating system**: Windows 10 or newer
- **Runtime**: .NET Framework 4.8
- **Dependency library**: Magick.NET (based on ImageMagick) -- included in the zip package
- **gifsicle.exe external program**: Search and download it (e.g., "gifsicle for Windows") and ensure its location is included in the system environment variable **PATH**, otherwise it cannot be invoked.

---

## Installation & Usage

### Viewing cropped GIF results
After processing, five files are saved to the selected folder:
```
[original_filename]_Part1.gif
[original_filename]_Part2.gif
[original_filename]_Part3.gif
[original_filename]_Part4.gif
[original_filename]_Part5.gif
```
Each file must be under 5MB or it cannot be uploaded to Steam. If a file exceeds 5MB, adjust the source GIF or use other tools like EZGif to modify the individual part, but remember to change the file's last byte afterwards.

### Scaling feature
The scaling feature is basic; it is slower than professional conversion software and large source GIFs can consume significant memory.

---

### Merging five GIFs into one
The merge feature is also basic and not perfect, but it allows previewing the result. For perfect output, use professional tools.

---

## Crop range definition -- **766px**
### **150px** each, **4px** gap

| File part | X range |
|-----------|---------|
| Part 1    | 0 - 149 |
| Part 2    | 153 - 303 |
| Part 3    | 307 - 457 |
| Part 4    | 461 - 611 |
| Part 5    | 615 - remainder |

## Crop range definition -- **774px**
### **150px** each, **6px** gap

| File part | X range |
|-----------|---------|
| Part 1    | 0 - 149 |
| Part 2    | 155 - 305 |
| Part 3    | 311 - 461 |
| Part 4    | 467 - 617 |
| Part 5    | 623 - remainder |

---

## Notes

1. **Source GIF width limitation**: Only GIFs with widths of **766px** or **774px** can be processed.
2. **Output file format**: The program only outputs GIF files. The crop range and image height are preset and cannot be customized.
3. **Steam showcase**: Ensure your GIF meets Steam's showcase requirements. The cropped files can be used on your Steam profile page.
3. **Memory usage**: May consume a lot of memory depending on the GIF size.
5. **Tested dimensions**: Only tested with GIFs of 766px × 432px (16:9) and 766px × 353px (iPhone 14 Pro video).

## Known Issues

1. **Not all GIFs can be processed successfully**: It's impossible to test every related tool.
2. **GIF creation tool compatibility is uncertain**: Tested with Filmora and EZGif.
3. **A thin black line may appear at the edge of cropped images**: Unsure if caused by the video tool or this program.

## Reference: Steam Workshop conversion steps

1. Find or create the desired video source.
2. Convert it to GIF format; tools like [EZGif](https://ezgif.com/) can help.
3. Resize the original GIF to a width of **766px**.
4. Use this program to split the **766px** GIF into five equal parts (150px × 5, with 4px gaps totaling 16).
5. Use the included arrange.html to test the cropped files.
6. Each file must not exceed 5MB.
7. Upload using Chrome or Brave at: https://steamcommunity.com/sharedfiles/edititem/767/3/
8. In the browser console (press F12 and go to the console tab) enter: `$J('#ConsumerAppID').val(480),$J('[name=file_type]').val(0),$J('[name=visibility]').val(0);`
9. Some browsers require typing `allow paste` before executing the above command.
10. After entering the command, upload files with numbered filenames for easier processing.
11. Repeat the upload process; if successful, the files will appear in the Workshop.
12. On your Steam profile, add a Workshop showcase and arrange the uploaded GIFs.

## Reference: Artwork upload / Artwork showcase
After uploading the images:
```
var num= document.getElementsByName("image_width")[0].value;
document.getElementsByName("image_height")[0].value = num-(num-1);
document.getElementsByName("image_width")[0].value= num*100;
```

## Reference: Screenshot showcase
```
document.getElementsByName("file_type")[0].value= 5;
var num= document.getElementsByName("image_width")[0].value;
document.getElementsByName("image_height")[0].value = num-(num-1);
document.getElementsByName("image_width")[0].value= num*100;
```

## Aspect ratio reference for **766px** width
| **Aspect ratio** | **Converted size (px)** |
|------------------|------------------------|
| 4:3              | 766px × 575px           |
| 16:9             | 766px × 431px           |
| 16:10            | 766px × 479px           |
| 19.5:9           | 766px × 353px           |
| 21:9             | 766px × 329px           |
