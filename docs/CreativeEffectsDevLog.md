# Creative Effects — Dev Log / Handoff Notes

本檔記錄創意 GIF 效果的**實作細節**（檔案清單、commit、語意、踩雷筆記），供後續 session 接手。
功能的**點子與完成狀態（roadmap）**請見 [CreativeFeatureIdeas.md](CreativeFeatureIdeas.md)。

這些「766px 單輸出、可串接」效果（格網馬賽克、拉霸、流沙、水波紋）的共同精神：把 Steam 展示櫃那 5 個並排槽位當成一整塊寬螢幕來玩，輸出單一 766px 全寬 GIF（不分割），最後再用主頁「切割 GIF」切成 5 份（切割時才加 100px 延伸 + 檔尾 0x21）。

---

## 已實作功能的實作細節

### 格網馬賽克切割（Grid Mosaic Split）
把每張切割圖內部再加上同寬、等距、與槽位邊界對齊的格線（透明或實心可選），讓整片 5 槽展示櫃讀成一個刻意的格網／馬賽克。

- **關鍵洞見**：Steam 強制的 4px(766)/5px(774) 槽位間隙拿不掉、本來像「被迫的接縫」；加上對齊的內部格線後，那些間隙融入格網、從 bug 變 feature。
- **預設值對應使用者原始構想**：每槽 4 欄 → 5 槽共 20 欄（19 條垂直線 = 4 條 Steam 間隙 + 15 條內部線）、5 列 → 4 條水平線。
- **透明格線**透出個人檔案背景、與 Steam 間隙完全融合；**實心色格線**像窗櫺/像素牆（但不會跟間隙同色）。
- **檔案**：`src/Core/GridMosaicSettings.cs`、`GridMosaicGeometry.cs`（純函式，可單測）、`GridMosaicRenderer.cs`（Magick 繪製，像素寫入）、`src/Dialogs/GridMosaicDialog.cs`、`GifProcessor.GridMosaic()` 入口、`SplitGif` 多一個選用 `GridMosaicSettings grid = null` 參數、`FlatProgressBar`、三語 resx、`SteamGifCropper.Tests/GridMosaicTests.cs`。
- **Commits**：`fe2548a`（feature）、`f730b0a`（XC policy 修正）、`a69d096`（進度條修正）。
- **後續（不分割化）**：原本走 `SplitGif` 直接輸出 5 份；後來改為**只輸出單一 766px 全寬 GIF（不分割）**，方便與其他效果串接（拉霸→格網→…），最後再用主頁「切割 GIF」切成 5 份（切割時才加 100px 延伸 + 檔尾 0x21）。`GifProcessor.GridMosaic()` 改呼叫新的 `ApplyGridMosaic()`（逐 frame 在各槽 x 位置疊格線，gap 不畫），不再走 `SplitGif`。按鈕改名「766px 分割用格網馬賽克」。

### 拉霸 / 777 五轉輪（Slot Machine，B 段）
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

### 流沙橫向流動（Quicksand Flow）
把 766px 圖片／GIF 切成 N 條**水平層**，每層各自橫向 wrap-scroll，套用速度梯度（下／上／中最快），ease-in-out 加減速、整數圈數保證循環結束時每層回到原圖對齊位置 → **無縫循環**。本質是「拉霸」轉 90°（垂直欄→水平層、`Roll(0,off)`→`Roll(off,0)`、隨機減速→確定性梯度）。**輸出單一 766px 全寬 GIF（不分割）**，可串接；要切 5 份用主頁「切割 GIF」。

