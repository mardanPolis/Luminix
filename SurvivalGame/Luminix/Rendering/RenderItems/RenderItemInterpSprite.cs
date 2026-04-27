using Microsoft.Xna.Framework;

namespace Luminix.Rendering.RenderItems;
internal struct RenderItemInterpSprite
{
    public RenderItemInterpSprite(Sprite sprite, Vector2 position, Vector2 scale, float rotation, float layerDepth)
    {
        Sprite = sprite;
        Position = position;
        Scale = scale;
        Rotation = rotation;
        LayerDepth = layerDepth;
    }

    public Sprite Sprite;
    public Vector2 Position0;
    public Vector2 Position;
    public Vector2 Scale0;
    public Vector2 Scale;
    public float Rotation0;
    public float Rotation;
    public float LayerDepth;
}