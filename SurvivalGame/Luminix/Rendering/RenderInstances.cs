using Luminix.Resources;
using Microsoft.Xna.Framework;

namespace Luminix.Rendering.RenderInstances;

internal readonly record struct InstanceSprite(ShaderResource Shader, TextureResource Texture, Color Color, Vector3 Position, Vector2 Scale, float Rotation, int Group = -1);
internal readonly record struct InstanceText(ShaderResource Shader, FontResource Font, Color Color, string Text, Vector3 Position, Vector2 Scale, float Rotation,  int Group = -1);

internal readonly record struct InterpolatedInstanceSprite(ShaderResource Shader, TextureResource Texture, Color Color, Vector3 Position0, Vector3 Position, Vector2 Scale0, Vector2 Scale, float Rotation0, float Rotation);
internal readonly record struct InterpolatedInstanceText(ShaderResource Shader, FontResource Font, Color Color, string Text, Vector3 Position0, Vector3 Position, Vector2 Scale0, Vector2 Scale, float Rotation0, float Rotation);

internal readonly record struct InstanceImage(ShaderResource Shader, TextureResource Texture, Color Color, int X, int Y, int Width, int Height, float Rotation);
internal readonly record struct InstanceLabel(ShaderResource Shader, FontResource Font, Color Color, string Text,  int X, int Y, int Width, int Height, float Rotation);
