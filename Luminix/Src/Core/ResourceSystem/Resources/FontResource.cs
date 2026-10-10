using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class FontResource : Resource
{
    public readonly struct Glyph
    {
        public Glyph(char character, TextureResource texture, Rectangle bounds, Rectangle cropping, float leftBearing, float width, float rightBearing)
        {
            Character = character;
            Texture = texture;
            Bounds = bounds;
            Cropping = cropping;
            LeftBearing = leftBearing;
            Width = width;
            RightBearing = rightBearing;
        }

        public char Character { get; }
        public TextureResource Texture { get; }   // region of the atlas, null when there is nothing to draw (space, ...)
        public Rectangle Bounds { get; }          // source rect in the atlas
        public Rectangle Cropping { get; }        // X,Y = offset of the trimmed glyph inside its original cell
        public float LeftBearing { get; }
        public float Width { get; }
        public float RightBearing { get; }

        public bool IsVisible => Texture != null;
    }

    private static uint globalIndex = 0;

    private readonly Dictionary<char, Glyph> glyphs;

    internal FontResource(string name, XnbFontResult data, TextureResource atlas, TextureResource[] regions) : base(name)
    {
        Index = globalIndex++;

        LineSpacing = data.LineSpacing;
        Spacing = data.Spacing;
        DefaultCharacter = data.DefaultCharacter;
        Atlas = atlas;

        glyphs = new Dictionary<char, Glyph>(data.Glyphs.Length);
        for (int i = 0; i < data.Glyphs.Length; i++)
        {
            var g = data.Glyphs[i];
            glyphs[g.Character] = new Glyph(g.Character, regions[i], g.Bounds, g.Cropping, g.LeftBearing, g.Width, g.RightBearing);
        }
    }

    internal uint Index { get; }

    public TextureResource Atlas { get; }

    public int LineSpacing { get; }
    public float Spacing { get; }
    public char? DefaultCharacter { get; }
    public int GlyphCount => glyphs.Count;

    public bool TryGetGlyph(char c, out Glyph glyph)
    {
        if (glyphs.TryGetValue(c, out glyph)) return true;
        if (DefaultCharacter is char d && glyphs.TryGetValue(d, out glyph)) return true;
        glyph = default;
        return false;
    }

    public TextEnumerator Enumerate(ReadOnlySpan<char> text) => new TextEnumerator(this, new Text(text));
    public TextEnumerator Enumerate(Text text) => new TextEnumerator(this, text);

    public Vector2 Measure(ReadOnlySpan<char> text)
    {
        float ox = 0, width = 0;
        int lines = 1;
        bool first = true;

        foreach (char c in text)
        {
            if (c == '\r') continue;
            if (c == '\n')
            {
                ox = 0; lines++; first = true;
                continue;
            }

            if (!TryGetGlyph(c, out var g)) continue;

            if (first) { ox = MathF.Max(g.LeftBearing, 0); first = false; }
            else ox += Spacing + g.LeftBearing;

            ox += g.Width;
            width = MathF.Max(width, ox + MathF.Max(g.RightBearing, 0));
            ox += g.RightBearing;
        }

        return new Vector2(width, lines * LineSpacing);
    }

    public Vector2 Measure(Text text)
    {
        float ox = 0, width = 0;
        int lines = 1;
        bool first = true;

        foreach (char c in text)
        {
            if (c == '\r') continue;
            if (c == '\n')
            {
                ox = 0; lines++; first = true;
                continue;
            }

            if (!TryGetGlyph(c, out var g)) continue;

            if (first) { ox = MathF.Max(g.LeftBearing, 0); first = false; }
            else ox += Spacing + g.LeftBearing;

            ox += g.Width;
            width = MathF.Max(width, ox + MathF.Max(g.RightBearing, 0));
            ox += g.RightBearing;
        }

        return new Vector2(width, lines * LineSpacing);
    }

    public sealed override string ToString() => Identifier;

    public ref struct TextEnumerator
    {
        private readonly FontResource font;
        private readonly Text text;

        private int index;
        private int line;
        private float ox, oy;
        private bool first;
        private GlyphInstance current;

        internal TextEnumerator(FontResource font, Text text)
        {
            this.font = font;
            this.text = text;

            index = -1;
            line = 0;
            ox = 0;
            oy = 0;
            first = true;
            current = default;
        }

        public GlyphInstance Current => current;

        public TextEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (++index < text.Length)
            {
                char c = text[index];

                if (c == '\r') continue;
                if (c == '\n')
                {
                    ox = 0;
                    oy += font.LineSpacing;
                    line++;
                    first = true;
                    continue;
                }

                if (!font.TryGetGlyph(c, out var g)) continue;

                if (first) { ox = MathF.Max(g.LeftBearing, 0); first = false; }
                else ox += font.Spacing + g.LeftBearing;

                var offset = new Vector2(ox + g.Cropping.X, oy + g.Cropping.Y);
                ox += g.Width + g.RightBearing;

                if (!g.IsVisible) continue;

                current = new GlyphInstance(c, index, line, text.Length, g.Texture, offset, new Vector2(g.Bounds.Width, g.Bounds.Height));
                return true;
            }

            return false;
        }
    }
}