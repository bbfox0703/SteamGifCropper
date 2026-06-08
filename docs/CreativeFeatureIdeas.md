# Creative GIF Feature Ideas & Roadmap

本文件記錄針對 Steam 個人展示櫃所發想的創意 GIF 功能組合，以及已實作的成果，供後續 session 接手。

核心思路：以既有的處理積木（split、merge、concatenate、overlay、scroll、reverse、transition、palette、tail byte）為基礎，組合出有趣的新效果。**最有特色的方向是把 Steam 展示櫃那 5 個並排槽位當成一整塊寬螢幕來玩**——因為只有本工具知道 766/774 的精確切割座標，能自動對齊、消除使用者手做時的 try-and-error。

---

## 已實作（this round）

### ✅ 格網馬賽克切割（Grid Mosaic Split）
把每張切割圖內部再加上同寬、等距、與槽位邊界對齊的格線（透明或實心可選），讓整片 5 槽展示櫃讀成一個刻意的格網／馬賽克。

- **關鍵洞見**：Steam 強制的 4px(766)/5px(774) 槽位間隙拿不掉、本來像「被迫的接縫」；加上對齊的內部格線後，那些間隙融入格網、從 bug 變 feature。
- **預設值對應使用者原始構想**：每槽 4 欄 → 5 槽共 20 欄（19 條垂直線 = 4 條 Steam 間隙 + 15 條內部線）、5 列 → 4 條水平線。
- **透明格線**透出個人檔案背景、與 Steam 間隙完全融合；**實心色格線**像窗櫺/像素牆（但不會跟間隙同色）。
- **檔案**：`src/Core/GridMosaicSettings.cs`、`GridMosaicGeometry.cs`（純函式，可單測）、`GridMosaicRenderer.cs`（Magick 繪製，像素寫入）、`src/Dialogs/GridMosaicDialog.cs`、`GifProcessor.GridMosaic()` 入口、`SplitGif` 多一個選用 `GridMosaicSettings grid = null` 參數、`FlatProgressBar`、三語 resx、`SteamGifCropper.Tests/GridMosaicTests.cs`。
- **Commits**：`fe2548a`（feature）、`f730b0a`（XC policy 修正）、`a69d096`（進度條修正）。
- **後續（不分割化）**：原本走 `SplitGif` 直接輸出 5 份；後來改為**只輸出單一 766px 全寬 GIF（不分割）**，方便與其他效果串接（拉霸→格網→…），最後再用主頁「切割 GIF」切成 5 份（切割時才加 100px 延伸 + 檔尾 0x21）。`GifProcessor.GridMosaic()` 改呼叫新的 `ApplyGridMosaic()`（逐 frame 在各槽 x 位置疊格線，gap 不畫），不再走 `SplitGif`。按鈕改名「766px 分割用格網馬賽克」。

### ✅ 拉霸 / 777 五轉輪（Slot Machine，B 段）
把 766px 圖片／GIF 做成 5 槽位拉霸：每個 Steam 欄＝一個垂直轉輪，wrap-scroll 自己那一欄的內容，套用 ease-out cubic 由快到慢、各自隨機停止，最後鎖定成原圖。**輸出單一 766px 全寬 GIF（不分割）**，方便串接；要切成 5 份時用主頁「切割 GIF」。

