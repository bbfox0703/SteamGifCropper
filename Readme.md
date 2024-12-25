# SteamGifCropper

SteamGifCropper 是一個設計為 **Steam 工作坊個人展示櫃**的小工具，用於對 GIF 檔案進行裁切和處理。此程式可以將寬度為 766 / 774 像素的 GIF 動畫分割成多個部分、將Gif寬度調整為 **766px** 、或是把GIF檔案最後一個位元組由0x3B改為0x21。支援 gifscile 後段處理。

---

## 功能

- **檢查 GIF 寬度**：最好使用寬度為 **766px** 的 GIF 檔案作為來源檔 (也接受 **774px**)。
- **自動裁切**：將 GIF 動畫分割為五部分，裁切範圍根據預設的 X 座標範圍執行。
- **高度擴展**：裁切後的每部分高度增加 **100px**，新增部分設置為透明背景。
- **透明色設置**：將新增部分的底部像素作為透明色，確保透明效果一致。
- **保持動畫播放速度**：保留 GIF 的Frame延遲值，確保輸出後的動畫播放速度與原始檔案一致。
- **自動修改GIF資料：**：最後一位元改為0x21、第08 & 09位元改為高度增加100px前的值。
- **縮放GIF檔案至766px寬度**：提供GIF寬度縮放功能。
- **把GIF檔案最後一個位元組由0x3B改為0x21**：如果原先不是0x3B則不處理。 
- **gifscile後續處理支援**：程式直接呼叫gifscile.exe、對GIF檔案做最佳化。
---

## 系統需求

- **操作系統**：Windows 10 或更高版本
- **Runtime**：.NET Framework 4.8
- **依賴函式庫**：Magick.NET（基於 ImageMagick）-- 已經內含於zip檔中
- **gifscile.exe外部程式**：自行使用關鍵字例如「gifscile for Windows」尋找、下載並設定；gifscile.exe的位置必須包含在OS系統環境變數**PATH **中，否則會無法呼叫。
---

## 安裝與使用

### 查看GIF切割結果
- 切割處理完成後，五個裁切檔案將保存到指定的資料夾中，檔案稱之格式為：
  ```
  [原始檔案名稱]_Part1.gif
  [原始檔案名稱]_Part2.gif
  [原始檔案名稱]_Part3.gif
  [原始檔案名稱]_Part4.gif
  [原始檔案名稱]_Part5.gif
  ```
單一檔案不得大於5MB，否則上傳不了Steam，如果單一檔案大於5MB，可以針對來源GIF做調整、或是使用其它工具例如EZGif單獨調整該分割檔，但是請記得最後要再修改檔案尾位元。

### 縮放功能
- 縮放功能只是附帶，其功能沒一般專業軟體轉換速度那麼快、且如果來源GIF檔案大一些，會吃掉不少記憶體。
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
1. **無法卻認GIF製作程式相容性**：使用過Filmora和EZGif測試正常。
1. **切出來的圖片可能邊緣會有條黑線**：懶得搞了，也不知是影片製作工具、還是程式問題?

## 備考：創意工作坊轉檔方式
1. 找到想要的影片片源。
1. 想辦法轉成 GIF 動畫格式，可以使用 [EZGif](https://ezgif.com/) 來做一些處理。
1. 將原始 GIF 調成寬度 **766px** (150\*5個檔案、外加每個檔案有4px間隔、共4\*4=16)。
1. 使用本程式將 **766px** 的 GIF 切成五等份。
1. 可以使用附的 arrange.html 來測試切出來的檔案有沒有問題。
1. 各別檔案不得超過 5MB。
1. 使用 Chrome / Brave 瀏覽器上傳檔案，展示櫃上傳位址：https://steamcommunity.com/sharedfiles/edititem/767/3/
1. 要先在Browser console (按下F12後，在 console 頁) 輸入： $J('#ConsumerAppID').val(480),$J('[name=file_type]').val(0),$J('[name=visibility]').val(0);
1. 有的瀏覽器有安全措施，例如要先輸入 allow input 後，才能執行上述動作。
1. 輸入後上傳檔案、檔名記得編號、方便後續處理。
1. 重覆上傳動作。
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
