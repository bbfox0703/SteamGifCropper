using System.Collections.Generic;
using ImageMagick;

namespace GifProcessorApp
{
    internal enum GifWriteMode
    {
        Gif,
        Frame
    }

    internal sealed class GifWriteDefines : IWriteDefines
    {
        public GifWriteMode WriteMode { get; set; } = GifWriteMode.Gif;
        public int RepeatCount { get; set; } = 0;
        public MagickFormat Format => MagickFormat.Gif;
        public IEnumerable<IDefine> Defines
        {
            get
            {
                yield return new MagickDefine(Format, "write-mode", WriteMode == GifWriteMode.Gif ? "gif" : "frame");
                yield return new MagickDefine(Format, "repeat", RepeatCount);
            }
        }
    }
}
