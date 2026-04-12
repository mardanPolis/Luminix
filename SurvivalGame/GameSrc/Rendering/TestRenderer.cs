using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Verse.Rendering;
using Verse.Resources;
using Verse.Simulation;

namespace GameSrc.Rendering;

public class TestRenderer : Renderer<Test>
{

    Sprite sprite;

    List<Vector2> positions;

    public override void Initialize()
    {
        var textureRegion = ResourceManager.GetTextureRegion("null_texture");
        sprite = new Sprite(textureRegion, origin: new Vector2(textureRegion.Width / 2f, textureRegion.Height / 2f));

        positions = new List<Vector2>();

        for (int y = 0; y < 250; y++)
        {
            for (int x = 0; x < 250; x++)
            {
                Vector2 position = new Vector2(x - 125, y - 125);
                positions.Add(position);
            }
        }
    }
    public override void Render(Test renderable, RenderStack renderStack)
    {
        for(int i = 0; i < positions.Count; i++)
        {
            renderStack.PostSprite(
                sprite, 
                positions[i], 
                rotation: 0f, 
                scale: Vector2.One, 
                layerDepth: 0f
            );
        }
    }
}
