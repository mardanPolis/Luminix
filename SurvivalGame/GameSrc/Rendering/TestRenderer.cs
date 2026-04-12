using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Verse.Rendering;
using Verse.Resources;

namespace GameSrc.Rendering;

public class TestRenderer : Renderer<Test>
{
    Sprite sprite;
    Vector2[] positions;

    public override void Initialize()
    {
        var textureRegion = ResourceManager.GetTextureRegion("null_texture");
        sprite = new Sprite(textureRegion, origin: new Vector2(textureRegion.Width / 2f, textureRegion.Height / 2f));

        var positions = new List<Vector2>();

        for (int y = 0; y < 250; y++)
        {
            for (int x = 0; x < 250; x++)
            {
                Vector2 position = new Vector2(x - 125, y - 125);
                positions.Add(position);
            }
        }

        this.positions = positions.ToArray();
    }
    public override void Render(Test renderable, RenderStack renderStack)
    {
        renderStack.PostSpriteBatch(
            sprite, 
            positions, 
            Vector2.One, 
            0f
        );
    }
}
