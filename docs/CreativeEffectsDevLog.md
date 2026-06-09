# Creative Effects — Dev Log / Handoff Notes

本檔記錄創意 GIF 效果的**實作細節**（檔案清單、commit、語意、踩雷筆記），供後續 session 接手。
功能的**點子與完成狀態（roadmap）**請見 [CreativeFeatureIdeas.md](CreativeFeatureIdeas.md)。

這些「766px 單輸出、可串接」效果（格網馬賽克、拉霸、流沙、水波紋）的共同精神：把 Steam 展示櫃那 5 個並排槽位當成一整塊寬螢幕來玩，輸出單一 766px 全寬 GIF（不分割），最後再用主頁「切割 GIF」切成 5 份（切割時才加 100px 延伸 + 檔尾 0x21）。

> **基本不變式（所有特效／轉換共通）：不截斷來源影片。** 進行任何特效或轉換後，**剩下尚未播放完的 GIF 影片片段一定要繼續播到結束**。這是所有特效／轉換都必須遵守的基本條件，詳見下方「實作要點 / 踩雷紀錄」第 9 點。

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
9. **不截斷來源影片（基本不變式，所有特效／轉換必守）**：任何特效或轉換都**不得把來源影片砍短**；效果／轉換窗結束後，**尚未播放的片段必須播完**。各模式怎麼展現這條：
   - ripple / wind / quicksand / rain「同步播放」＝效果只混在 `[start, start+duration)` 窗內的 live 幀、窗外與其後照播原片（**輸出長度＝GIF 全長**）。
   - 「定格再播」＝效果做完後，再從頭播**完整**的 GIF（輸出＝效果秒數＋GIF 全長）。
   - morph A→B＝先播 A 的 PreRoll → 轉換 → 再播 **B 的剩餘片段**到結束。**唯一例外**：轉換中 A 已完全變透明消失，故 A 的剩餘不播；但接手的 B 一定要播完（總長＝PreRoll + B 全長）。
   - concat 串接＝每段都完整播放，只有 transition 的 overlap 處兩段重疊共享。
   - **早期 ripple/quicksand 曾誤把「同步播放」做成「重取樣成 `Duration×fps` 幀、截斷 GIF」（檔案異常小、4 秒就結束），已修正——這正是此不變式要防的回歸。新效果一律遵守。**

---

## 微/強風吹襲（Wind Sway，風吹麥田）— 已實作

把圖片／GIF 加上「風吹過麥田」的起伏波狀效果：一道道沿風向掃過畫面的行進波 + 陣風的時間漲落。本質是**水波紋的方向版**——把 ripple 的「點源徑向波」換成「平面行進波」，其餘（逐像素 inverse-map + bilinear 重採樣 + `Parallel.For` + edge-clamp 的渲染管線、play-during/frozen 兩種播放模式、`GifEffectWindow` 時間窗、inline dialog + theme、純函式抽出做單測、三語 resx）全部沿用 ripple。**輸出單一 766px 全寬 GIF**（不分割、可串接；非 766/774 自動 `Resize(766,0)`；要切 5 份用主頁「切割 GIF」）。

> 設計決策（與使用者確認）：① 每陣風有**獨立 duration 欄**（非僅 start+intensity）；② 核爆版用**同一 dialog 的模式切換**呈現（非獨立按鈕）。

- **為何比水波紋便宜**：平面波每像素只算 1 個風向投影 + 1~3 陣風的 sin/exp，沒有 ripple 的 per-drop `sqrt` 距離迴圈。

#### 數學模型（`WindField.cs`，純函式，link 進 test）
- 8 方向 = 行進方向單位向量 `d=(cosθ,sinθ)`（座標 +x 右、+y 下）。UI 選「**風從哪邊來**」（規格用語），內部轉成 travel = 反向。8 個 from→travel：左→(1,0)、右→(−1,0)、上→(0,1)、下→(0,−1)、左上→(.707,.707)、右上→(−.707,.707)、左下→(.707,−.707)、右下→(−.707,−.707)。
- 每像素 `p=(x,y)`、絕對時間 `t`，對每陣風 g 累加（`d_g` = forward 為 `d`、reverse 為 `−d`）：
  - `s = p·d_g`（投影到風向＝相位座標）
  - `τ = t − start_g`；`τ<0` 或 `τ>duration_g` → 不貢獻
  - `env = intensity_g · GustEnvelope(τ, duration_g)`，Hann 窗 `sin²(π·τ/duration)`：漲→盛→衰、兩端為 0 → 陣風自然吹起又停（每陣風自帶 duration，故不需 ripple 那種共用 TimeDamping 壽命）
  - `ripple = sin(k·s − ω·τ)`，`k=2π/wavelength`、`ω=waveSpeed·k`（行進的滾動波）
  - `disp += env · ( BendRatio·d_g + ripple·d_g + FlutterRatio·rippleP·perp(d_g) )`
    - 沿風向 steady bend（麥子被吹彎）+ 滾動 ripple（一波波掃過）+ 微量垂直 flutter（`perp(d)=(−d.y,d.x)`，可用稍高頻 `rippleP`）讓它不死板
  - 最後整體 `× SwayStrength`（px 位移尺度）
