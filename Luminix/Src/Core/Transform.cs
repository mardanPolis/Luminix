
using Microsoft.Xna.Framework;

namespace Luminix;

public struct Transform
{
    public Transform(Vector3 position)
    {
        Position = position;
        Scale = Vector2.One;
        Rotation = 0;
    }
    public Transform(Vector3 position, float rotation)
    {
        Position = position;
        Scale = Vector2.One;
        Rotation = rotation;
    }
    public Transform(Vector3 position, Vector2 scale)
    {
        Position = position;
        Scale = scale;
        Rotation = 0;
    }
    public Transform(Vector3 position, Vector2 scale, float rotation)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
    }

    public Vector3 Position { get; set; }
    public Vector2 Scale { get; set; }
    public float Rotation { get; set; }
}
