
using Microsoft.Xna.Framework;

namespace Luminix;

public struct Transform
{
    public Transform(Vector3 position)
    {
        Position = position;
    }
    public Transform(Vector3 position, float rotation)
    {
        Position = position;
        Rotation = rotation;
    }
    public Transform(Vector3 position, Vector2 scale)
    {
        Position = position;
        Scale = scale;
    }
    public Transform(Vector3 position, Vector2 scale, float rotation)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
    }

    public Vector3 Position;
    public Vector2 Scale;
    public float Rotation;
}