- **兩個變體**：靜態圖（鎖定後 hold 數秒）、動態 GIF。
- **GIF 播放方式（可選）**：`旋轉時同步播放`（預設，輸出總長＝GIF 長度，轉動時 GIF 已照常播放、轉輪在 live frame 上捲動，鎖定後續播剩餘段）或 `先轉再播放`（轉動時定格第一幀、鎖定後再播整圈 GIF）。
- **前置**：非 766/774 寬會自動 `Resize(766,0)`。**不自動分割**（見上）；可選 gifsicle 套用在整個 766px 檔（門檻仍有效）。後續用主頁「切割 GIF」切成 5 份時才加 100px 延伸 + 檔尾 0x21（在 gifsicle 之後）。
- **轉輪內容**：選定為「該欄自己的內容上下捲動」（最穩、任何寬度適用），非 5 欄輪播。
- **隨機化（取代固定 stagger）**：每輪的停止秒數＝旋轉時間 ± 波動%（預設 ±20%、上限 30%）、旋轉圈數＝設定值 ± 波動%（預設 ±25%、上限 50%），用 `System.Random` 計算 → 哪輪先停、哪輪轉最久都隨機，不再有「轉輪錯開」設定。
- **減速 + 回彈**：ease-out cubic 由快而慢（非急停）；停止後有 overshoot 阻尼擺動（`Bounce` 秒數，預設 0.5s）模擬實體拉霸彈跳。固定式減速，未做減速時點/幅度的設定。
- **方向**：可選由上向下（預設）/ 由下向上。
- **安全限制**：GIF 變體每輪旋轉秒數會被夾到 ≤ 來源 GIF 全長，避免旋轉比 GIF 本身還長。
- **預設**：FPS 15、Duration 3s、Spins 4。
- **檔案**：`src/Core/SlotMachineGeometry.cs`（純函式 ease/stop/offset，可單測）、`SlotMachineSettings.cs`、`src/Dialogs/SlotMachineDialog.cs`、`GifProcessor.SlotMachineStaticImage()`/`SlotMachineGif()`/`RunSlotMachine()`/`BuildSlotMachineAnimation()`、主視窗兩顆按鈕（表單加高至 524）、三語 resx、`SteamGifCropper.Tests/SlotMachineGeometryTests.cs`。

### ✅ 流沙橫向流動（Quicksand Flow）
把 766px 圖片／GIF 切成 N 條**水平層**，每層各自橫向 wrap-scroll，套用速度梯度（下／上／中最快），ease-in-out 加減速、整數圈數保證循環結束時每層回到原圖對齊位置 → **無縫循環**。本質是「拉霸」轉 90°（垂直欄→水平層、`Roll(0,off)`→`Roll(off,0)`、隨機減速→確定性梯度）。**輸出單一 766px 全寬 GIF（不分割）**，可串接；要切 5 份用主頁「切割 GIF」。

- **黏性流體感**：愈下／上／中（可選 `FastBand`）流愈快、另一端最慢，中間層用 `Viscosity` gamma 曲線塑形（>1 = 慢層更黏）。每層圈數 = `MinRevolutions`..`MaxRevolutions` 依 `BandSpeed^viscosity` 內插後四捨五入成整數（整數圈才能精準回歸 → 無縫循環的關鍵）。
- **回歸原座標**：位移 = 圈數 × 寬度 × easeInOut(t)；t=0 與 t=1 皆對齊原圖，且頭尾速度≈0 → 銜接無跳變。frame 0 即原圖。
- **GIF 播放方式（對齊拉霸的兩種模式）**：`流動時同步播放`（流沙剪切混在 **live GIF** 上做前 `Duration` 秒、之後 GIF 繼續播完剩餘；**輸出長度＝GIF 長度**；flow window 夾到 ≤GIF 長度確保流動在片內回歸對齊→無縫，等同 `BuildSlotMachinePlayDuringSpin`）或 `先流動再播放`（凍結 frame 0 流動 `Duration` 秒、再從 frame 0 播放**完整** GIF；**輸出長度＝Duration＋GIF 長度**，等同 `BuildSlotMachineSpinThenLock`）。靜態圖只走流動路徑（輸出＝Duration、循環）。⚠️ 早期版本曾誤把同步模式做成「重取樣成 `Duration×fps` 幀、截斷 GIF」（檔案異常小），已修正為上述語意。
- **方向 + 軸（單一 4 選下拉）**：向右→／向左←（水平，切橫列、`Roll(off,0)`、wrap=寬）或向下↓／向上↑（垂直，切直欄、`Roll(0,off)`、wrap=高）。`cmbDirection.SelectedIndex` 0/1=水平、2/3=垂直；`FlowRight`(=正向 roll)=index 0 或 2。「最快層位置」下拉依軸**動態改字**（水平＝下/上/中、垂直＝右/左/中），但 index→enum 映射不變（0=末層、1=首層、2=中），`CmbDirection_SelectedIndexChanged`→`RefreshFastBandLabels()` 處理。
- **軸無關引擎**：`BuildQuicksandAnimation` 分派器算 `bool vertical`、`bandTotal`（垂直=寬、水平=高）餵 `BandBounds`；兩個 build helper 共用 `CropQuicksandBand()`（依軸切橫列/直欄）+ `RollAndCompositeBand()`（依軸 `Roll(off,0)`/`Roll(0,off)` 並合成），`wrapLength`=另一個維度。
- **前置**：非 766/774 寬自動 `Resize(766,0)`；不自動分割、不自動 gifsicle（同拉霸）。
- **預設**：Layers 16、Duration 6s、FPS 15、Max 12 / Min 2 圈、FastBand 下方、Viscosity 1.0、向右流（水平）、同步播放。
- **檔案**：`src/Core/QuicksandGeometry.cs`（純函式 ease/band-bounds/speed/revolutions/offset，可單測）、`QuicksandSettings.cs`、`src/Dialogs/QuicksandDialog.cs`、`GifProcessor.QuicksandStaticImage()`/`QuicksandGif()`/`RunQuicksand()`/`BuildQuicksandAnimation()`（分派器）/`BuildQuicksandPlayDuringFlow()`/`BuildQuicksandFlowThenPlay()`、主視窗兩顆按鈕（新增第 9 列 y=255、下方元件 +31px、表單加高至 556）、三語 resx、`SteamGifCropper.Tests/QuicksandGeometryTests.cs`（22 例）。

