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

    Vector2 oldPos1 = Vector2.Zero;
    Vector2 oldPos2 = Vector2.Zero;

    public override void Render(Test renderable, RenderStack renderStack)
    {
        Vector2 position1 = new Vector2(-1, (TriangleWave(World.TickCount, 60) - 0.5f) * 2);
        Vector2 position2 = new Vector2(1, (TriangleWave(World.TickCount, 60) - 0.5f) * -2);

        renderStack.PostSprite(sprite,  oldPos1, position1, 0,  0, Vector2.One, Vector2.One, 1);
        renderStack.PostSprite(sprite,  Vector2.Zero,  0, Vector2.One, 1);
        renderStack.PostSprite(sprite,  oldPos2, position2, 0,  0, Vector2.One, Vector2.One, 1);

        oldPos1 = position1;
        oldPos2 = position2;
    }

    public override void RenderGizmo(Test renderable, RenderStack renderStack)
    { 
        
    }
}
