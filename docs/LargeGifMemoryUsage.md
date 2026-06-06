# Large GIF Memory Usage

This project demonstrates techniques for handling very large GIF files without
consuming excessive memory.

## FFmpeg streaming

Operations that invoke FFmpeg now use pipe-based streaming so that frames are
processed sequentially. This avoids loading an entire GIF into memory.

```csharp
await FFMpegArguments
    .FromPipeInput(new StreamPipeSource(inputStream))
    .OutputToPipe(new StreamPipeSink(outputStream), options => options
        .ForceFormat("gif")
        // additional options
    )
    .ProcessAsynchronously();
```

## Magick.NET resource limits

Magick.NET respects configured `ResourceLimits`. The test
`LargeGif_RespectsResourceLimits` creates a large GIF while limiting memory to
8 MB, demonstrating that processing falls back to temporary disk storage.

Run the test with:

```bash
dotnet test --filter LargeGif_RespectsResourceLimits
```

This verifies that large GIFs can be processed under strict memory
constraints.