- **黏性流體感**：愈下／上／中（可選 `FastBand`）流愈快、另一端最慢，中間層用 `Viscosity` gamma 曲線塑形（>1 = 慢層更黏）。每層圈數 = `MinRevolutions`..`MaxRevolutions` 依 `BandSpeed^viscosity` 內插後四捨五入成整數（整數圈才能精準回歸 → 無縫循環的關鍵）。
- **回歸原座標**：位移 = 圈數 × 寬度 × easeInOut(t)；t=0 與 t=1 皆對齊原圖，且頭尾速度≈0 → 銜接無跳變。frame 0 即原圖。
- **GIF 播放方式（對齊拉霸的兩種模式）**：`流動時同步播放`（流沙剪切混在 **live GIF** 上做前 `Duration` 秒、之後 GIF 繼續播完剩餘；**輸出長度＝GIF 長度**；flow window 夾到 ≤GIF 長度確保流動在片內回歸對齊→無縫，等同 `BuildSlotMachinePlayDuringSpin`）或 `先流動再播放`（凍結 frame 0 流動 `Duration` 秒、再從 frame 0 播放**完整** GIF；**輸出長度＝Duration＋GIF 長度**，等同 `BuildSlotMachineSpinThenLock`）。靜態圖只走流動路徑（輸出＝Duration、循環）。⚠️ 早期版本曾誤把同步模式做成「重取樣成 `Duration×fps` 幀、截斷 GIF」（檔案異常小），已修正為上述語意。
- **方向 + 軸（單一 4 選下拉）**：向右→／向左←（水平，切橫列、`Roll(off,0)`、wrap=寬）或向下↓／向上↑（垂直，切直欄、`Roll(0,off)`、wrap=高）。`cmbDirection.SelectedIndex` 0/1=水平、2/3=垂直；`FlowRight`(=正向 roll)=index 0 或 2。「最快層位置」下拉依軸**動態改字**（水平＝下/上/中、垂直＝右/左/中），但 index→enum 映射不變（0=末層、1=首層、2=中），`CmbDirection_SelectedIndexChanged`→`RefreshFastBandLabels()` 處理。
- **軸無關引擎**：`BuildQuicksandAnimation` 分派器算 `bool vertical`、`bandTotal`（垂直=寬、水平=高）餵 `BandBounds`；兩個 build helper 共用 `CropQuicksandBand()`（依軸切橫列/直欄）+ `RollAndCompositeBand()`（依軸 `Roll(off,0)`/`Roll(0,off)` 並合成），`wrapLength`=另一個維度。
- **前置**：非 766/774 寬自動 `Resize(766,0)`；不自動分割、不自動 gifsicle（同拉霸）。
- **預設**：Layers 16、Duration 6s、FPS 15、Max 12 / Min 2 圈、FastBand 下方、Viscosity 1.0、向右流（水平）、同步播放。
- **檔案**：`src/Core/QuicksandGeometry.cs`（純函式 ease/band-bounds/speed/revolutions/offset，可單測）、`QuicksandSettings.cs`、`src/Dialogs/QuicksandDialog.cs`、`GifProcessor.QuicksandStaticImage()`/`QuicksandGif()`/`RunQuicksand()`/`BuildQuicksandAnimation()`（分派器）/`BuildQuicksandPlayDuringFlow()`/`BuildQuicksandFlowThenPlay()`、主視窗兩顆按鈕（新增第 9 列 y=255、下方元件 +31px、表單加高至 556）、三語 resx、`SteamGifCropper.Tests/QuicksandGeometryTests.cs`（22 例）。

### GIF 效果時間窗（浮點秒數 + 起始秒，拉霸 & 流沙共用）
拉霸與流沙的 GIF「同步播放」模式可指定效果在 GIF 時間軸上的 **[起始秒, 長度] 窗**（皆 2 位小數）：

- **浮點秒數**：Duration int→double（2dp），對齊最近的 frame；非同步模式（先轉/流動再播放、靜態）用 `round(秒數×fps)` 轉幀數。
- **起始秒數**：只對「同步播放」（`PlayDuring*`）生效（dialog 中 GIF + 該模式才 enable），效果在 [start, start+len] 窗內套用、窗外播放原 GIF live 幀、輸出＝GIF 全長。拉霸：轉輪在窗起點開始轉、窗內各自隨機停；流沙：剪切在窗內 ease-in/out。
- **夾擠規則**（`GifEffectWindow.Clamp`）：`len>=GIF長` → (0, 全長)；`start+len>GIF長` → start 回推到 `GIF長-len`；負 start→0。最差 start=0、len=全長。
- **共用純函式** `src/Core/GifEffectWindow.cs`（`Clamp`/`NearestFrameIndex`/`ResolveFrames`/`FramePhase`，可單測，`GifEffectWindowTests` 16 例）。拉霸 `BuildSlotMachinePlayDuringSpin` 多 `windowStartSec` 參數（轉輪由 startFrame 起算、stop=start+各輪時長）；流沙 `BuildQuicksandPlayDuringFlow` 改用 `ResolveFrames`+`FramePhase`。共用標籤 `Dialog_StartSeconds`（三語）。兩個 dialog 各加一列 Start（GIF only、play-during 才 enable），表單加高至 300。

### 水波紋 / 聲波（Water Ripple）— v1
逐像素徑向位移場（非切片）。每個輸出像素依阻尼徑向波在 C# 端算 (dx,dy)、到來源做雙線性重採樣（`Parallel.For`，繞過 Magick.NET-Q8 `Displace` 量化）。

