using Verse.Threads;

namespace Verse.Simulation;

public class World
{
    private static CoreThread coreThread;
    internal static void Initialize(CoreThread coreThread)
    {
        World.coreThread = coreThread;
    }

    public static uint TickRate
    {
        set { coreThread.TickRate = value; }
        get { return coreThread.TickRate; }
    }

    public static ulong TickCount => coreThread.TickCount;
    public static uint TickTimeInUS => coreThread.TickTimeInUS;
    public static uint AvgTickTimeInUS => coreThread.AvgTickTimeInUS;

    public static float PartialTick => coreThread.PartialTick;
}
