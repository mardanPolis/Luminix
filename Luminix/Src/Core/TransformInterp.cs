using Microsoft.Xna.Framework;

namespace Luminix;

public struct TransformInterp
{
    public TransformInterp(Vector3 position0, Vector3 position)
    {
        Position0 = position0;
        
        Position = position;
    }
    public TransformInterp(Vector3 position0, float rotation0, Vector3 position, float rotation)
    {
        Position0 = position0;
        Rotation0 = rotation0;

        Position = position;
        Rotation = rotation;
    }
    public TransformInterp(Vector3 position0, Vector2 scale0, Vector3 position, Vector2 scale)
    {
        Position0 = position0;
        Scale0 = scale0;
        
        Position = position;
        Scale = scale;
    }
    public TransformInterp(Vector3 position0, Vector2 scale0, float rotation0, Vector3 position, Vector2 scale, float rotation)
    {
        Position0 = position0;
        Rotation0 = rotation0;
        Scale0 = scale0;

        Position = position;
        Scale = scale;
        Rotation = rotation;
    }

    internal TransformInterp(in Transform transform)
    {
        Position0 = transform.Position;
        Rotation0 = transform.Rotation;
        Scale0 = transform.Scale;

        Position = transform.Position;
        Rotation = transform.Rotation;
        Scale = transform.Scale;
    }

    public Vector3 Position0;
    public Vector2 Scale0;
    public float Rotation0;

    public Vector3 Position;
    public Vector2 Scale;
    public float Rotation;

    public readonly Transform StartTransform => new(Position0, Scale0, Rotation0);
    public readonly Transform EndTransform => new(Position, Scale, Rotation);
}