- 多陣風相加 → 重疊起伏／亂流自動浮現（同 ripple 的干涉）。
- 純函式：`GustEnvelope(τ,duration)`、`TotalSeconds(gusts)`（= `max(start+duration)`）、`AnyGustActive(t,gusts)`（play-along 跳過無作用幀直接複製）、`Displacement(x,y,t,gusts,medium)`。

#### 設定（`WindSettings.cs`）
- **場景**：`Fps`、`DurationSeconds`（總時間，master）、`PlayGifDuringWind`（GIF 才有意義）、`EffectStartSeconds`（效果窗起點，僅 play-during；沿用 `GifEffectWindow.Clamp/ResolveFrames`，陣風 start 相對窗起點）。
- **共用介質**：`Direction`（8 選 enum→單位向量）、`Wavelength`、`WaveSpeed`、`SwayStrength`、`BendRatio`、`FlutterRatio`。
- **模式 `Mode`：Normal | Nuclear**（dialog 切換，隱藏/顯示對應面板）。
  - Normal：`List<WindGust>`（最多 3），每陣 `{StartSeconds, DurationSeconds, Intensity, enabled}`，方向皆 forward（共用）。編欄位自動勾選（比照 ripple drop）。
  - Nuclear（**只 1 波**）：`BlastStrength/BlastDuration`、`StillGap`、`ReverseStrength/ReverseDuration`。內部展開成 2 個 gust：A `{start 0, dur=Blast, int=BlastStrength, forward}`、B `{start Blast+Gap, dur=Reverse, int=ReverseStrength, reverse=true}`。**風向反轉是「方向共用」的唯一例外，只在核爆模式發生。**

#### 播放 / mixing（重用 ripple 結構，語意同 `BuildRipplePlayAlong`/`BuildRippleFrozenThenPlay`）
- `BuildWindPlayAlong`（GIF + 同步）：**輸出 = GIF 全長、原生 delay**；風效只混在 `[EffectStartSeconds, +Duration)` 窗內的 live 幀（窗內無作用幀用 `AnyGustActive` 跳過、直接複製原幀），窗外播原片。→ 影片 15s / 風 6s / start 0 = 前 6s mixing + 後 9s 原片，完全吻合需求。
- `BuildWindFrozenThenPlay`（GIF + 定格 / 靜態圖）：定格 frame 0（或靜態圖）吹 `Duration` 秒 @ FPS，GIF 再播**完整**原片（原生時序，輸出 = Duration + GIF 長）；靜態圖只吹 `Duration`（輸出 = Duration）。

#### 渲染（`WindRenderer.cs`）
- 與 `RippleRenderer.RenderFrame` 幾乎一字不差（讀 RGBA byte buffer、`Parallel.For` 逐 row、`SampleBilinearRgba` edge-clamp、`PixelReadSettings`/`ReadPixels`/`ResetPage`），只把 `RippleField.Displacement` 換成 `WindField.Displacement`。**建議**：把 `SampleBilinearRgba` + 重採樣骨架抽成共用 helper（如 `DisplacementResampler`），ripple/wind 共用、只換位移來源 delegate。
- 上風側取樣會落在畫面外 → edge-clamp 邊緣像素（同 ripple，可接受的 smear）。

#### 檔案清單（實作時）
- `src/Core/WindField.cs`（純函式）、`WindRenderer.cs`、`WindSettings.cs`（含 8-dir enum + `ToMedium()` + nuclear→gusts 展開）
- `src/Dialogs/WindDialog.cs`（鏡射 `RippleDialog`：inline `InitializeComponent`、`namespace GifProcessorApp : Form`、`UpdateUIText()`/`ApplyTheme()`/dark-light helper、`BuildSettings(bool)`；8 方向下拉；GIF 才顯示播放模式 combo + EffectStart；模式切換顯示/隱藏 Normal vs Nuclear 面板。**不需** picker——風效是全畫面，無落點）
- `src/Core/GifProcessor.Wind.cs`（`WindStaticImage()`/`WindGif()`/`RunWind()`/`BuildWindAnimation()` 分派 + `BuildWindPlayAlong()`/`BuildWindFrozenThenPlay()`）
- `src/Forms/GTMainForm.cs` + `.Designer.cs`：`btnWindStatic`/`btnWindGif` 兩顆（第 11 列、下方元件 +31、表單加高），`UpdateUIText` 補字串，`ExecuteWithErrorHandling` 包裝（比照 `btnRippleStatic/Gif`）
- 三語 resx（`Properties/Resources.resx`/`.zh-TW.resx`/`.ja.resx`）+ `Resources.Designer.cs`：Title、各欄 label（方向/波長/波速/強度/彎曲/抖動/陣風欄頭/核爆參數）、按鈕 `Button_WindStatic`/`Button_WindGif`、模式與方向選項、`Status_WindBuilding`
- `SteamGifCropper.Tests/WindFieldTests.cs` + 在 `SteamGifCropper.Tests.csproj` 加 `<Compile Include="..\src\Core\WindField.cs">`（`GifEffectWindow.cs` 已 link）

