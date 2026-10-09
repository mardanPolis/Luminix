using Microsoft.Xna.Framework;

namespace Luminix.Rendering;

internal readonly record struct RenderInstance(ShaderResource Shader, Resource Resource, Color Color, TransformInterp Transform, string Text);
internal readonly record struct RenderInstanceUI(ShaderResource Shader, Resource Resource, Color Color, int X, int Y, int Width, int Height, float Rotation, string Text);
