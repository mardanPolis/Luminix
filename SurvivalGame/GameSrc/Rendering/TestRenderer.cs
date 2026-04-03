using System;
using Microsoft.Xna.Framework;
using Verse.Rendering;
using Verse.Resources;
using Verse.Simulation;

namespace GameSrc.Rendering;

public class TestRenderer : Renderer<Test>
{

    static float TriangleWave(ulong t, uint period)
    {
        float cycle = (float)(t % period) / period; // normalize to [0,1)
        return 1f - Math.Abs(2f * cycle - 1f); // rises 0→1 then falls 1→0
    }

    Sprite sprite;

    public override void Initialize()
    {
        var textureRegion = ResourceManager.GetTextureRegion("null_texture");
        sprite = new Sprite(textureRegion, origin: new Vector2(textureRegion.Width / 2f, textureRegion.Height / 2f));
    }

    Vector2 position0 = Vector2.Zero;
    float rotation0 = 0f;

    public override void Render(Test renderable, RenderStack renderStack)
    {
        Vector2 position = Vector2.Zero;
        position.Y = (TriangleWave(World.TickCount, 60) - 0.5f) * 50;
        var rotation = TriangleWave(World.TickCount, 60);

        renderStack.PostSprite(sprite,  position,  rotation, Vector2.One, 1);

        position0 = position;
        rotation0 = rotation;
    }

    public override void RenderGizmo(Test renderable, RenderStack renderStack)
    { 
        
    }
}
