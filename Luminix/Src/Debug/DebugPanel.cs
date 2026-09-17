using System;
using System.Text;
using Luminix.Extensions;
using Microsoft.Xna.Framework;

namespace Luminix.Debug;

public class DebugPanel : GameObject
{
    internal DebugPanel()
    {   
        time = Workspace.Current.GetService<Time>();
        graphics = Workspace.Current.GetService<Graphics>();
        resourceProvider = Workspace.Current.GetService<Resources>();
        pixel = resourceProvider.GetResource<TextureResource>("Pixel");
        defaultFont = resourceProvider.GetResource<FontResource>("Font-Medium");

        tickTimes = new double[256];
        frameTimes = new double[256];
        managedMemoryAmounts = new double[256];
    }

    protected readonly Time time;
    protected readonly Graphics graphics;
    protected readonly Resources resourceProvider;

    protected readonly TextureResource pixel;
    protected readonly FontResource defaultFont;

    private const int HistorySize = 256;
    private const double SecondsToMillis = 1000.0;

    private readonly double[] tickTimes;
    private readonly double[] frameTimes;
    private readonly double[] managedMemoryAmounts;
    private StringBuilder sb = new StringBuilder();

    private double tickTimeAccumulated;
    private double tickTimeAvg;

    private double tickDeltaTimeAccumulated;
    private double tickDeltaTimeAvg;

    private double frameTimeAccumulated;
    private double frameTimeAvg;

    private double frameDeltaTimeAccumulated;
    private double frameDeltaTimeAvg;

    protected override void Render()
    {
        uint tickRate = time.TickRate;
        long tickCount = time.TickCount;
        double tickTime = time.TickTime;             // seconds
        long tickAllocated = time.TickAllocated;

        double frameTime = graphics.FrameInfo.Time;   // seconds
        long frameAllocated = graphics.FrameInfo.Allocated;

        long usedMemoryBytes = GC.GetTotalMemory(false);
        double usedMemoryMb = usedMemoryBytes / 1024.0 / 1024.0;

        int currentIndex = (int)((tickCount - 1) & (HistorySize - 1));

        // --- Accumulate for the rolling average window ---
        tickTimeAccumulated += tickTime;
        tickDeltaTimeAccumulated += time.TickDeltaTime;
        frameTimeAccumulated += frameTime;
        frameDeltaTimeAccumulated += graphics.FrameInfo.DeltaTime;

        // Guard against tickRate being 0 or 1, which would make interval 0
        // and crash on the modulo below.
        int interval = Math.Max(1, (int)tickRate / 2);

        if (tickCount > 0 && tickCount % interval == 0)
        {
            tickTimeAvg       = tickTimeAccumulated       / interval;
            tickDeltaTimeAvg  = tickDeltaTimeAccumulated  / interval;
            frameTimeAvg      = frameTimeAccumulated      / interval;
            frameDeltaTimeAvg = frameDeltaTimeAccumulated / interval;

            tickTimeAccumulated       = 0;
            tickDeltaTimeAccumulated  = 0;
            frameTimeAccumulated      = 0;
            frameDeltaTimeAccumulated = 0;
        }

        // --- Snapshot into ring buffers ---
        tickTimes[currentIndex] = tickTime;
        frameTimes[currentIndex] = frameTime;
        managedMemoryAmounts[currentIndex] = usedMemoryMb;

        sb.Clear();

        sb.Append("Build-Version:").Append(Application.BuildVers)
        .Append(" | Build-Date:").Append(Application.BuildDate) // fixed: was BuildVers twice
        .Append('\n');

        sb.Append(graphics.RenderSettings.Resolution.X).Append('x')
        .Append(graphics.RenderSettings.Resolution.Y).Append(" @ ")
        .Append(graphics.RenderSettings.Resolution.RefreshRate).Append("hz\n");

        sb.AppendDouble(frameDeltaTimeAvg > 0 ? double.Round(1.0 / frameDeltaTimeAvg) : 0, 0, 3);
        sb.Append(":FPS | ");
        sb.AppendDouble(tickDeltaTimeAvg > 0 ? double.Round(1.0 / tickDeltaTimeAvg) : 0, 0, 3);
        sb.Append(":TPS\n");

        sb.Append("DrawCalls      : ").Append(graphics.FrameInfo.DrawCalls).Append('\n');

        sb.Append("FrameTime      : "); sb.AppendDouble(frameTimeAvg * SecondsToMillis, 4, 2); sb.Append("ms\n");
        sb.Append("FrameDeltaTime : "); sb.AppendDouble(frameDeltaTimeAvg * SecondsToMillis, 4, 2); sb.Append("ms\n");
        sb.Append("TickTime       : "); sb.AppendDouble(tickTimeAvg * SecondsToMillis, 4, 2); sb.Append("ms\n");
        sb.Append("TickDeltaTime  : "); sb.AppendDouble(tickDeltaTimeAvg * SecondsToMillis, 4, 2); sb.Append("ms\n");

        sb.Append("Managed Memory : "); sb.AppendDouble(usedMemoryMb, 4, 2); sb.Append(" MB\n");

        sb.Append("RenderThread allocated : "); sb.AppendLong(frameAllocated, 3); sb.Append(" bytes\n");
        sb.Append("CoreThread allocated   : "); sb.AppendLong(tickAllocated, 3); sb.Append(" bytes\n");

        graphics.DrawLabel(defaultFont, sb.ToString(), 0, 0);
    }

