namespace Luminix.Threading.Buffers;

internal sealed class RenderInfoBuffer
{
    public ushort AccumulatedFrameCount { get; set; }
    public long FrameCount { get; set; }
    public double FrameTime { get; set; }
    public double FrameDeltaTime { get; set; }
    public long Allocated { get; set; }
    public long DrawCalls { get; set; }

    public bool Updated { get; set; }

    public void Reset()
    {
        AccumulatedFrameCount = 0;
        FrameCount = 0;
        FrameTime = 0;
        FrameDeltaTime = 0;
        Allocated = 0;
        DrawCalls = 0;

        Updated = false;
    }
}
