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

    private int tickSkipped;

    protected override void Update()
    {
        
    }

    protected override void Render()
    {
        float gHeight = 200f;

        double tickBudget = 1000000.0 / time.TickRate;

        uint tickRate = time.TickRate;
        long tickCount = time.TickCount;
        double tickTime = time.TickTime;
        long tickAllocated = time.TickAllocated;

        double frameTime = graphics.FrameInfo.Time;
        long frameAllocated = graphics.FrameInfo.Allocated;

        long usedMemory = GC.GetTotalMemory(false);

        var currentIndex = (int)((tickCount - 1) & 255);

        tickTimeAccumulated += time.TickTime;
        tickDeltaTimeAccumulated += time.TickDeltaTime;
        frameTimeAccumulated += frameTime;
        frameDeltaTimeAccumulated += graphics.FrameInfo.DeltaTime;

        int interval = (int)tickRate / 2;

        if(tickCount % interval == 0)
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

        tickTimes[currentIndex] = tickTime;
        frameTimes[currentIndex] = frameTime;
        managedMemoryAmounts[currentIndex] = usedMemory / 1024.0 / 1024.0;
        
        sb.Clear();
        sb.Append("Build-Version:").Append(Application.BuildVers).Append(" | Build-Date:").Append(Application.BuildVers).Append('\n');
        sb.Append(graphics.RenderSettings.Resolution.X).Append('x').Append(graphics.RenderSettings.Resolution.Y).Append(" @ ").Append(graphics.RenderSettings.Resolution.RefreshRate).Append('\n');
        sb.AppendDouble(frameDeltaTimeAvg > 0 ? double.Round(1.0 / (frameDeltaTimeAvg / 1000)) : 0, 0, 3);
        sb.Append(":FPS | ");
        sb.AppendDouble(tickDeltaTimeAvg > 0 ? double.Round(1.0 / (tickDeltaTimeAvg / 1000)) : 0, 0, 3);
        sb.Append(":TPS\n");

        sb.Append("DrawCalls : ");
        sb.Append(graphics.FrameInfo.DrawCalls);
        sb.Append('\n');

        sb.Append("FrameTime : ");
        sb.AppendDouble(frameTimeAvg / 1000000.0, 4, 2);
        sb.Append('\n');

        sb.Append("FrameDeltaTime : ");
        sb.AppendDouble(frameDeltaTimeAvg, 4, 2);
        sb.Append('\n');

        sb.Append("TickTime  : ");
        sb.AppendDouble(tickTimeAvg / 1000.0, 4, 2);
        sb.Append('\n');

        sb.Append("TickDeltaTime  : ");
        sb.AppendDouble(tickDeltaTimeAvg, 4, 2);
        sb.Append('\n');

        sb.Append("Managed Memory : ");
        sb.AppendDouble(usedMemory / 1024.0 / 1024.0, 4, 2);
        sb.Append(" MB\n");

        sb.Append("RenderThread allocated: ");
        sb.AppendLong(frameAllocated, 3);
        sb.Append(" bytes\n");

        sb.Append("CoreThread allocated: ");
        sb.AppendLong(tickAllocated, 3);
        sb.Append(" bytes\n");

        string builtString = sb.ToString();

        graphics.DrawLabel(defaultFont, builtString, 0, 0);
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