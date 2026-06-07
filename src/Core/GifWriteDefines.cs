using System.Collections.Generic;
using ImageMagick;

namespace GifProcessorApp
{
    internal sealed class GifWriteDefines : IWriteDefines
    {
        public int RepeatCount { get; set; } = 0;
        public MagickFormat Format => MagickFormat.Gif;
        public IEnumerable<IDefine> Defines
        {
            get
            {
                yield return new MagickDefine(Format, "repeat", RepeatCount);
            }
        }
    }
}
