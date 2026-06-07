// Several tests (e.g. MergeGifTests, LargeGifMemoryTests) deliberately mutate ImageMagick's
// process-GLOBAL ResourceLimits (memory/disk) to small values to exercise constrained-resource
// paths, restoring them in a finally block. Under xUnit's default parallel-by-collection
// execution those global mutations stomp on ImageMagick operations running concurrently in
// other test classes, producing non-deterministic "cache resources exhausted" failures
// (e.g. LargeGifMemoryTests pins the global limit to 8 MB while a merge runs in parallel).
//
// ImageMagick's ResourceLimits, temp directory and native pixel cache are all process-global,
// so these tests cannot safely run in parallel. Serialize the whole assembly: each test then
// owns the global limits for its duration.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
