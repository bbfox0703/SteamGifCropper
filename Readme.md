# SteamGifCropper

[English](./Readme_en.md) | [日本語](./Readme_ja.md)

SteamGifCropper 是一個設計為 **Steam 工作坊個人展示櫃**的小工具，用於對 GIF 檔案進行裁切和處理。此程式可以將寬度為 766 / 774 像素的 GIF 動畫分割成多個部分、將Gif寬度調整為 **766px** 、或是把GIF檔案最後一個位元組由0x3B改為0x21。支援 gifsicle 後段處理。

---
下圖是使用SteamGifCropper v0.2.1版切出來的五張GIF圖片  
因為載入的時間差，在這看五張GIF動畫影片，可能會有點點怪怪的，會有點時間差，可以重新整理網頁看看 (PC瀏覽器上按F5即可)  

<div style="display: flex; flex-wrap: wrap; gap: 10px;">
  <img src="./res/new_shiny1_766px_Part1.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part2.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part3.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part4.gif" style="flex: 1 1 18%; height: auto;">
  <img src="./res/new_shiny1_766px_Part5.gif" style="flex: 1 1 18%; height: auto;">
</div>


---

## 功能

- **檢查 GIF 寬度**：建議使用寬度為 **766px** 的來源檔（亦支援 **774px**）。
- **自動裁切**：依預設範圍將 GIF 分成五段，並在每段底部延伸 **100px** 透明區塊，同時保留原始幀延遲。
- **透明與高度調整**：為新增區域套用相同透明色，並還原原始高度位元資訊。
- **縮放至 766px 寬度**：提供具有進度回饋的快速縮放工具。
- **尾位元工具**：可批次將多個 GIF 檔案的最後一個位元組在 `0x3B`／`0x21` 之間切換。
- **合併並再次切割五個 GIF**：自動將輸入檔縮放（約 153px），對齊時間並建立共用調色盤，合併成 766px 預覽後再切回五個展示檔。
- **並排合併 2～5 個 GIF**：不調整寬度直接拼接，提供共用調色盤選項並在 FPS 差異過大時提出警示。
- **串接 GIF 與轉場效果**：將多個 GIF 串成單一動畫，可統一 FPS／尺寸／調色盤，並選擇淡入淡出、滑動、縮放、溶解等轉場。
- **逆向播放 GIF**：產生反向播放版本的 GIF。
- **MP4 → GIF 轉檔**：透過 FFmpeg 指定起始時間與長度進行轉換。
- **GIF 重疊功能**：將一個 GIF 疊加在另一個 GIF 上，輸出新的動畫。
- **捲動動畫**：讓靜態圖片或現有 GIF 依指定方向、步進、週期及自動計算的循環時間進行捲動。
- **調整 GIF 尺寸與 FPS**：使用 FFmpeg（若可用）重新輸出 GIF，並提供鎖定長寬比選項。
- **gifsicle 後處理**：呼叫 `gifsicle.exe` 進行調色盤最佳化、Lossy 壓縮與抖動設定。
- **資源限制保護**：遵守 Magick.NET 設定的記憶體／磁碟限制，避免耗盡系統資源。
- **多語系與主題**：介面支援繁體中文／英文／日文，並自動切換 Windows 淺色／深色主題。

---

## 系統需求

- **操作系統**：Windows 10 1904 或更高版本
- **Runtime**：.NET 8 runtime
- **依賴函式庫**：Magick.NET（基於 ImageMagick）-- 已經內含於zip檔中
- **FFMPEG**：使用FFMPEG功能的部份，系統要先裝好FFMPEG，並設定在OS系統環境變數**PATH **中，否則會無法呼叫。可以直接使用Powershell 7下指令：winget install ffmpeg安裝。
- **gifsicle.exe外部程式**：自行使用關鍵字例如「gifsicle for Windows」尋找、下載並設定；gifsicle.exe的位置必須包含在OS系統環境變數**PATH **中，否則會無法呼叫。
---

## 資源限制設定

預設情況下，程式會限制 ImageMagick 的資源使用，以避免過度消耗系統資源：

- 記憶體限制：**1024 MB**
- 磁碟暫存限制：**4096 MB**

這些值可以透過以下方式覆寫：

1. **修改 `App.config`**：在 `<appSettings>` 中設定 `ResourceLimits.MemoryMB` 與 `ResourceLimits.DiskMB`。
2. **命令列參數**：啟動程式時加入 `--memory-limit=<MB>` 或 `--disk-limit=<MB>`。

例如：

```
SteamGifCropper.exe --memory-limit=2048 --disk-limit=8192
```

同時可以透過 `App.config` 調整 FFmpeg 行為：

- `FFmpeg.TimeoutSeconds`：設定每次 FFmpeg 執行的逾時秒數（預設 300 秒）。
- `FFmpeg.Threads`：限制 FFmpeg 使用的執行緒數，`0` 表示使用預設值。

---

## 安裝與使用

### 查看GIF切割結果
- 切割處理完成後，五個裁切檔案將保存到指定的資料夾中，檔案名稱格式為：
  ```
  [原始檔案名稱]_Part1.gif
  [原始檔案名稱]_Part2.gif
  [原始檔案名稱]_Part3.gif
  [原始檔案名稱]_Part4.gif
  [原始檔案名稱]_Part5.gif
  ```
單一檔案不得大於5MB，否則上傳不了Steam，如果單一檔案大於5MB，可以針對來源GIF做調整、或是使用其它工具例如EZGif單獨調整該分割檔，但是請記得最後要再修改檔案尾位元。

