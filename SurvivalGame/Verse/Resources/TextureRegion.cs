using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Resources;
public class TextureRegion
{
    private float widthCoefficient;
    private float heightCoefficient;

    public TextureRegion(Texture2D texture, int x, int y, int width, int height)
    {
        if(texture == null) throw new ArgumentNullException(nameof(texture));

        Texture = texture;
        SourceRectangle = new Rectangle(x, y, width, height);
        widthCoefficient = 1f / width;
        heightCoefficient = 1f / height;
    }

    public readonly Texture2D Texture;
    public readonly Rectangle SourceRectangle;

    public float Width => SourceRectangle.Width;
    public float Height => SourceRectangle.Height;

    public float WidthCoefficient => widthCoefficient;
    public float HeightCoefficient => heightCoefficient;
}