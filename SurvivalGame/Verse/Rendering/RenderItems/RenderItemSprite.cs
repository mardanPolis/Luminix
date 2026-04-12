using Microsoft.Xna.Framework;

namespace Verse.Rendering.RenderItems;
internal struct RenderItemSprite
{
    public RenderItemSprite(Sprite sprite, Vector2 position, Vector2 scale, float rotation, float layerDepth)
    {
        Sprite = sprite;
        Position = position;
        Scale = scale;
        Rotation = rotation;
        LayerDepth = layerDepth;
    }

    public Sprite Sprite;
    public Vector2 Position;
    public Vector2 Scale;
    public float Rotation;
    public float LayerDepth;
}