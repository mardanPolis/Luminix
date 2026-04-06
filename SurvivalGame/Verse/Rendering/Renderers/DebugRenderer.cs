using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Resources;
using Verse.Threads;

namespace Verse.Rendering.Renderers;

internal sealed class DebugRenderer
{
    public DebugRenderer(SpriteBatch spriteBatch)
    {
        this.spriteBatch = spriteBatch;
    }

    private Main main;
    private CoreThread coreThread;
    private SpriteBatch spriteBatch;

    private Texture2D texture;

    private double[] tickTimes;

    public void Initialize(Main main, CoreThread coreThread)
    {
        this.main = main;
        this.coreThread = coreThread;

        tickTimes = new double[255];
        texture = ResourceManager.LoadTexture("pixel");
    }
    public void Render()
    {
        Viewport viewport = RenderingSystem.GraphicsDevice.Viewport;

        long tickCount = coreThread.TickCount;
        double tickTime = coreThread.TickTime;
        double tickDeltaTime = coreThread.TickDeltaTime;
        uint currentTickRate = coreThread.TickRate;

        int currentIndex = (int)((tickCount - 1) % tickTimes.Length);
        tickTimes[currentIndex] = tickTime;

        string fps = main.FrameDeltaTime > 0 
            ? $"{1000f / main.FrameDeltaTime:F2}" : "INF";
        string tps = tickDeltaTime > 0 
            ? $"{1000000 / tickDeltaTime:F2}" : "INF";

        spriteBatch.Begin();

        spriteBatch.DrawString(ResourceManager.GenericFontRegular, 
            $"{Main.VERSION}\n" +
            $"{fps}FPS; Frame DeltaTime: {main.FrameDeltaTime:F4}ms; Frame Time:{main.FrameTime / 1000000.0:F4}ms\n" +
            $"{tps}TPS; Tick DeltaTime: {tickDeltaTime / 1000:F4}ms Tick Time:{tickTime / 1000f:F4}ms;",
            new Vector2(15, 15), Color.White);

        //THIS PART WAS MADE WITH AI, so idk whats going on... apparently draws a tick time graph.
        float margin = 20f;
        float barWidth = 5f;
        float graphWidth = tickTimes.Length * barWidth;
        float graphHeight = 100f;
        Vector2 graphBottomLeft = new Vector2(margin, viewport.Height - margin);

        Rectangle bgRect = new Rectangle(
            (int)graphBottomLeft.X - 5, 
            (int)(graphBottomLeft.Y - graphHeight - 5), 
            (int)graphWidth + 10, 
            (int)graphHeight + 10
        );
        spriteBatch.Draw(texture, bgRect, Color.Black * 0.5f);

        double budgetUs = 1000000.0 / currentTickRate;

        for (int i = 0; i < tickTimes.Length; i++)
        {
            double timeUs = tickTimes[i];

            int age = (currentIndex - i + tickTimes.Length) % tickTimes.Length;
            
            float fade = 1.0f - ((float)age / tickTimes.Length);
            fade = MathHelper.Clamp(fade, 0.1f, 1.0f); 

            float stress = Math.Clamp((float)(timeUs / budgetUs), 0f, 1f);
            
            float r = stress * fade;
            float g = (currentIndex == i ? 1f : 0f) * fade;
            float b = (1f - stress) * 0.2f * fade;

            Color barColor = new Color(r, g, b, fade);

            float barHeight = Math.Max(1f, stress * graphHeight);
            float xPos = graphBottomLeft.X + (i * barWidth);

            Rectangle barRect = new Rectangle(
                (int)xPos, 
                (int)(graphBottomLeft.Y - barHeight), 
                (int)barWidth, 
                (int)barHeight
            );
            spriteBatch.Draw(texture, barRect, barColor);
        }

        DrawLine(new Vector2(graphBottomLeft.X, graphBottomLeft.Y - graphHeight),
                new Vector2(graphBottomLeft.X + graphWidth, graphBottomLeft.Y - graphHeight),
                Color.White * 0.3f, 1);

        spriteBatch.End();
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(texture,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
            null, 
            color, 
            angle, 
            new Vector2(thickness / 2, 0),
            SpriteEffects.None, 
            0);
    }
}
