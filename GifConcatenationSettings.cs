using System;
using System.Collections.Generic;

namespace GifProcessorApp
{
    public class GifConcatenationSettings
    {
        public List<string> GifFilePaths { get; set; }
        public string OutputFilePath { get; set; }
        
        // FPS settings
        public FpsUnificationMode FpsMode { get; set; }
        public int CustomFps { get; set; }
        public int ReferenceFpsGifIndex { get; set; }
        
        // Palette settings
        public PaletteUnificationMode PaletteMode { get; set; }
        public int ReferencePaletteGifIndex { get; set; }
        public bool UseStrongPaletteWeighting { get; set; }
        
        // Transition settings (for future implementation)
        public TransitionType Transition { get; set; }
        public float TransitionDuration { get; set; }
        
        // General settings
        public bool UnifyDimensions { get; set; }
        public bool UseGifsicleOptimization { get; set; }
        public bool UseFasterPalette { get; set; }

        public GifConcatenationSettings()
        {
            GifFilePaths = new List<string>();
            FpsMode = FpsUnificationMode.AutoHighest;
            CustomFps = 30;
            ReferenceFpsGifIndex = 0;
            
            PaletteMode = PaletteUnificationMode.AutoMerge;
            ReferencePaletteGifIndex = 0;
            UseStrongPaletteWeighting = true;
            
            Transition = TransitionType.None;
            TransitionDuration = 0.5f;
            
            UnifyDimensions = true;
            UseGifsicleOptimization = false;
            UseFasterPalette = false;
        }
    }

    public enum FpsUnificationMode
    {
        AutoHighest,     // 使用最高FPS
        UseReference,    // 使用指定GIF的FPS
        Custom          // 自定義FPS
    }

    public enum PaletteUnificationMode
    {
        AutoMerge,      // 自動合併所有調色盤
        UseReference    // 使用指定GIF的調色盤為主
    }

    public enum TransitionType
    {
        None,           // 無過場轉換
        Fade,           // 淡入淡出
        SlideLeft,      // 向左滑動
        SlideRight,     // 向右滑動
        SlideUp,        // 向上滑動
        SlideDown,      // 向下滑動
        ZoomIn,         // 放大轉換
        ZoomOut,        // 縮小轉換
        Dissolve,       // 溶解效果
        CrossFade       // 交叉淡化
    }
}