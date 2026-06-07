using System.Drawing;

namespace GifProcessorApp
{
    public enum GridLineStyle
    {
        Transparent,    // 透明格線（透出 Steam 個人檔案背景）
        Solid           // 實心色格線
    }

    public class GridMosaicSettings
    {
        public string InputFilePath { get; set; }
        public int ColumnsPerSlot { get; set; }   // 每個切割槽位內的欄數
        public int Rows { get; set; }              // 列數
        public int LineWidth { get; set; }         // 格線寬度（px），預設等於 Steam 間隙寬
        public GridLineStyle Style { get; set; }
        public Color LineColor { get; set; }       // Style 為 Solid 時使用（避免 dialog 耦合 Magick 型別）

        public GridMosaicSettings()
        {
            InputFilePath = string.Empty;
            ColumnsPerSlot = 4;
            Rows = 5;
            LineWidth = 4;
            Style = GridLineStyle.Transparent;
            LineColor = Color.Black;
        }
    }
}
