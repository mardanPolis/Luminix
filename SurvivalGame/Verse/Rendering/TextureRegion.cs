using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Rendering;
public class TextureRegion
{
    public TextureRegion(Texture2D texture, int x, int y, int width, int height)
    {
        if(texture == null) throw new ArgumentNullException(nameof(texture));

        Texture = texture;
        SourceRectangle = new Rectangle(x, y, width, height);
        WidthCoefficient = 1f / width;
        HeightCoefficient = 1f / height;
    }


    public readonly Texture2D Texture;
    public readonly Rectangle SourceRectangle;

    public float Width => SourceRectangle.Width;
    public float Height => SourceRectangle.Height;
    
    public readonly float WidthCoefficient;
    public readonly float HeightCoefficient;
}