#### 預設值（起始猜測，待視覺調校）
- Duration 6s、FPS 20、風從左來（向右吹）、Wavelength 120、WaveSpeed 300、SwayStrength 10、BendRatio 0.4、FlutterRatio 0.2
- 3 陣風：① `{0, 2.5, 1.0, on}` ② `{1.5, 3.0, 0.8}` ③ `{3.5, 2.5, 1.0}`
- 核爆：BlastStrength 1.2 / BlastDuration 0.4、StillGap 0.6、ReverseStrength 2.0 / ReverseDuration 4.0

#### 踩雷提醒（同其他創意效果）
- 純函式務必抽到 `WindField.cs` 並在 test csproj 加 `<Compile Include>`（測試用 stub，不編譯 `GifProcessor.cs`）。
- 進度條用 `FlatProgressBar`/`SetProgressBar(...)`，別用原生 `ProgressBar`。
- ImageMagick 安全政策不新增 coder（XC 純色畫布已允許）；風效不需 Png32 picker。
- 新字串三個 resx 同步加、`Resources.Designer.cs` 補強型別屬性才能編譯。

#### 實作結果與與設計的小偏離
- **檔案**（皆已建立並建置通過）：`src/Core/WindField.cs`（純函式）、`WindRenderer.cs`（ripple 重採樣的獨立複本，**未共用**——依使用者決定「ripple/wind 先獨立，日後要調整才方便」）、`WindSettings.cs`（8-dir enum + `WindMode` + `ToMedium()` + `ResolveGusts()` nuclear 展開）、`src/Dialogs/WindDialog.cs`（模式切換顯示 `_normalControls`/`_nuclearControls` 兩組）、`src/Core/GifProcessor.Wind.cs`（`WindStaticImage/WindGif/RunWind/BuildWindAnimation/BuildWindPlayAlong/BuildWindFrozenThenPlay`）。
- **主視窗**：`btnWindStatic`/`btnWindGif`（第 11 列 y=317、TabIndex 25/26），下方元件（語言鈕、資源標籤、framerate 列）+31px、`ClientSize` 587→**618**；click handler + UpdateUIText 已接。
- **在地化**：新增 **34** 個 `Wind*` 鍵（三語 resx + `Resources.Designer.cs`）。
- **時間包絡**：採 Hann 窗 `sin²(π·τ/duration)`（如設計）。**Nuclear 反向腳**透過 `WindGust.Reverse`（風向乘 −1）。
- **測試**：`SteamGifCropper.Tests/WindFieldTests.cs`（**17** 例：方向向量、Hann 包絡、TotalSeconds、AnyGustActive、位移為零/沿風向/隨強度/疊加加倍/反向取負、`ResolveGusts` nuclear 展開、`ToMedium`），csproj link `WindField.cs` + `WindSettings.cs`。全 **192** 例綠燈。
- **csproj NoWarn**：因把 `WindSettings.cs`（與 `RippleSettings.cs` 同樣未標註 nullable 的 string 屬性）link 進 Nullable-enabled 的測試專案，測試專案 `NoWarn` 加上 `CS8618`（生產組件 Nullable 關閉、無此警告）。
- **未做 picker**：風是全畫面、無落點，故無 RippleDropPicker 等價物（如設計）。

---

## 通用尺寸開關：「保持原始尺寸（不縮到 766px）」— 已實作

水波紋 / 風 / 流沙的底層 math 本來就吃任意寬高，766 只出現在入口的 auto-resize 守門。加一個 per-dialog 勾選框，讓這三個效果可當**通用 GIF 特效**用（非 Steam 前置）。

