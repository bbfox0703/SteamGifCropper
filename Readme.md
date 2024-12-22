# SteamGifCropper

SteamGifCropper 是一個設計為 **Steam Workshop 個人展示櫃** 小工具，用於對 GIF 檔案進行裁切和處理。此程式可以快速將寬度為 766 像素的 GIF 動畫分割成多個部分。

---

## 功能

- **檢查 GIF 寬度**：最好使用寬度為 **766px** 的 GIF 檔案作為來源檔。
- **自動裁切**：將 GIF 動畫分割為五部分，裁切範圍根據預設的 X 座標範圍執行。
- **高度擴展**：裁切後的每部分高度增加 **100px**，新增部分設置為透明背景。
- **透明色設置**：將新增部分的底部像素作為透明色，確保透明效果一致。
- **保持動畫播放速度**：保留 GIF 的幀延遲值，確保輸出後的動畫播放速度與原始檔案一致。
- **進度條顯示**：支持處理過程中顯示進度。
- **自動修改GIF資料：**：最後一位元改為0x21、第08 & 09位元改為高度增加100px前的值。

---

## 系統需求

- **操作系統**：Windows 10 或更高版本
- **Runtime**：.NET Framework 4.8
- **依賴函式庫**：Magick.NET（基於 ImageMagick）-- 已經內含

---

## 安裝與使用

### 1. 下載並執行程式
下載 **SteamGifCropper.exe** 並執行。

### 2. 選擇 GIF 檔案
- 跳出 **"Select a GIF file to process"** 視窗，選擇影像寬度為 **766px** 的 GIF 檔案。

### 3. 輸出文件位置
- 跳出 **"Save the processed GIF file"** 視窗，選擇裁切後 GIF 檔案的存檔位置。

### 4. 自動處理
- 程式將自動分割 GIF 檔案為五部分，並為每個檔案增加圖片高度。

### 5. 查看輸出結果
- 處理完成後，五個裁切檔案將保存到指定的資料夾中，檔案稱之格式為：
  ```
  [原始文件名]_Part1.gif
  [原始文件名]_Part2.gif
  [原始文件名]_Part3.gif
  [原始文件名]_Part4.gif
  [原始文件名]_Part5.gif
  ```

---

## 文件裁切範圍

| 檔案部分   | X 座標範圍 |
|------------|------------|
| Part 1     | 0 - 149    |
| Part 2     | 153 - 303  |
| Part 3     | 307 - 457  |
| Part 4     | 461 - 611  |
| Part 5     | 615 - 剩下  |

---

## 注意事項

1. **輸入文件寬度限制**：寬度為 **766px** 的 GIF 檔案為佳。
1. **輸出文件格式**：程式僅支援輸出 GIF 檔案，且分割範圍與新增圖片高度、皆已經有預設值，無法自行定義。
1. **Steam 個人展示櫃**：請確保您的 GIF 檔案與 Steam 展示櫃要求相符，裁切後的文件可用於 Steam 個人頁面的展示。
1. **執行中可能吃掉2G記憶體**：依來源檔不同，有時會吃到2GB記憶體。
1. **只有試過長寛為 766px x 431px的GIF**

## 已知問題
1. **不是所有的GIF皆能順利處理**：我試過某些GIF製作程式，如果使用影像品質最佳的輸出方式，經本程式處理後，圖片會怪怪的無法使用。
1. **無法卻認GIF製作程式相容性**：只有使用過Filmora測試正常 (Filmora影像品質一般；如設為最佳好像會有問題)
1. **EZGif轉出的似乎有問題**：轉出的檔案怪怪的

## 備考：轉檔方式
1. 找到想要的影片片源。
1. 想辦法轉成 GIF 動畫格式，可以使用 [EZGif](https://ezgif.com/) 來做一些處理或是減少檔案大小的動作。
1. 將原始 GIF 調成宽度 **766px** (150x5 + 4x4)，一般 16:9的影片，轉成的長寬是 **766px X 431px** 左右。
1. 使用本程式將 **766px** 的 GIF 切成五等份。
1. 可以使用 arrange.html 測試有沒有問題。
1. 檢查檔案有沒有問題、且不得超過 5MB
1. 使用 Chrome / Brave 瀏覽器上傳檔案，展示櫃上傳位址：https://steamcommunity.com/sharedfiles/edititem/767/3/
1. 要先在Browser console (按下F12後，在 console 頁) 輸入： $J('#ConsumerAppID').val(480),$J('[name=file_type]').val(0),$J('[name=visibility]').val(0);
1. 有的瀏覽器有安全措施，例如要先輸入 allow input 後，才能執行上述動作。
1. 輸入後上傳檔案、檔名記得編號、方便後續處理。
1. 重覆上傳動作。
1. 在Steam個人頁面中，新增工作坊展示欄，依序把上傳的 GIF 佈置好即OK