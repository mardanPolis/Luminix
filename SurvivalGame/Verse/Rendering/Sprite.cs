using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Resources;

namespace Verse.Rendering;
public class Sprite(TextureRegion textureRegion, SpriteEffects effects = SpriteEffects.None, Vector2 origin = default)
{
    public readonly TextureRegion TextureRegion = textureRegion;
    public SpriteEffects Effects { get; set; } = effects;
    public Vector2 Origin { get; set; } = origin;
    public Color Color { get; set; } = Color.White;

    public void CenterOrigin()
    {
        Origin = new Vector2(TextureRegion.Width / 2f, TextureRegion.Height / 2f);
    }

    public float Width => TextureRegion.Width;
    public float Height => TextureRegion.Height;
}