- **模型**（`RippleField`，純函式）：每滴 = 從落點擴張的環波，波前半徑 `R=c·τ`（τ=已落下秒）。波前內每點 `amp = 強度·exp(−時間衰減·τ)·exp(−空間衰減·r)·sin(k·r−ω·τ)`，沿**徑向單位向量**推。**多滴把各自的徑向位移向量相加 → 干涉自動浮現**。某滴 envelope `強度·exp(−時間衰減·τ)` < 門檻 → 剔除（決定壽命）。輸出 = `Strength·Σ`。落點**可在圖外**（取樣 edge-clamp）。
- **介質共用 / 每滴各異**：`RippleMedium`（波速/波長/空間衰減/時間衰減/位移強度/消失門檻）全域共用——這是讓干涉良定義的關鍵；`RippleDrop`（X/Y/起始秒/強度）每滴各自。**最多 3 滴**（效能不是限制，是 dialog 與視覺清晰度）。
- **GIF 兩種模式（比照拉霸/流沙，保留 GIF 全長、原生時序）**：`波疊在播放上`＝輸出==GIF 全長、原生 delay；波在 `[0, Duration)` 窗內混在 live 幀上（窗內無作用幀用 `AnyDropActive` 跳過直接複製），`Duration` 後播原樣（`BuildRipplePlayAlong`）。`定格第一幀`＝定格 frame 0 做波 `Duration` 秒（@輸出 fps）、再播**完整** GIF（原生時序），輸出==Duration＋GIF 長度（`BuildRippleFrozenThenPlay`）。靜態圖只做波 `Duration` 秒。⚠️ 早期版本誤把跟播做成「重取樣成 `Duration×fps` 幀、截斷/循環 GIF」（4 秒就結束），已修正。
- **FPS / 播放時間**：跟播保留 GIF 原生 delay → **輸出播放時長＝來源時長**（來源 15/30fps 都不變）；輸出 FPS 設定只影響「定格/靜態」那段合成場景，不改 GIF 本身播放速度。
- **每滴自動啟用**：編輯某滴的 X/Y/起始/強度任一欄會自動勾選該滴（`ValueChanged`，在 `InitializeComponent` 後接、避免初始設值誤觸）；picker 點選也自動勾選。避免「改了強度卻沒勾、看起來沒效果」。
- **v1 未做**：邊界回波（鏡像法）—— v2 再加，只是在 drops 串列多疊 4 個對邊鏡射的衰減次波源（同一 `RippleField.Displacement` 即可吃）。
- **檔案**：`src/Core/RippleField.cs`（純函式 Envelope/DropLifetime/TotalSeconds/Displacement，可單測）、`RippleRenderer.cs`（並行雙線性重採樣，RGBA byte buffer + `ToByteArray`/`ReadPixels`）、`RippleSettings.cs`（含 `ToMedium()`）、`src/Dialogs/RippleDialog.cs`（手寫；`MakeNum` helper + decimal 字面值 + 迴圈建 3 滴列；**dialog 自己 `BuildSettings()`** 避免漏抄欄位）、`GifProcessor.RippleStaticImage()`/`RippleGif()`/`RunRipple()`/`BuildRippleAnimation()`、主視窗第 10 列兩顆按鈕（下方元件 +31、表單加高至 587）、三語 resx、`RippleFieldTests`（13 例）+ `RippleRendererTests`（3 例 smoke）。
- **預設**：Duration 4s、FPS 20、波速 220、波長 36、空間衰減 0.004、時間衰減 0.8、位移強度 8、消失門檻 0.03；第 1 滴預設開（200,150,t=0,強度1）。

