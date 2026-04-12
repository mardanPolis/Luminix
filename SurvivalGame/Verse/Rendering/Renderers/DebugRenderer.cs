using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Verse.Resources;
using Verse.Threading;

namespace Verse.Rendering;

public abstract class DebugRenderer
{
    protected SpriteBatch spriteBatch { get; private set; }
    internal void Initialize(SpriteBatch spriteBatch)
    {
        this.spriteBatch = spriteBatch;
        pixel = ResourceManager.LoadTexture("pixel");
    }
    
    protected Texture2D pixel;

    public abstract void Render();
    
    protected void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);

        thickness = Math.Max(thickness, 1);

        spriteBatch.Draw(pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
            null, 
            color, 
            angle, 
            new Vector2(0, thickness / 2),
            SpriteEffects.None, 
            0);
    }

    protected void DrawPanel(Vector2 start, Vector2 scale, Color color)
    {
        Rectangle rect = new Rectangle(
            (int)start.X, 
            (int)start.Y, 
            (int)scale.X, 
            (int)scale.Y
        );
        spriteBatch.Draw(pixel, rect, color);
    }
    protected void DrawGraph(Vector2 position, float height, double[] data, double maxValue, int valueLineThickness, int valueMargin, int lastIndex, bool invertedWidth)
    {
        float graphAreaWidth = data.Length * valueLineThickness;
        Vector2 scale = new Vector2(graphAreaWidth + (valueMargin * 2), height);
        
        // Draw the background
        if(invertedWidth)
            position = new Vector2(position.X - scale.X, position.Y);

        DrawPanel(position, scale, Color.Black * 0.5f);

        Vector2 leftUpper  = position + new Vector2(valueMargin, valueMargin);
        Vector2 leftLower  = position + new Vector2(valueMargin, scale.Y - valueMargin);
        Vector2 rightUpper = position + new Vector2(scale.X - valueMargin, valueMargin);
        Vector2 rightLower = position + new Vector2(scale.X - valueMargin, scale.Y - valueMargin);

        // 3. Graphing Constants
        float availableHeight = scale.Y - (valueMargin * 2) - 5;
        float graphBottomY = leftLower.Y;

        for (int i = 0; i < data.Length; i++)
        {
            float xPos = leftLower.X + (i * valueLineThickness);

            float ratio = (float)(data[i] / maxValue);

            float barHeight = ratio * availableHeight;

            Vector2 startPoint = new Vector2(xPos, graphBottomY);

            int distance = (i <= lastIndex) 
                ? (lastIndex - i) 
                : (lastIndex + (data.Length - i));

            float fade = 1.0f - ((float)distance / data.Length);
            fade = MathF.Pow(fade, 1.5f); 

            float r = ratio;
            float g = 1.0f - ratio * 0.5f;
            float b = 0.2f;

            Color barColor = new Color(r, g, b) * fade;

            DrawVerticalLine(startPoint, -(int)(barHeight + 5), barColor, valueLineThickness);
        }

        DrawLine(leftUpper, rightUpper, Color.White, 1);
        DrawLine(rightUpper, rightLower, Color.White, 1);
        DrawLine(rightLower, leftLower, Color.White, 1);
        DrawLine(leftLower, leftUpper, Color.White, 1);
    }

    protected void DrawVerticalLine(Vector2 position, int height, Color color, int lineThickness)
    {
        lineThickness = Math.Max(lineThickness, 1);

        spriteBatch.Draw(pixel,
            new Rectangle((int)position.X, (int)position.Y + height, lineThickness, -height),
            null, 
            color, 
            0, 
            new Vector2(0, 0),
            SpriteEffects.None, 
            0);
    }
    protected void AppendLong(StringBuilder sb, long value, int minDigits = 1)
    {
        if (value < 0)
        {
            sb.Append('-');
            value = -value;
        }

        long temp = value;
        int digitsFound = 0;
        do { digitsFound++; temp /= 10; } while (temp > 0);

        for (int i = digitsFound; i < minDigits; i++)
        {
            sb.Append('0');
        }

        Span<char> buffer = stackalloc char[20]; 
        int pos = 0;
        
        temp = value;
        do
        {
            buffer[pos++] = (char)('0' + (temp % 10));
            temp /= 10;
        } while (temp > 0);

        for (int i = pos - 1; i >= 0; i--)
        {
            sb.Append(buffer[i]);
        }
    }
    protected void AppendDouble(StringBuilder sb, double value, int decimalPlaces = 4, int minIntDigits = 1)
    {
        if (double.IsNaN(value)) { sb.Append("NaN"); return; }
        if (double.IsInfinity(value)) { sb.Append("INF"); return; }
        if (value < 0) { sb.Append('-'); value = -value; }

        double integral = Math.Floor(value);
        AppendLong(sb, (long)integral, minIntDigits);

        if (decimalPlaces > 0)
        {
            sb.Append('.');
            double remainder = value - integral;
            for (int i = 0; i < decimalPlaces; i++)
            {
                remainder *= 10;
                int digit = (int)remainder;
                sb.Append((char)('0' + digit));
                remainder -= digit;
            }
        }
    }
}

