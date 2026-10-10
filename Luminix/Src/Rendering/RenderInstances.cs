using Microsoft.Xna.Framework;

namespace Luminix.Rendering;

internal readonly record struct RenderInstance(ShaderResource Shader, TextureResource Texture, Color Color, TransformInterp Transform);
internal readonly record struct RenderInstanceUI(ShaderResource Shader, TextureResource Texture, Color Color, Point Position, Point Scale, float Rotation);
