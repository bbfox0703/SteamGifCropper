# SteamGifCropper

SteamGifCropper 是一個設計為 **Steam Workshop 個人展示櫃** 開發的工具，用於對 GIF 文件進行裁切和處理。此工具可以快速將寬度為 766 像素的 GIF 動畫分割成多個部分。

---

## 功能

- **檢查 GIF 寬度**：僅接受寬度為 **766px** 的 GIF 文件作為來源檔案。
- **自動裁切**：將 GIF 動畫分割為五部分，裁切範圍根據預設的 X 座標範圍執行。
- **高度擴展**：裁切後的每部分高度增加 **100px**，新增部分設置為透明背景。
- **透明色設置**：將新增部分的底部像素作為透明色，確保透明效果一致。
- **保持動畫播放速度**：保留 GIF 的幀延遲值，確保輸出後的動畫播放速度與原始文件一致。
- **進度條顯示**：支持處理過程中顯示實時進度。
- **自動修改GIF資料：**：最後一位元改為0x21、第08 & 09位元改為高度增加100px前的值。

---

## 系統需求

- **操作系統**：Windows 10 或更高版本
- **開發框架**：.NET Framework 4.8 或以上
- **依賴函式庫**：Magick.NET（基於 ImageMagick）-- 已經內含

---

## 安裝與使用

### 1. 下載並運行工具
下載 **SteamGifCropper.exe** 並執行。

### 2. 選擇 GIF 文件
- 點擊 **"Select a GIF file to process"** 按鈕，選擇影像寬度為 **766px** 的 GIF 文件。
- 如果GIF影像寬度不符合要求，工具會提示錯誤並退出。

### 3. 輸出文件位置
- 點擊 **"Save the processed GIF file"**，選擇裁切後 GIF 文件的保存位置。

### 4. 自動處理
- 工具將自動分割 GIF 文件為五部分，並為每部分增加圖片高度。

### 5. 查看輸出結果
- 處理完成後，五個裁切文件將保存到指定的目錄中，文件名格式為：
  ```
  [原始文件名]_Part1.gif
  [原始文件名]_Part2.gif
  [原始文件名]_Part3.gif
  [原始文件名]_Part4.gif
  [原始文件名]_Part5.gif
  ```

---

## 文件裁切範圍

| 文件部分   | X 座標範圍 |
|------------|------------|
| Part 1     | 0 - 149    |
| Part 2     | 153 - 303  |
| Part 3     | 307 - 457  |
| Part 4     | 461 - 611  |
| Part 5     | 615 - 765  |

---

## 注意事項

1. **輸入文件寬度限制**：工具僅接受寬度為 **766px** 的 GIF 文件。
2. **輸出文件格式**：工具僅支持輸出 GIF 文件，且分割範圍與新增高度已預設，無法自定義。
3. **Steam 個人展示櫃**：請確保您的 GIF 文件與 Steam 展示櫃要求相符，裁切後的文件可用於 Steam 個人頁面的展示。
4. **不是所有的GIF皆能順利處理**：我試過某些GIF製作程式，如果使用影像品質最佳的輸出方式，經本程式處理後，圖片會怪怪的。
5. **執行中可能吃掉2G記憶體**：依來源檔不同，有時會吃到2GB記憶體。
6. **只有試過長寛為 766px x 431px的GIF**

## 備考：轉檔方式
1. 找到想要的影片源。
1. 想辦法轉成 GIF 動畫格式，可以使用 [EZGif](https://ezgif.com/) 來做一些處理或是減少檔案大小的動作。
1. 將原始 GIF 調成宽度 **766px** (150x5 + 4x4)，一般 16:9的影片，轉成的長寬是 **766px X 431px** 左右。
1. 使用本工具將 **766px** 的 GIF 切成五等份。
1. 檢查檔案有沒有問題、且不得超過 5MB
1. 使用 Chrome / Brave 瀏覽器上傳檔案，展示櫃上傳位址：https://steamcommunity.com/sharedfiles/edititem/767/3/
1. 要先在Browser console (按下F12後，在 console 頁) 輸入： $J('#ConsumerAppID').val(480),$J('[name=file_type]').val(0),$J('[name=visibility]').val(0);
1. 輸入後上傳檔案、檔名記得編號、方便後續處理。
1. 重覆上傳動作。
1. 在Steam個人頁面中，新增工作坊展示欄，依序把上傳的 GIF 佈置好即OK