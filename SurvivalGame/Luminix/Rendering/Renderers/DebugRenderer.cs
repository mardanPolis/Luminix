using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Resources;
using Luminix.Threading.Buffers;
using Luminix.Extensions;

namespace Luminix.Rendering;

public abstract class DebugRenderer
{
    public DebugRenderer()
    {
        universe = Workspace.Current.GetService<Universe>();
        resourceProvider = Workspace.Current.GetService<ResourceProvider>();
        pixel = resourceProvider.GetResource<TextureResource>("Pixel");
    }
    protected readonly Universe universe;
    protected readonly ResourceProvider resourceProvider;
    protected readonly TextureResource pixel;
    
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
    public abstract void Render(RenderBuffer renderBuffer);
}

internal sealed class SystemDebugRenderer : DebugRenderer
{
    public SystemDebugRenderer()
    {
        graphics = GraphicsAdapter.DefaultAdapter.Description.Split('/')[0];

        tickTimes = new double[128];
        frameTimes = new double[128];
        managedMemoryAmounts = new double[128];
    }

    private string graphics;

    private readonly double[] tickTimes;
    private readonly double[] frameTimes;
    private readonly double[] managedMemoryAmounts;
    private StringBuilder sb = new StringBuilder();

    private double elapsed = 0;

    public override void Render(RenderBuffer renderBuffer)
    {
        if(!Settings.DebugInfo)
            return;

        Viewport viewport = universe.Viewport;

        float gHeight = 200f;

        var currentIndex = (int)((universe.TickCount - 1) & 127);

        uint tickRate = universe.TickRate;
        long tickCount = universe.TickCount;
        double tickTime = universe.TickTime;
        double tickDeltaTime = universe.TickDeltaTime;
        long tickAllocated = universe.TickAllocated;

        uint frameRate = universe.FrameRate;
        long frameCount = universe.FrameCount;
        double frameTime = universe.FrameTime;
        double frameDeltaTime = universe.FrameDeltaTime; 
        long frameAllocated = universe.FrameAllocated;

        double budgetUs = 1000000.0 / universe.TickRate;

        long usedMemory = GC.GetTotalMemory(false);
        managedMemoryAmounts[currentIndex] = usedMemory / 1024.0 / 1024.0;
        
        elapsed += frameDeltaTime; // Assuming this is in seconds

        sb.Clear();
        sb.Append("Build-Version:").Append(Main.Version).Append(" | Build-Date:").Append(Main.BuildDate).Append('\n');
        // sb.Append(graphics).Append($" | ").Append(renderingSystem.ResolutionWidth).Append(':').Append(renderingSystem.ResolutionHeight).Append('\n');
        sb.AppendDouble(frameDeltaTime > 0 ? MathF.Round(1.0f / (float)frameDeltaTime) : 0, 0, 3);
        sb.Append(":FPS | ");
        sb.AppendDouble(tickDeltaTime > 0 ? MathF.Round(1.0f / (float)tickDeltaTime) : 0, 0, 3);
        sb.Append(":TPS\n");

        sb.Append("FrameTime : ");
        sb.AppendDouble(frameTime / 1000000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("TickTime  : ");
        sb.AppendDouble(tickTime / 1000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("FrameDeltaTime : ");
        sb.AppendDouble(frameDeltaTime * 1000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("TickDeltaTime  : ");
        sb.AppendDouble(tickDeltaTime * 1000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("Managed Memory : ");
        sb.AppendDouble(usedMemory / 1024.0 / 1024.0, 4, 2);
        sb.Append(" MB\n");

        sb.Append("RenderThread allocated: ");
        sb.AppendLong(frameAllocated, 3);
        sb.Append(" bytes\n");

        sb.Append("CoreThread allocated: ");
        sb.AppendLong(tickAllocated, 3);
        sb.Append(" bytes\n");

        // sb.Append("DrawCalls : ");
        // sb.Append(renderingSystem.DrawCount);

        // spriteBatch.Begin();
        // spriteBatch.DrawString(defaultFontRegular.Source, sb,
        //     new Vector2(15, 15), Color.White);

        // Vector2 graphPos = new Vector2(viewport.Width - 20, viewport.Height - gHeight - 20);
        // DrawGraph(graphPos, gHeight, managedMemoryAmounts, 128, 5, 5, currentIndex, true);

        // graphPos = new Vector2(20, viewport.Height - gHeight - 20);
        // DrawGraph(graphPos, gHeight, tickTimes, budgetUs, 5, 5, currentIndex, false);
        // spriteBatch.End();
    }
}