    // protected void DrawGraph(Vector2 position, float height, double[] data, double maxValue, int valueLineThickness, int valueMargin, int lastIndex, bool invertedWidth)
    // {
    //     float graphAreaWidth = data.Length * valueLineThickness;
    //     Vector2 size = new Vector2(graphAreaWidth + (valueMargin * 2), height);
        
    //     // Draw the background
    //     if(invertedWidth)
    //         position = new Vector2(position.X - size.X, position.Y);

    //     DrawPanel(position, size, Color.Black * 0.5f);

    //     Vector2 leftUpper  = position + new Vector2(valueMargin, valueMargin);
    //     Vector2 leftLower  = position + new Vector2(valueMargin, size.Y - valueMargin);
    //     Vector2 rightUpper = position + new Vector2(size.X - valueMargin, valueMargin);
    //     Vector2 rightLower = position + new Vector2(size.X - valueMargin, size.Y - valueMargin);

    //     // 3. Graphing Constants
    //     float availableHeight = size.Y - (valueMargin * 2) - 5;
    //     float graphBottomY = leftLower.Y;

    //     for (int i = 0; i < data.Length; i++)
    //     {
    //         float xPos = leftLower.X + (i * valueLineThickness);

    //         float ratio = (float)(data[i] / maxValue);

    //         float barHeight = ratio * availableHeight;

    //         Vector2 startPoint = new Vector2(xPos, graphBottomY);

    //         int distance = (i <= lastIndex) 
    //             ? (lastIndex - i) 
    //             : (lastIndex + (data.Length - i));

    //         float fade = 1.0f - ((float)distance / data.Length);
    //         fade = MathF.Pow(fade, 1.5f); 

    //         float r = ratio;
    //         float g = 1.0f - ratio * 0.5f;
    //         float b = 0.2f;

    //         Color barColor = new Color(r, g, b) * fade;

    //         DrawVerticalLine(startPoint, -(int)(barHeight + 5), barColor, valueLineThickness);
    //     }

    //     DrawLine(leftUpper, rightUpper, Color.White, 1);
    //     DrawLine(rightUpper, rightLower, Color.White, 1);
    //     DrawLine(rightLower, leftLower, Color.White, 1);
    //     DrawLine(leftLower, leftUpper, Color.White, 1);
    // }
}