- **範圍**：僅 ripple / wind / quicksand（真正尺寸無關）。**拉霸 / 格網維持 766-only**——它們的語意是 5 個 Steam 槽位（`Ranges766/774`、`reelCount = ranges.Length = 5`），「自由尺寸」沒有明確定義，要做得另開「N 等分欄」的題目。
- **設定**：`RippleSettings`/`WindSettings`/`QuicksandSettings` 各加 `bool KeepOriginalSize`（預設 `false` ＝維持現況）。三個 dialog 各加 `chkKeepSize`（共用字串 `Dialog_KeepOriginalSize`）。
- **引擎**：`RunRipple`/`RunWind`/`RunQuicksand` 把 auto-resize 條件改成 `!settings.KeepOriginalSize && !IsValidCanvasWidth(width)`，其餘完全不變（math 已通用）。
- **記憶體護欄**（共用 `GifProcessor.cs`）：`EstimatePeakMemoryMB(frames,w,h) = 2×幀×寬×高×4`（兩份 RGBA：coalesced 來源 + 輸出 collection）；`ConfirmLargeCanvas(...)` 在估值 > `max(512MB, ResourceLimits.Memory×0.6)` 時 marshal 到 UI thread 跳 Yes/No 警告（`Warn_LargeMemory`/`Warn_LargeMemoryTitle`），取消則 `canceled = true` 提前 return、不顯示成功訊息（成功訊息已包進 `if (!canceled)`）。766px 時估值極小、等同 no-op。`outFrames` 取 `max(來源幀, 輸出幀)`：play-during = 來源長度；frozen/static = `Duration×fps`(+GIF)。
- **連帶修正**：`QuicksandDialog` 的 dark/light 主題原本沒處理 `CheckBox`（深色下會黑字黑底看不見），已補上 CheckBox 分支；ripple/wind dialog 本來就一起處理 `Label || CheckBox`。
- **取捨（重要）**：`KeepOriginalSize` 的輸出若非 766/774，**餵不進主頁「Split GIF」**（Steam 5 切擋寬度）——這正是這開關的用意（通用、非 Steam）。
- **檔案**：3 settings、3 dialog、`GifProcessor.cs`（兩個 helper）、3 個 `Run*`、3 resx + `Resources.Designer.cs`（3 新鍵）。建置 0 warning，全測試綠燈。

### 合併記憶體強化（合併 2–5、合併並切 5）— 已實作

合併是全工具最吃記憶體的操作：它要把**同一時刻**各檔的 frame 併排合成，所以必須**同時**握住所有來源的全部影格＋整個合併結果（再對結果跑一次 `Quantize`）。早期 24–40GB 的失控是在設 `ResourceLimits`（PR #89/#91，`f64f268`/`afae4e6`）+ 修 leak（`b202393`）之前；現在全程走 Magick pixel cache → 受 4GB/8GB 上限管，溢寫磁碟、超過才丟**可攔截**的 `cache resources exhausted`（跳錯誤框、非 crash）。本次再做兩件事降峰值：

- **提早釋放來源**：
  - `MergeMultipleGifs`：所有 merged frame 合成完後、`Quantize` 前就 `Dispose()` 來源 `collections`（之後只用 `mergedCollection`），把 Quantize/Write 期間峰值大致砍半。
  - `MergeAndSplitFiveGifs`：它更重（`collections`→`resizedCollections`→`syncedCollections`→merged `output` 共 ~4 份 clone）。改成 resize 完就釋放 `collections`、sync 完就釋放 `resizedCollections`（最外層 `finally` 仍會再 dispose 一次——`MagickImageCollection.Dispose` 為 idempotent，安全）。
- **事前警告**：兩條路徑都在動工前用共用的 `ConfirmLargeMemory(mainForm, estMb, w, h, frames)`（從 `ConfirmLargeCanvas` 抽出）估算（合併=Σ來源像素＋結果像素；合併並切 5≈來源×2 的 clone 重疊），超門檻跳 Yes/No、取消則提前 return（外層 finally 照樣 dispose／還原 0x21）。`ConfirmLargeMemory` 改用 `InvokeRequired` 判斷，UI thread 直接跑、背景 thread 才 marshal（合併的呼叫點在 UI thread）。
- **結論**：合併不會再失控吃到 24–40GB；最壞是走磁碟變慢或丟可攔截的錯誤。前提仍是 temp 磁碟要有 ~8GB 空間。

---

## 下雨疊層特效（Rain Overlay）— 已實作

在圖片／GIF 上**疊一層半透明雨絲**（注意：是 overlay 合成，**非**像素位移；與 ripple/wind 的折射不同）。比照 wind 的整體骨架（入口/Run/分派/play-along/frozen-then-play、`GifEffectWindow` 時間窗、inline dialog + theme、純函式抽出單測、三語 resx、輸出單一 766px 全寬可串接）。

