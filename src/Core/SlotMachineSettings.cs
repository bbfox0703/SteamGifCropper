namespace GifProcessorApp
{
    // Parameters captured from SlotMachineDialog and consumed by the GifProcessor slot-machine engine.
    public class SlotMachineSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }

        public double DurationSeconds { get; set; } = 3.0;     // base spinning time (2dp)
        public double StartSeconds { get; set; } = 0.0;        // GIF "play during spin" only: when spinning begins
        public int DurationVariancePercent { get; set; } = 20; // per-reel +/- swing on the stop time
        public int Fps { get; set; } = 15;
        public int Spins { get; set; } = 4;                    // base full vertical revolutions per reel
        public int SpinsVariancePercent { get; set; } = 25;    // per-reel +/- swing on the revolutions
        public double BounceSeconds { get; set; } = 0.5;       // overshoot/wobble at each reel's stop
        public bool TopToBottom { get; set; } = true;          // scroll direction
        public int HoldSeconds { get; set; } = 1;              // static variant: hold the locked image

        // GIF variant only. true: the GIF plays while the reels spin (output length == GIF length, the
        // spin consumes the first part of the GIF then it keeps playing). false: reels spin over a frozen
        // first frame, then the full GIF plays after they lock (output length == spin + GIF length).
        public bool PlayGifDuringSpin { get; set; } = true;
    }
}
