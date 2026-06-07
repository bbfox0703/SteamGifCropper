namespace GifProcessorApp
{
    // Parameters captured from SlotMachineDialog and consumed by the GifProcessor slot-machine engine.
    public class SlotMachineSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }
        public int DurationSeconds { get; set; } = 3;   // length of the spinning phase
        public int Fps { get; set; } = 20;
        public int Spins { get; set; } = 4;             // full vertical revolutions per reel
        public double StaggerSeconds { get; set; } = 0.3; // gap between adjacent reels stopping
        public int HoldSeconds { get; set; } = 1;       // static variant: hold the locked image
    }
}
