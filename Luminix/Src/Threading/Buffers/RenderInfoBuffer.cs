using System;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix.Threading.Buffers;

internal sealed class RenderInfoBuffer
{
    public uint FrameRate { get; set; }
    public ulong FrameCount { get; set; }
    public short AccumulatedFrameCount { get; set; }
    public double FrameTime { get; set; }
    public double FrameDeltaTime { get; set; }
    public long Allocated { get; set; }
    public long DrawCalls { get; set; }

    public bool Updated { get; set; }

    public void Reset()
    {
        FrameRate = 0;
        FrameCount = 0;
        AccumulatedFrameCount = 0;
        FrameTime = 0;
        FrameDeltaTime = 0;
        Allocated = 0;
        DrawCalls = 0;

        Updated = false;
    }
}