internal sealed class GameDebugRenderer : DebugRenderer
{
    private Main main;
    private CoreThread coreThread;

    double[] managedMemoryAmounts = new double[128];
    private StringBuilder sb = new StringBuilder();

    private double frameTime = 0;
    private double elapsed = 0;
    private double fps = 0;

    public void Initialize(Main main, CoreThread coreThread)
    {
        this.main = main;
        this.coreThread = coreThread;
    }
    public override void Render()
    {
        Viewport viewport = RenderingSystem.GraphicsDevice.Viewport;

        float gHeight = 200f;

        var tickTimes = coreThread.TickTimes;
        var currentIndex = (int)((coreThread.TickCount - 1) & 127);

        double frameDeltaTime = main.FrameDeltaTime; 
        double tickTime = coreThread.TickTime;
        double tickDeltaTime = coreThread.TickDeltaTime;

        double budgetUs = 1000000.0 / coreThread.TickRate;

        long usedMemory = GC.GetTotalMemory(false);
        managedMemoryAmounts[currentIndex] = usedMemory / 1024.0 / 1024.0;
        
        elapsed += frameDeltaTime; // Assuming this is in seconds

        if (elapsed >= 0.05) 
        {
            fps = main.FPS;
            frameTime = main.FrameTime; 
            elapsed = 0;
        }

        sb.Clear();
        sb.Append("GAME VERSION:").Append(Main.VERSION).Append("\n");
        AppendDouble(sb, fps, 3, 3);
        sb.Append(":FPS | ");
        AppendDouble(sb, tickDeltaTime > 0 ? 1.0 / tickDeltaTime : 0, 3, 3);
        sb.Append(":TPS\n");

        sb.Append("FrameTime : ");
        AppendDouble(sb, frameTime / 1000000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("TickTime  : ");
        AppendDouble(sb, tickTime / 1000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("FrameDeltaTime : ");
        AppendDouble(sb, frameDeltaTime * 1000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("TickDeltaTime  : ");
        AppendDouble(sb, tickDeltaTime * 1000.0, 4, 2);
        sb.Append("ms\n");

        sb.Append("Managed Memory : ");
        AppendDouble(sb, usedMemory / 1024.0 / 1024.0, 4, 2);
        sb.Append(" MB\n");

        sb.Append("RenderThread allocated: ");
        AppendLong(sb, main.FrameAllocationAmount, 3);
        sb.Append(" bytes\n");

        sb.Append("CoreThread allocated: ");
        AppendLong(sb, coreThread.TickAllocatedAmount, 3);
        sb.Append(" bytes\n");

        sb.Append("DrawCalls : ");
        sb.Append(RenderingSystem.DrawCount);

        spriteBatch.Begin();

        spriteBatch.DrawString(ResourceManager.GenericFontMedium, sb,
            new Vector2(15, 15), Color.White);

        Vector2 graphPos = new Vector2(viewport.Width - 20, viewport.Height - gHeight - 20);
        DrawGraph(graphPos, gHeight, managedMemoryAmounts, 128, 5, 5, currentIndex, true);

        graphPos = new Vector2(20, viewport.Height - gHeight - 20);
        DrawGraph(graphPos, gHeight, tickTimes, budgetUs, 5, 5, currentIndex, false);

        spriteBatch.End();

        var keyboard = Keyboard.GetState();
        if(keyboard.IsKeyDown(Keys.Home))
        {
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                Console.WriteLine($"Gen {i} collections: {GC.CollectionCount(i)}");
            }
        }
    }
}