- **渲染慣例**：repo 刻意不用 Magick `Drawables`（XC 政策 + Q8 量化），故 `RainRenderer` 直接把雨絲以 **DDA 畫線 + alpha-blend 寫進 RGBA `byte[]`**（同 ripple/wind 的 buffer 寫法），非 Drawables。雨色淺藍白、stroke alpha 0.55 × 層淡出。
- **數學模型**（`RainField.cs`，純函式）：`DropCount(amount,w,h)` 由 0..100 雨量 × 畫布面積換算（夾 40..1500）；每滴用**種子化 hash**（`Hash01(i,salt,seed)`，無 RNG 狀態 → 純函式可重現）取 x/相位/速度因子/長度因子；`Streaks(t,w,h,p)` 回傳線段陣列（head→tail，tail 沿速度反向 = motion-blur），head 在畫布 + 40px 上緣 margin 內**垂直/水平 wrap**（連續且循環）；風向/風強度給橫向 drift → 雨絲傾斜。`FadeAlpha(te,winDur,fadeOut,fadeSeconds)`＝雨停在窗尾 `fadeSeconds` 內 1→0 線性淡出；`AnyRainActive` 讓無雨幀直接複製。
- **設定**（`RainSettings.cs`，`ToParams(w,h)` 把尺寸無關值解析成 `RainParams`）：`RainAmount`(0..100)、`WindDirection`(None/Left/Right enum→index)、`WindStrength`(px/s)、`DropLength`、`FadeOut`+`FadeOutSeconds`、`Seed`，＋場景 `Fps/DurationSeconds/EffectStartSeconds/PlayGifDuringRain/KeepOriginalSize`。
- **播放**：`BuildRainPlayAlong`（雨混在 `[EffectStart,+Duration)` 窗內的 live 幀、窗外播原片、**輸出=GIF 全長**；雨絲用**絕對時間** `startSec[i]` 連續移動、淡出用窗內**相對時間** `te`）／`BuildRainFrozenThenPlay`（定格 frame 0 下雨 `Duration` 秒 @ FPS、再播完整 GIF；靜態圖只下雨 `Duration`）。
- **檔案**：`src/Core/RainField.cs`/`RainSettings.cs`/`RainRenderer.cs`/`GifProcessor.Rain.cs`、`src/Dialogs/RainDialog.cs`（鏡射 `WindDialog`：`MakeNum`/`UpdateUIText`/`ApplyTheme`/dark-light、`BuildSettings(bool)`、`chkFadeOut`→`numFadeSeconds` enable）、主視窗 `btnRainStatic`/`btnRainGif`、`RainFieldTests`（6 例）。

## 疊圖轉換 A→B（Morph Transition）— 已實作

兩個 clip 的轉場，採**全新時間模型**（與既有 concat 的 overlap 模型不同，故獨立 dialog + 單顆按鈕，使用者已同意「另開 topic」）：A 先播 `PreRoll` 秒 → A 在 `Morph` 秒內轉成 B（**轉換結束時 A 已全透明消失、剩餘 A 不播**）→ 播 B 剩餘片段到結束。

- **時間軸恆等式**：`total = PreRoll + Morph + (Bdur − Morph) = PreRoll + Bdur`（當 `Morph ≤ Bdur`；否則 `Morph` 夾到 `Bdur`、無剩餘 B）。例 A=10s/B=11s/PreRoll=4/Morph=6 → 4+6+5 = **15s**。純函式 `MorphTimeline.ClampMorph/TotalSeconds`（在 `MorphSettings.cs`）可單測。
- **兩風格**（同一 dialog 的 `cmbStyle` 切換兩組控制項，比照 wind 的 Normal/Nuclear）：
  - **雨滴暈染**（`RaindropRevealField`/`RaindropRevealRenderer`）：種子化雨滴（birth∈[0,0.85)、隨機位置、`MaxR=SpreadRadius×(1±SizeVar%)`）；`Coverage(x,y,t)∈[0,1]`＝各 born 雨滴 `SmoothStep(age)` 成長的 **soft disc union**（soft edge 即「暈開」羽化），疊上 `GlobalFloor(t)`（最後 15% ramp→1，**保證 t=1 全為 B**），對固定像素**單調遞增**。Renderer 逐像素 cross-dissolve `out = A·(1−cov) + B·cov`（`Parallel.For`、RGBA byte[]）。
  - **翻轉拼圖**（`TileFlipGeometry`/`TileFlipRenderer`）：`ComputeGrid(w,h,divisions)`＝cols=divisions、**rows 自動算成近正方格**；`CellPhase(index,t,seed,flipFraction=0.35)` 用種子 scatter 錯開每格起翻時間（t=1 時全部=1 → 全翻成 B）；`CellScale(phase)=|1−2·phase|`（phase 0.5 時壓成邊緣 sliver）；`CellShowsB(phase)=phase≥0.5`；`CellAxis`（Up/Down 垂直壓、Left/Right 水平壓、Random 種子選軸）。Renderer 逐格 `Crop→ResetPage→Resize(IgnoreAspectRatio 壓扁)→置中 Composite`。
