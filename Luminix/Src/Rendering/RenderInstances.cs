using Microsoft.Xna.Framework;

namespace Luminix.Rendering;

internal readonly record struct RenderInstance(ShaderID Shader, TextureID Texture, Rectangle SourceRectangle, Color Color, TransformInterp Transform);
internal readonly record struct RenderInstanceUI(ShaderID Shader, TextureID Texture, Rectangle SourceRectangle, Color Color, Point Position, Point Scale, float Rotation);
