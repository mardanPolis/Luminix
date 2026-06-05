using Luminix.Resources;
using Microsoft.Xna.Framework;

namespace Luminix.Rendering.RenderInstances;

internal readonly struct InstanceText(FontResource font, string text, Vector3 position, Vector2 size, float rotation,  int group = -1)
{
    public readonly FontResource Font = font;
    public readonly string Text = text;
    public readonly Vector3 Position = position;
    public readonly Vector2 Size = size;
    public readonly float Rotation = rotation;
    public readonly int Group = group;
}