- **引擎**（`GifProcessor.Morph.cs`）：載入 A/B `Coalesce`；target = A 尺寸（KeepOriginalSize）或 fit 766px 寬；B 各幀以 `FitToCanvas`（保留 aspect、置中、**沿用來源 delay/ticks**）對齊 target，使 morph 能逐像素混合；用 `FrameStartSeconds` 累積秒數 + `GifEffectWindow.NearestFrameIndex` 在 morph 窗內依時間取 A/B 幀（`tA=PreRoll+k/fps` 超過 A 尾自動**凍結最後幀**、`tB=k/fps`）；三段組裝（pre-roll A 原生時序 → N=`round(Morph×fps)` 個 morph 幀 @ delay=`round(100/fps)` → 剩餘 B 原生時序）。
- **設定**（`MorphSettings.cs`）：`Style`、`PreRollSeconds`、`MorphSeconds`、`Fps`、`KeepOriginalSize`、`Seed`；雨滴組 `RainIntensity/DropSizeVariationPct/SpreadRadius/SoftEdge`（`SpreadVariationPct` 保留未用）；翻轉組 `Divisions/FlipDirection`。
- **檔案**：`src/Core/MorphSettings.cs`(含 `MorphTimeline`)/`RaindropRevealField.cs`/`RaindropRevealRenderer.cs`/`TileFlipGeometry.cs`/`TileFlipRenderer.cs`/`GifProcessor.Morph.cs`、`src/Dialogs/MorphTransitionDialog.cs`（A/B 兩個輸入 + 輸出、`cmbStyle` 切換 `_raindropControls`/`_tileControls`）、主視窗 `btnMorphTransition`（單顆、整寬）、`RaindropRevealFieldTests`(3)/`TileFlipGeometryTests`(8)/`MorphTimelineTests`(5)。

#### 主視窗版面（rain + morph 一起）
- `btnRainStatic`/`btnRainGif`（第 12 列 y=348、TabIndex 27/28）、`btnMorphTransition`（第 13 列 y=379、整寬 606、TabIndex 29）。
- 下方既有元件統一 **+62px**：語言鈕 349→411、資源標籤 351→413、framerate 列 375→437 / 377→439、gifsicle 面板 400→462、status 519→581、進度條 534→596；`ClientSize` 618→**680**。
- 在地化新增 **34** 鍵（三語 resx + `Resources.Designer.cs`）：`Button_Rain*`/`Status_RainBuilding`/`RainDialog_*`/`RainDir_*`、`Button_MorphTransition`/`Status_MorphBuilding`/`MorphDialog_*`/`MorphStyle_*`/`FlipDir_*`。`csproj` test 連結新增 5 個純檔（`RainField`/`RainSettings`/`RaindropRevealField`/`MorphSettings`/`TileFlipGeometry`）+ 3 個 Magick renderer 做 smoke test（`RainRenderer`/`RaindropRevealRenderer`/`TileFlipRenderer`，見 `MorphRainRendererTests`）。build 0 warning、測試全綠。

## SIMD 加速評估（本輪：維持 Parallel.For，延後）

決策：本輪**不**做向量化（使用者選「先用 Parallel.For，SIMD 之後再評估」）。

- **熱點候選**（raw RGBA `byte[]`、SIMD 友善）：`RippleRenderer`/`WindRenderer.SampleBilinearRgba`（4-channel 雙線性權重）、`RaindropRevealRenderer` 的逐像素 cross-dissolve。
- **效益低、不值得**：Rain 雨絲（稀疏 DDA 畫線、非密集像素迴圈）、TileFlip（瓶頸在 Magick `Crop/Resize/Composite`，非自寫迴圈）。
- **日後若要動手**：`SteamGifCropper.csproj` 加 `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`，用 `System.Numerics.Vector<T>` 或 `System.Runtime.Intrinsics`（Vector256/Avx2）；**先用 `Stopwatch` benchmark** 確認時間真的花在像素迴圈（而非 ImageMagick 內部 `Composite`，那本身可能已向量化），再決定向量化哪個 kernel。現有 `Parallel.For` 已跨 row 平行，先確保正確性。

## 驗證 & 後續 UI 微調（已實機測試 OK）

