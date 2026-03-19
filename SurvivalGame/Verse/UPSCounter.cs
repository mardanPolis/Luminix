
namespace Verse;

public sealed class UPSCounter
{
    public static int Curr { get; private set; } = 0; // current
    public static int High { get; private set; } = 0; // high
    public static int Avg { get; private set; } = 0; // avg
    public static int Low { get; private set; } = 0; // low

    private int[] frameTimes = new int[1000];

    public void StartUpdate()
    {
        
    }

    public void EndUpdate()
    {
        
    }
}