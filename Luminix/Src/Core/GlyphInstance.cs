using Microsoft.Xna.Framework;

namespace Luminix;

public readonly struct GlyphInstance
{
    public GlyphInstance(char character, int index, int line, int textLength, TextureResource texture, Vector2 offset, Vector2 size)
    {
        Character = character;
        Index = index;
        Line = line;
        TextLength = textLength;
        Texture = texture;
        Offset = offset;
        Size = size;
    }

    public char Character { get; }
    public int Index { get; }
    public int Line { get; }
    public int TextLength { get; }
    public TextureResource Texture { get; }
    public Vector2 Offset { get; }
    public Vector2 Size { get; }
    public Vector2 Center => Offset + Size * 0.5f;
}
