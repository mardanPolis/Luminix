using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Resources;

namespace Verse.Rendering;
public sealed class Sprite
{
    public Sprite(TextureRegion textureRegion, SpriteEffects effects = SpriteEffects.None, Vector2 origin = default)
    {
        if(textureRegion == null) throw new ArgumentNullException(nameof(textureRegion));

        TextureRegion = textureRegion;
        Effects = effects;
        Origin = origin;
    }

    public readonly TextureRegion TextureRegion;
    public readonly SpriteEffects Effects;
    public readonly Vector2 Origin;
    public readonly Color Color = Color.White;

    public float Width => TextureRegion.Width;
    public float Height => TextureRegion.Height;
}