### GIF 覆蓋功能
1. 點選 **Overlay GIF** 按鈕，選擇要處理的基底 GIF。
2. 選擇要疊加的 GIF 檔案，並設定 X/Y 位置。
3. 確認後將兩者合併為新的 GIF。

> 注意：疊加高解析度或大型 GIF 時，視設定可能會佔用大量記憶體。

### 並排合併 2～5 個 GIF
- 保留原始寬度直接拼接，支援建立共用調色盤（可選擇較快速模式）。
- 偵測到來源 GIF 的 FPS 差異時會提出警示，便於事先調整。

### 合併並重新切割五個 GIF
1. 點選 **Merge & Split** 並依序加入五個 GIF 檔案。
2. 工具會自動縮放（約 153px）、對齊動畫長度並建立共用調色盤。
3. 先產生 766px 寬的合併預覽檔，再套用切割流程輸出 `*_Part1.gif` ~ `*_Part5.gif`。

### 串接 GIF 與轉場效果
1. 點選 **Concatenate GIFs** 並挑選至少兩個 GIF 檔案。
2. 設定 FPS／尺寸／調色盤的統一方式（自動、參考特定檔案或自訂）。
3. 選擇轉場效果（無、淡入淡出、滑動、縮放、溶解）及方向／時間。
4. 可額外啟用快速調色盤模式或在輸出後執行 gifsicle 最佳化。

### 捲動 GIF
- **Scroll static image**：讓 PNG、JPG 等靜態圖片依自訂方向、步進與移動次數捲動，亦可加入完整循環的緩衝區。
- **Scroll animated GIF**：支援載入 GIF，並在啟用自動計算時估算完整循環時間。

兩種模式都可搭配主視窗的 gifsicle 勾選項目進行後續最佳化。

---

## 檔案裁切範圍定義 -- **766px**
### **150px** each, **4px** gap

| 檔案部分   | X 座標範圍 |
|------------|------------|
| Part 1     | 0 - 149    |
| Part 2     | 153 - 303  |
| Part 3     | 307 - 457  |
| Part 4     | 461 - 611  |
| Part 5     | 615 - 剩下  |

## 檔案裁切範圍定義 -- **774px**
### **150px** each, **6px** gap

| 檔案部分   | X 座標範圍 |
|------------|------------|
| Part 1     | 0 - 149    |
| Part 2     | 155 - 305  |
| Part 3     | 311 - 461  |
| Part 4     | 467 - 617  |
| Part 5     | 623 - 剩下  |

---

## 注意事項

1. **切割檔案之來源GIF寬度限制**：寬度為 **766px** / **774px** 的 GIF 檔案。
1. **輸出文件格式**：程式僅支援輸出 GIF 檔案，且分割範圍與圖片高度、皆已經有預設值，無法自行定義。
1. **Steam 個人展示櫃**：請確保您的 GIF 檔案與 Steam 展示櫃要求相符，裁切後的文件可用於 Steam 個人頁面的展示。
1. **執行中可能吃掉不少記憶體**：要看GIF檔案大小了。
1. **只有試過長寛為 766px \* 432px (16:9) 及 766px \* 353px (iPhone 14 Pro影片) 的GIF**

## 已知問題
1. **不是所有的GIF皆能順利處理**：畢竟不可能測過所有相關工具。
1. **無法確認GIF製作程式相容性**：使用過Filmora和EZGif測試正常。
1. **切出來的圖片可能邊緣會有條黑線**：懶得搞了，也不知是影片製作工具、還是程式問題?

## 備考：創意工作坊轉檔方式
1. 找到想要的影片片源、或者自行製作。
1. 想辦法轉成 GIF 動畫格式，可以使用 [EZGif](https://ezgif.com/) 來做一些處理。
1. 將原始 GIF 調成寬度 **766px**。
1. 使用本程式將 **766px** 的 GIF 切成五等份 (150\*5個檔案、外加每個檔案有4px間隔、共4\*4=16)。
1. 可以使用附的 arrange.html 來測試切出來的檔案有沒有問題。
1. 各別檔案不得超過 5MB。
1. 使用 Chrome / Brave 瀏覽器上傳檔案，展示櫃上傳位址：https://steamcommunity.com/sharedfiles/edititem/767/3/
1. 要先在Browser console (按下F12後，在 console 頁) 輸入： $J('#ConsumerAppID').val(480),$J('[name=file_type]').val(0),$J('[name=visibility]').val(0);
1. 有的瀏覽器有安全措施，例如要先輸入 allow paste 後，才能執行上述動作。
1. 輸入後上傳檔案、檔名記得編號、方便後續處理。
1. 重複上傳動作、沒問題的話檔案會上傳到工作坊。
1. 在Steam個人頁面中，新增工作坊展示欄，依序把上傳的 GIF 佈置好即OK

## 備考：藝術作品上傳 / 藝術作品展示櫃
1. 上傳完圖像之後：

var num= document.getElementsByName("image_width")[0].value;
document.getElementsByName("image_height")[0].value = num-(num-1);document.getElementsByName("image_width")[0].value= num*100;

## 備考：螢幕擷圖展示櫃
document.getElementsByName("file_type")[0].value= 5;
var num= document.getElementsByName("image_width")[0].value;
document.getElementsByName("image_height")[0].value = num-(num-1);
document.getElementsByName("image_width")[0].value= num\*100;


## 註 **766px** 長寬比參考：
| **影片比例** | **轉成的長寬 (px)**     |
|--------------|-----------------------|
| 4:3          | 766px \* 575px        |
| 16:9         | 766px \* 431px        |
| 16:10        | 766px \* 479px        |
| 19.5:9       | 766px \* 353px        |
| 21:9         | 766px \* 329px        |
