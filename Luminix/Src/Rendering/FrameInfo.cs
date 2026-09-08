namespace Luminix.Rendering;

public struct FrameInfo
{
    internal FrameInfo(long index, double time, double deltaTime, long allocated, long drawCalls)
    {
        Index = index;
        Time = time;
        DeltaTime = deltaTime;
        Allocated = allocated;
        DrawCalls = drawCalls;
    }

    public long Index { get; }
    public double Time { get; }
    public double DeltaTime { get; }
    public long Allocated { get; }
    public long DrawCalls { get; }
}