- **實機驗證**：使用者已在 app 內實測下雨、雨滴暈染、翻轉拼圖三項，視覺與行為皆正常（OK）；預設值經實測可用，非單純猜測。
- **Morph 時間語意（實測確認）**：A 先單獨播 `PreRoll`；B 在**轉換開始那一刻才從自身 frame 0 進場**（非與 A 同時起跑），故 B 全長都會被播到、A 的剩餘在轉換後丟棄。`tA=PreRoll+k/fps`（A 接續）、`tB=k/fps`（B 從 0）。
- **後續 UI 微調**（同批 commit）：
  - 移除 **流沙/水波紋/風吹** 對話框標題的「（766px）」（`QuickDialog_Title`/`RippleDialog_Title`/`WindDialog_Title`，三語）；拉霸保留（語意為 Steam 5 槽位）。
  - 加寬被 CJK 截斷的「（秒）」標籤：Rain `時間長度`、Ripple `時間長度`、Morph `先播 A`/`轉換`、Wind `時間長度`、Quicksand `時間長度`（label 寬度按日文最寬語系抓，連帶右移同列數值框）。
  - Morph「保持原始尺寸（不縮到 766px）」原擠在 PreRoll/Morph/FPS 同列被截斷 → 移到「樣式」那一列、寬度給足。
  - 下雨播放模式下拉改用 `RainDialog_GifPlayDuring`/`RainDialog_GifFreeze`（原借用 `WindDialog_*` 字串，顯示成「風疊在播放上」）。
- **最終狀態**：build 0 warning、xUnit 測試全綠。

## 疊圖轉換再加 2 風格：Spotlight / Jigsaw（已實機測試前）

延伸 `MorphStyle`（現 4 種：RaindropReveal / TileFlip / **Spotlight** / **Jigsaw**），共用既有 morph 引擎（PreRoll + 轉換窗 + 剩餘 B 三段、`GifProcessor.Morph.cs` 的 `switch(Style)` 分派、同一 `MorphTransitionDialog` 用 `cmbStyle` 切 4 組控制項）。兩者都是逐像素 cross-dissolve（RGBA byte[] + `Parallel.For`），t=1 保證全 B。

### Spotlight（聚光燈）
一顆圓形聚光燈像撞球般在畫面內等速移動、撞四邊反彈，**只在照到處顯示 B**（非累積——光移開該處回到 A）；最後 `ExpandSeconds` 內圓心凍結、半徑長到畫布對角線把整面填成 B。
- **`SpotlightField.cs`（純函式）**：`Bounce(startFrac,v,tSec,lo,hi)` 用三角波解析式做 1D 反彈（無逐步漂移、可重現）；`Center` 對 x/y 各跑一條（vy 乘上種子係數讓 x/y 週期不同 → 掃得較像隨機而非單一對角線），進入 expand 後凍結；`RadiusAt` 移動期＝設定半徑、expand 期 SmoothStep 長到 `sqrt(w²+h²)`（**從任何圓心都覆蓋整面 → 末幀全 B**）；`Coverage` 為 soft 圓。`ExpandFrac=1−ExpandSeconds/MorphSeconds`。半徑用 `ClampRadius` 夾到 `min(w,h)/2−1`（圓心仍可達各邊）。
- **`SpotlightRenderer.cs`（Magick）**：每幀算一次 center/radius，逐像素 `out=A·(1−cov)+B·cov`。
- **設定**：`SpotlightRadius`(光圈大小 px)、`SpotlightSpeed`(px/sec)、`SpotlightExpandSeconds`(末段擴大秒數，須>0 才會收尾全 B)、`SpotlightSoftEdge`。速度用 px/sec → 引擎傳 `tNorm` 與 `morphSeconds`，內部 `tau=tNorm·morphSeconds`。
- **語意決策**：採「移動式探照（非累積）+ 末段擴大填滿」（spec「到設定秒數後圓形擴大、最後顯示整個 B」即暗示非累積，否則擴大多餘）。

### Jigsaw（拼圖）
底層 A，拼圖區塊以**種子 scatter 順序逐塊**淡入顯示 B（每塊在自己的小 fill 窗 cross-fade），組裝過程畫出區塊邊界線（可指定色或不顯示＝透明），**全拼完時邊界線淡出消失**。
- **`JigsawGeometry.cs`（純函式）**：`PiecePhase(index,t,seed,fill)`（錯開起拼、t=1 全為 1＝全 B，salt 與 tile flip 不同）、`LineAlpha(t,fadeStart)`（fadeStart 前全亮、之後 1→0、t=1 為 0）。格子共用 `TileFlipGeometry.ComputeGrid`（近正方、`Divisions` 即區塊數，與翻轉共用該欄位）。
- **`JigsawRenderer.cs`（Magick）**：預建整數邊界 `colEdge/rowEdge` + 每像素 `colOf/rowOf` 查表 → 取 `PiecePhase` 當 cov 混合；之後若 `JigsawShowLines` 才在內部邊界畫線（色 `JigsawLineR/G/B`、alpha=`LineOpacity×LineAlpha(t)`），寫進同一 RGBA buffer。
- **設定**：`Divisions`（區塊數，dialog 用獨立 `numJigsawPieces` 但寫回同一 `Divisions`）、`JigsawShowLines`、`JigsawLineR/G/B`。dialog 用 `ColorDialog` + 一個 `Panel` 色票（Panel 不在 theme 切換內 → 色票顏色不被深色主題蓋掉）；取消勾選＝透明（不畫線）。

