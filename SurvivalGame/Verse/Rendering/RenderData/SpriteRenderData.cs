
using Microsoft.Xna.Framework;

namespace Verse.Rendering.RenderData;
internal struct SpriteRenderData
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;

    public Sprite Sprite;
    public Color Color;

    public SpriteRenderData(Vector2 position, float rotation, Vector2 scale, Sprite sprite, Color color)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Sprite = sprite;
        Color = color;
    }
}