using System.Collections.Generic;
using ImageMagick;

namespace GifProcessorApp
{
    internal enum GifWriteMode
    {
        Gif,
        Frame,
        LastFrame
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
                var mode = WriteMode switch
                {
                    GifWriteMode.Gif => "gif",
                    GifWriteMode.Frame => "frame",
                    GifWriteMode.LastFrame => "last",
                    _ => "gif"
                };

                yield return new MagickDefine(Format, "write-mode", mode);
                yield return new MagickDefine(Format, "repeat", RepeatCount);
            }
        }
    }
}