### 🔭 水波紋 / 聲波（Water Ripple）— 評估完成、尚未實作
逐像素徑向位移場（非切片，現有積木幫不上忙；參考 `GridMosaicRenderer` 的像素寫入）。建議在 C# 端自做雙線性重採樣（繞過 Magick.NET-Q8 的 `Displace` 整數量化）：每個輸出像素依阻尼徑向波公式 `A·exp(-衰減·r)·時間包絡·sin(k·r−ωt)` 算 (dx,dy)、到來源雙線性取樣。落點可在圖外；邊界回波用「鏡像法 (method of images)」加衰減次波源（建議 v2 再加）。難度 ★★★（物理是封閉解、不難；成本在新 render primitive + 參數調校）。GIF 定格/跟播沿用同一二分法。

### ✅ 連帶修正
- **XC coder 政策**（`f730b0a`）：`Program.cs` 的安全政策原本只允許 GIF/PNG/JPEG/BMP，誤擋了內部純色畫布產生器 `XC`，導致所有 `new MagickImage(color, w, h)`（split/merge/overlay/scroll/Coalesce 都用）失敗。XC 不是檔案解析器、無攻擊面，已加回白名單。
- **進度條**（`a69d096`）：改用自繪 `FlatProgressBar`（`UserPaint` 純色填滿），繞過原生 comctl32 的 chunk/動畫繪製（深色主題下會在填滿邊緣留下兩條移動的黑線）；並把 `SplitGif` 進度改為單調遞增（每個 part 一個 20% 區段，不再每 part 跳到 100%）。
- **移除「較快的調色盤處理」選項**：原本主視窗 + 合併/合併分割/串接 3 個對話框各有此勾選框（跳過 dithering、效益不明顯）。已全部移除 UI 與 resx，合併/串接一律用 FloydSteinberg 品質調色盤（內部 `useFastPalette`/`GifConcatenationSettings.UseFasterPalette` 等恆為 false 的休眠死分支、以及 `SplitGif` 沒人用的 `grid` 參數，皆已於後續清除）。
- **publish.cmd 修正**：原本只做增量 `dotnet publish`，會用到舊 obj 狀態 + 殘留舊本地化 DLL。改成先刪 `publish\`+`bin\`+`obj\` 再 `dotnet publish`（含 `pause`），確保 build 到最新。CI（fresh runner）本就無此問題。
- **gifsicle 只在「切成 5 份」時自動套用**：原本拉霸/格網/捲動/疊加（主面板勾選框驅動）也會自動跑 gifsicle，導致單一 766px 大檔 gifsicle 逾時。改為**只有 `SplitGif`（5 份切割）才自動 gifsicle**；串接保留自己獨立的勾選框。
- **gifsicle timeout 可調**：`GifsicleWrapper.ProcessTimeout`（原硬編 30s）改由面板新 `numUpDownGifsicleTimeout` 控制（預設 30、5–600s），存進 `GifsicleSnapshot` 套用。
- **新增「對單一 GIF 執行 gifsicle」按鈕**（`GifProcessor.OptimizeSingleGif`）：選一個 GIF，用面板 Lossy/Palette/Optimize/Dither/timeout 跑 gifsicle，輸出 `*_gifsicle.gif`（不受 chkGifsicle 或門檻限制）。
- **主視窗版面整理**：所有 operation button 統一 26px 高、改成整齊 8×2 格線（修掉 `btnMp4ToGif`/`btnScrollAnimatedGif` 重疊、`btnMergeAndSplit` 過高、移除勾選框後右側留白），下方設定與 gifsicle 面板上移貼齊。

---

## 點子庫（尚未實作）

> 難度標示：★ = 省力（多為既有積木的一鍵組合）。

### A. 跨槽位「整片化」效果（最有 Steam 特色）
精神：使用者看到 5 個獨立 GIF，但讓它們在時間與空間上協調，視覺上變成一整塊會動的畫面。關鍵是把槽位間 4px/6px 間隙算進去（座標見 `Ranges766`/`Ranges774`）。

- **跨槽位捲動橫幅 / 跑馬燈**（Scroll + Split）：輸入超寬圖或文字，產生 5 個同步 GIF，內容像在一整塊螢幕上連續滑過全部 5 格。做個人檔案的捲動標語/Logo 橫幅。實作：在「766 + 間隙」虛擬寬畫布上 roll，再切 5 份。
  - 註：與既有「單張圖→平移」相近，新意只在自動算間隙，屬增量。
- **角色穿越展示櫃**（Overlay + Split）：一個物件/角色/太空船從第 1 格走到第 5 格。把 OverlayGif 的「移動 overlay」邏輯放到全寬畫布再切割。
- **骨牌式 / 波浪式揭曉轉場**（Transition + Split + 每槽時間偏移）：切換到新圖時第 1 格先變、依序掃過 5 格。重用 `TransitionGenerator`，每格給一個 delay offset。
- **回音 / 殘影播放**（Split + 每槽相位偏移）：5 格同一段動畫但各錯開幾幀，產生波浪/殘響感。切完後對每份做 frame rotate，實作很輕。

### B. 拉霸 / 777 五轉輪（★ 很吸睛，5 槽 = 5 reel 完美對應）— ✅ 已實作（見上方「已實作」）
Steam 展示櫃是 5 個垂直欄位，拉霸機剛好是 5 個垂直轉輪，一對一對上。
- 每欄 = 一個 reel，內容是一條垂直符號帶在快速捲動（重用垂直 scroll）。
- 加緩動：快→慢→停（`TransitionGenerator` 的 cubic easing 可直接用）。
- 5 個轉輪由左到右錯開停止時間，最後鎖定固定結果（777、STEAM 五字、任意組合）。
- 雖不能真隨機，但「永遠中頭獎」照樣很動感、超適合個人檔案。實作 = 5 條 staggered 減速垂直捲動。

### C. 通用有趣組合
- **乒乓 / 迴力鏢無縫循環**（Reverse + Concatenate）★最省力：原片 + 反轉接後面（正放→倒放），任何 GIF 變永不跳格的順滑循環。`ReverseGif` + `ConcatenateGifs` 幾乎免費。
- **無縫循環縫合**（CrossFade 首尾）：自動把首尾幀做 crossfade，讓會「跳一下」的 GIF 變無縫。
- **2×2 / 九宮格拼貼**（Merge 延伸）：現有是橫向併排；加縱向就能格狀拼貼（contact sheet）。
- **視差多層捲動**（Scroll + Overlay 疊加）：前/中/背景以不同速度捲動，做出景深。

### D. 視覺特效
- **調色盤循環 / 彩虹流動**：固定畫面靠每幀旋轉 palette 製造流光，檔案極小（用既有 Quantize/Remap）。
- **形狀遮罩裁切**：圓形/圓角 + 透明，圓形大頭貼風 GIF。
- **鏡像 / 萬花筒**：對半鏡射做對稱動畫。
- **速度漸變（time remap）**：時間軸上慢→快變速，做戲劇性開場。

### 建議優先序
1. ~~**拉霸 777**（B）~~ — ✅ 已實作。
2. **乒乓循環**（C）— 幾乎零成本的新按鈕。
3. **跨槽位捲動橫幅 / 角色穿越**（A）— 凸顯工具獨家的 Steam 定位。

---

## 實作要點 / 踩雷紀錄（接手前必讀）

1. **ImageMagick 安全政策**（`Program.cs` `ConfigureImageMagickPolicy`）：只允許 `GIF/PNG/JPEG/BMP/XC` coder。
   - 不要用清單外的格式 coder（SVG/PDF/TIFF… 全被擋，是刻意的）。
   - `new MagickImage(color, w, h)` 內部走 `xc:` pseudo-coder（已允許）。**向量 `Drawables` 繪製也會經過 XC** —— 本專案測試行程「不」套用此政策，所以用到 XC 的程式碼在測試會過、在 app 卻可能炸；新功能若用 Drawables 要記得這點，或改用像素寫入（見 `GridMosaicRenderer`）。
2. **新 dialog 樣式**：鏡射 `ScrollStaticImageDialog`（inline `InitializeComponent`、無 `.Designer.cs`、`namespace GifProcessorApp : Form`、含 `UpdateUIText()` + `ApplyTheme()` + 複製 `ApplyDark/LightThemeToControls`）。流程：dialog 開在 `GifProcessor.<Op>()` 裡，`ShowDialog()==OK` 後讀公開屬性再呼叫處理方法。
3. **單元測試**：測試專案用 **stub**（`GifProcessor.Stub.cs`），並非編譯真正的 `GifProcessor.cs`，而是逐檔 `<Compile Include>` 連結無重依賴的小檔。要單測新邏輯，**把純函式抽到無依賴的獨立檔**（如 `GridMosaicGeometry.cs`），並在 `SteamGifCropper.Tests.csproj` 加一行 `<Compile Include>` 連結它。
4. **進度條**：一律用 `FlatProgressBar`（`src/Forms/FlatProgressBar.cs`），別用原生 `ProgressBar`（深色主題下填滿邊緣會有黑線/動畫殘影）。全 app 只有主視窗一條 `pBarTaskStatus`，進度都呼叫 `GifProcessor.SetProgressBar(...)`。
5. **Steam 切割座標**：`Ranges766`/`Ranges774` + `GetCropRanges()` + `SplitGif()` 都在 `GifProcessor.cs`；新「切成 5 份」類功能直接重用（`SplitGif` 已支援選用 `GridMosaicSettings grid` 參數的擴充模式，可比照加其他選用參數）。
6. **在地化**：新字串要同時加到 `Properties/Resources.resx`、`Resources.zh-TW.resx`、`Resources.ja.resx`，並在 `Resources.Designer.cs` 補強型別屬性才能編譯。
7. **建置 / 測試**：`dotnet build SteamGifCropper.sln`；測試用 `dotnet build` 後直接跑 `SteamGifCropper.Tests/bin/Debug/net10.0-windows/SteamGifCropper.Tests.exe`（`-class <Name>` 過濾）。`dotnet test` 在 .NET 10 SDK 不支援。
