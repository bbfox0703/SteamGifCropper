namespace GifProcessorApp
{
    // Direction the bands flow. Horizontal slices the image into stacked rows that wrap-scroll left/
    // right; Vertical slices it into side-by-side columns that wrap-scroll up/down. The per-band math
    // (QuicksandGeometry) is axis-agnostic — only the crop/roll/composite axis differs in the engine.
    public enum QuicksandFlowAxis
    {
        Horizontal = 0,
        Vertical = 1,
    }

    // Parameters captured from QuicksandDialog and consumed by the GifProcessor quicksand engine.
    // Output is a single full-width 766px GIF (no auto-split) so it can be chained with other effects;
    // split into 5 parts later with the main "Split GIF" button.
    public class QuicksandSettings
    {
        public QuicksandFlowAxis Axis { get; set; } = QuicksandFlowAxis.Horizontal;
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }

        public int Layers { get; set; } = 16;            // horizontal bands the image is sliced into
        public double DurationSeconds { get; set; } = 6.0; // length of one full flow-and-return cycle (2dp)
        public double StartSeconds { get; set; } = 0.0;  // GIF "play during flow" only: when the flow begins
        public int Fps { get; set; } = 15;
        public int MaxRevolutions { get; set; } = 12;    // whole turns of the FAST band over the cycle
        public int MinRevolutions { get; set; } = 2;     // whole turns of the SLOW band over the cycle
        public QuicksandFastBand FastBand { get; set; } = QuicksandFastBand.Bottom; // where flow is fastest
        public double Viscosity { get; set; } = 1.0;     // gradient gamma (>1 = stickier slow bands)
        public bool FlowRight { get; set; } = true;      // scroll direction

        // GIF variant only. true: the GIF plays while the bands flow (each output frame shears the live
        // GIF frame at that time). false: the bands flow over a frozen first frame.
        public bool PlayGifDuringFlow { get; set; } = true;

        // true: process at the input's native size instead of fitting to 766px (general-purpose use,
        // not Steam prep — the output then won't feed the 766/774-only "Split GIF").
        public bool KeepOriginalSize { get; set; } = false;
    }
}
