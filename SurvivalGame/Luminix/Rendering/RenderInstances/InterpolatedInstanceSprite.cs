using Luminix.Resources;
using Microsoft.Xna.Framework;

namespace Luminix.Rendering.RenderInstances;

internal readonly struct InterpolatedInstanceSprite(TextureResource texture,Vector3 position0, Vector3 position, Vector2 size0, Vector2 size, float rotation0, float rotation)
{
    public readonly TextureResource Texture = texture;
    public readonly Vector3 Position0 = position0;
    public readonly Vector3 Position = position;
    public readonly Vector2 Size0 = size0;
    public readonly Vector2 Size = size;
    public readonly float Rotation0 = rotation0;
    public readonly float Rotation = rotation;
}