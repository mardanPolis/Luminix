using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Verse.Rendering;
using Verse.Resources;

namespace GameSrc.Rendering;

public class TestRenderer : Renderer<Test>
{
    Sprite sprite;
    Vector3[] positions;

    public override void Initialize()
    {
        var textureRegion = ResourceManager.GetTextureRegion("null_texture");
        sprite = new Sprite(textureRegion, origin: new Vector2(textureRegion.Width / 2f, textureRegion.Height / 2f));

        var positions = new List<Vector3>();

        for (int y = 0; y < 500; y++)
        {
            for (int x = 0; x < 500; x++)
            {
                Vector3 position = new Vector3(x * 2 - 500, y * 2 - 500, 0);
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
