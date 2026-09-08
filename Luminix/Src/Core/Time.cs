using System;

namespace Luminix;

public sealed class Time : Workspace.Service
{
    internal Time() { }

    public uint TickRate { get; set; }
    public long TickCount { get; internal set; }
    public double TickTime { get; internal set; }
    public double TickDeltaTime { get; internal set; }
    public long TickAllocated { get; internal set; }
}