### Dialog / 在地化 / 測試
- `MorphTransitionDialog`：`cmbStyle` 加 2 項（enum 順序＝combo 順序，`Style=(MorphStyle)SelectedIndex`）；`_spotlightControls`/`_jigsawControls` 兩組與既有兩組共用同一 y 帶、依 style 顯示；Jigsaw 色票 `pnlJigsawColor`（`chkJigsawLines` 連動 enable）。
- 新增 **8** 鍵（三語 + Designer）：`MorphStyle_Spotlight/Jigsaw`、`MorphDialog_SpotRadius/SpotSpeed/SpotExpand/JigsawPieces/JigsawShowLines/JigsawLineColor`。
- 測試：`SpotlightFieldTests`(6：bounce 範圍、center 邊界、expand 凍結、末幀半徑=對角線、coverage 內/外、末幀全 B)、`JigsawGeometryTests`(3：piece phase 起迄+單調、line alpha)、`MorphRainRendererTests` 加 spotlight/jigsaw 各 3 例 smoke。test csproj 連結 `SpotlightField`/`JigsawGeometry`(純) + `SpotlightRenderer`/`JigsawRenderer`。build 0 warning、全 **236** 例綠燈。

## 疊圖轉換再加 1 風格：Brick（疊磚／木板掉落）

`MorphStyle` 第 5 種。把畫面沿掉落軸切成 N 片「木板」，**底圖 A 持續播放**，B 的木板由遠端一片片掉下來、撞底**彈跳**後疊好，全部疊完＝全 B。每片木板畫的是**它最終位置的 B 切片**（掉落過程中內容固定，不是掃過區段的內容）。

- **`BrickField.cs`（純函式物理）**：每片 drop order `d` 的掉落距離 `dist(d)=|dest−start|`（最遠的先掉、掉最久），fall 時間 ∝ `sqrt(dist)`（自由落體），錯開成 stagger（**前一片落到定位、下一片就開始掉**，故下一片起掉時前一片還在彈）。落地後 `τ` 內阻尼彈跳 `amp·e^(−decay·τ)·|sin|`：`amp` 由**衝擊速度**（`g`＋掉落高度 `H_m` → `v=sqrt(2g·dist_m)`）× **硬度** 決定、`decay` 由**重量**決定（愈重愈快靜止）。**最後 20%（最後掉的那幾片）不彈**。整段 normalize 到 morph 窗 `[0,1]`（**`MorphSeconds` 定整體節奏**、物理參數塑形「加速度＋彈跳手感」），故 `t=1` 必全片定位＝全 B。4 方向（上下左右）：`forward=(Down||Right)` 由低座標端掉入、堆在高端；`!forward` 反之；公式統一。
- **`BrickRenderer.cs`（Magick）**：clone A 當底，對每片 started 的木板 `Crop` B 的目的切片、`Composite` 到目前位置（掉落中可在畫面外，Magick 自動裁切；負偏移可用，比照 slide transition）。依 drop order 畫 → 正在空中的（最晚起掉）疊在已堆好的上面。
- **設定**（`MorphSettings` 的 brick 欄位，`ToBrickParams()`）：`BrickPieces`(塊數)、`BrickDirection`(上下左右)、`BrickTotalHeightM`(高度 m)、`BrickGravity`(g)、`BrickWeight`(重量)、`BrickHardness`(硬度 0..100→0..1)。dialog 第 5 組控制項 + `cmbBrickDir`（4 方向，enum 序＝combo 序）。
- **設計決定**：採「`MorphSeconds`＝整體掉落時長（normalize）、物理只塑形落速曲線＋彈跳」而非「物理回推絕對總長」——這樣 morph 窗語意對所有風格一致、不必停用 `轉換（秒）` 欄；`g`／高度主要透過**彈跳大小**（衝擊速度）展現（掉得高/重力大→撞擊大→彈得大），符合「重物丟下來」的手感。
- **測試**：`BrickFieldTests`(6：末幀全定位、切片無縫鋪滿、起始第一片在畫面外其餘未起、stagger 隨時間增多起掉數、方向決定先掉哪片、IsVertical)、`MorphRainRendererTests` 加 brick 4 方向 smoke。test csproj 連結 `BrickField`(純) + `BrickRenderer`。build 0 warning、測試全綠。