### 連帶修正
- **XC coder 政策**（`f730b0a`）：`Program.cs` 的安全政策原本只允許 GIF/PNG/JPEG/BMP，誤擋了內部純色畫布產生器 `XC`，導致所有 `new MagickImage(color, w, h)`（split/merge/overlay/scroll/Coalesce 都用）失敗。XC 不是檔案解析器、無攻擊面，已加回白名單。
- **進度條**（`a69d096`）：改用自繪 `FlatProgressBar`（`UserPaint` 純色填滿），繞過原生 comctl32 的 chunk/動畫繪製（深色主題下會在填滿邊緣留下兩條移動的黑線）；並把 `SplitGif` 進度改為單調遞增（每個 part 一個 20% 區段，不再每 part 跳到 100%）。
- **移除「較快的調色盤處理」選項**：原本主視窗 + 合併/合併分割/串接 3 個對話框各有此勾選框（跳過 dithering、效益不明顯）。已全部移除 UI 與 resx，合併/串接一律用 FloydSteinberg 品質調色盤（內部 `useFastPalette`/`GifConcatenationSettings.UseFasterPalette` 等恆為 false 的休眠死分支、以及 `SplitGif` 沒人用的 `grid` 參數，皆已於後續清除）。
- **publish.cmd 修正**：原本只做增量 `dotnet publish`，會用到舊 obj 狀態 + 殘留舊本地化 DLL。改成先刪 `publish\`+`bin\`+`obj\` 再 `dotnet publish`（含 `pause`），確保 build 到最新。CI（fresh runner）本就無此問題。
- **gifsicle 只在「切成 5 份」時自動套用**：原本拉霸/格網/捲動/疊加（主面板勾選框驅動）也會自動跑 gifsicle，導致單一 766px 大檔 gifsicle 逾時。改為**只有 `SplitGif`（5 份切割）才自動 gifsicle**；串接保留自己獨立的勾選框。
- **gifsicle timeout 可調**：`GifsicleWrapper.ProcessTimeout`（原硬編 30s）改由面板新 `numUpDownGifsicleTimeout` 控制（預設 30、5–600s），存進 `GifsicleSnapshot` 套用。
- **新增「對單一 GIF 執行 gifsicle」按鈕**（`GifProcessor.OptimizeSingleGif`）：選一個 GIF，用面板 Lossy/Palette/Optimize/Dither/timeout 跑 gifsicle，輸出 `*_gifsicle.gif`（不受 chkGifsicle 或門檻限制）。
- **主視窗版面整理**：所有 operation button 統一 26px 高、改成整齊 8×2 格線（修掉 `btnMp4ToGif`/`btnScrollAnimatedGif` 重疊、`btnMergeAndSplit` 過高、移除勾選框後右側留白），下方設定與 gifsicle 面板上移貼齊。

### 合併調色盤修正（共通調色盤，顏色不失真）
- **症狀**：合併 2–5 個 GIF（含把同一張 766px 切五份再合併）顏色嚴重失真——整張變暗、退色成近灰階（已用使用者的 `mudan_Part?.gif` → `z_merged.gif` 重現並逐 frame 比對確認）。
- **根因**：`GifProcessor.BuildSharedPalette` 量化完樣本集後只回傳 `paletteSamples[0]`（單一 frame／單一裁切），`Remap` 參考調色盤因此只含那一塊區域的顏色（實測整個合併**只剩 20 色**），其他顏色全被吸附到最近的幾色 → 嚴重退色。
- **修正（兩條合併路徑）**：`MergeMultipleGifs`（合併 2–5 為一）與 `MergeGifsHorizontally`（合併並切 5 份）改為**先把所有 frame 合成進 collection，再對整個 collection 跑一次 `Quantize(256, FloydSteinberg)`**——對「實際輸出像素、跨所有 frame」算單一最佳調色盤並套用。這就是 `OverlayGif`（`resultCollection.Quantize()`）一直在用、且本就正確的做法。修正後同 frame 顏色還原（red hair / red sweater / green trees 都回來）。
- **為何不沿用「frame 0 聯集調色盤 + Remap」**：實測 frame 0 聯集只涵蓋約 99 色，無法代表 389-frame 動畫的色域；改對整個合併 collection `Quantize` 才會採樣所有 frame，是最穩、最簡、與 overlay 一致的做法。
- **對話框「調色盤來源」已移除**：`Quantize` 對整體算最佳調色盤、不偏任何來源，故「調色盤來源」下拉已無作用。已從 `MergeGifsDialog` 與 `MergeFiveGifsDialog` 兩個對話框移除該下拉與相關屬性/方法、`MergeMultipleGifs`/`MergeAndSplitFiveGifs`/`MergeGifsHorizontally` 移除對應參數、清掉 resx `MergeDialog_PaletteSource`/`MergeDialog_GifNumberFormat`。`BuildSharedPalette`（含 `primaryGifIndex`）僅保留給串接（concatenate）路徑使用。
- **串接（concatenate）未動**：`ApplyUnifiedPalette` 對每個來源各自 `Quantize`（各自獨立 256 色調色盤）。串接是**循序播放**、每段可保留各自的 local color table，本就不需要、也不該硬套單一共通調色盤（硬套反而會降低各段保真度），故維持原樣。`BuildUnifiedPalette` 的結果一直未被套用（休眠死碼，與出貨版一致，未在本次更動）。
- **疊加（overlay）未動**：`OverlayGif` 走 `resultCollection.Quantize()` 本就正確。

### 合併 2–5 GIF：來源檔尾 0x21 自動處理
- **背景**：Steam-ready 的切割檔檔尾是 `0x21`（非標準 `0x3B`），ImageMagick 讀取會 fail。
- **流程**（`MergeMultipleGifs`）：載入前先 `FlipSteamTailToStandard`（0x21→0x3B、記住動到哪些檔，寫入失敗會先把已翻的回滾再拋例外）；`finally` 一律 `RestoreSteamTail`（0x3B→0x21）還原來源。
- **錯誤處理**：翻轉寫入失敗 → 跳 `MergeDialog_TailByteFlipError` 後結束；合併中途 exception → 跳 `MergeDialog_TailFilesModifiedOnError`（列出動到的檔，皆已還原）；還原失敗 → 跳 `MergeDialog_TailRestoreWarning`（列出仍為 0x3B 的檔）。三語 resx + `Resources.Designer.cs`。

---

## 實作要點 / 踩雷紀錄（接手前必讀）

1. **ImageMagick 安全政策**（`Program.cs` `ConfigureImageMagickPolicy`）：只允許 `GIF/PNG/PNG32/JPEG/BMP/XC` coder。
   - 不要用清單外的格式 coder（SVG/PDF/TIFF… 全被擋，是刻意的）。
   - `new MagickImage(color, w, h)` 內部走 `xc:` pseudo-coder（已允許）。**向量 `Drawables` 繪製也會經過 XC** —— 本專案測試行程「不」套用此政策，所以用到 XC 的程式碼在測試會過、在 app 卻可能炸；新功能若用 Drawables 要記得這點，或改用像素寫入（見 `GridMosaicRenderer`）。
   - **`PNG32`**（與 `PNG` 同一解析器、強制 8-bit RGBA）已加入白名單：水波紋落點 picker 把 frame 0 `Write(ms, MagickFormat.Png32)` 產生預覽 bitmap，否則會擲 `not authorized ... 'PNG32'`。**注意 coder 名稱要逐一列**——`Png32` 不被 `PNG` 涵蓋。
2. **新 dialog 樣式**：鏡射 `ScrollStaticImageDialog`（inline `InitializeComponent`、無 `.Designer.cs`、`namespace GifProcessorApp : Form`、含 `UpdateUIText()` + `ApplyTheme()` + 複製 `ApplyDark/LightThemeToControls`）。流程：dialog 開在 `GifProcessor.<Op>()` 裡，`ShowDialog()==OK` 後讀公開屬性再呼叫處理方法。
3. **單元測試**：測試專案用 **stub**（`GifProcessor.Stub.cs`），並非編譯真正的 `GifProcessor.cs`，而是逐檔 `<Compile Include>` 連結無重依賴的小檔。要單測新邏輯，**把純函式抽到無依賴的獨立檔**（如 `GridMosaicGeometry.cs`），並在 `SteamGifCropper.Tests.csproj` 加一行 `<Compile Include>` 連結它。
4. **進度條**：一律用 `FlatProgressBar`（`src/Forms/FlatProgressBar.cs`），別用原生 `ProgressBar`（深色主題下填滿邊緣會有黑線/動畫殘影）。全 app 只有主視窗一條 `pBarTaskStatus`，進度都呼叫 `GifProcessor.SetProgressBar(...)`。
5. **Steam 切割座標**：`Ranges766`/`Ranges774` + `GetCropRanges()` + `SplitGif()` 都在 `GifProcessor.cs`；新「切成 5 份」類功能直接重用（`SplitGif` 已支援選用 `GridMosaicSettings grid` 參數的擴充模式，可比照加其他選用參數）。
6. **在地化**：新字串要同時加到 `Properties/Resources.resx`、`Resources.zh-TW.resx`、`Resources.ja.resx`，並在 `Resources.Designer.cs` 補強型別屬性才能編譯。
7. **建置 / 測試**：`dotnet build SteamGifCropper.sln`；測試用 `dotnet build` 後直接跑 `SteamGifCropper.Tests/bin/Debug/net10.0-windows/SteamGifCropper.Tests.exe`（`-class <Name>` 過濾）。`dotnet test` 在 .NET 10 SDK 不支援。
8. **合併共通調色盤**：要把多個不同調色盤的 GIF 併到單一 256 色而不失真，**先把所有 frame 合成進一個 `MagickImageCollection`，再對整個 collection 跑一次 `Quantize(256, FloydSteinberg)`**（採樣所有 frame 的實際輸出像素 → 單一最佳共通調色盤）。這是 overlay 一直在用的做法。**別自己用單一 frame／單一裁切去建調色盤再 `Remap`**（舊 `BuildSharedPalette` bug，整張只剩約 20 色、嚴重退色）。串接是循序播放、每段保留各自調色盤即可，不需共通調色盤。
