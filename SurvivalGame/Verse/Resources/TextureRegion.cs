using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Resources;
public class TextureRegion(Texture2D texture, int x, int y, int width, int height)
{
    public readonly Texture2D Texture = texture;
    public readonly Rectangle SourceRectangle = new Rectangle(x, y, width, height);

    public float Width => SourceRectangle.Width;
    public float Height => SourceRectangle.Height;
}