using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace Luminix;

// ---------------------------------------------------------------------------------------------
//  XnbContainer : header + (optional) LZ4 decompression + type-reader table. Works for any XNB.
// ---------------------------------------------------------------------------------------------
internal sealed class XnbContainer
{
    private const byte FlagLz4 = 0x40; // MonoGame
    private const byte FlagLzx = 0x80; // XNA

    private XnbContainer(byte[] body, int objectOffset, string primaryReader)
    {
        Body = body;
        ObjectOffset = objectOffset;
        PrimaryReader = primaryReader;
    }

    /// <summary>Decompressed content, starting at the type-reader table.</summary>
    public byte[] Body { get; }

    /// <summary>Offset inside Body where the primary object's data starts (after its type index).</summary>
    public int ObjectOffset { get; }

    /// <summary>Full type name of the primary object's reader, e.g. "...SpriteFontReader, MonoGame.Framework".</summary>
    public string PrimaryReader { get; }

    public static XnbContainer Open(byte[] file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length < 14 || file[0] != 'X' || file[1] != 'N' || file[2] != 'B')
            throw new InvalidDataException("Not an XNB file.");

        byte version = file[4];
        byte flags = file[5];
        if (version != 4 && version != 5)
            throw new NotSupportedException($"XNB version {version} is not supported.");

        int fileSize = Math.Min(BitConverter.ToInt32(file, 6), file.Length);

        byte[] body;
        if ((flags & FlagLzx) != 0)
            throw new NotSupportedException("LZX-compressed XNB. Rebuild the content without compression or with LZ4.");

        if ((flags & FlagLz4) != 0)
        {
            int decompressedSize = BitConverter.ToInt32(file, 10);
            body = new byte[decompressedSize];
            Lz4.DecodeBlock(file, 14, fileSize - 14, body);
        }
        else
        {
            body = new byte[fileSize - 10];
            Buffer.BlockCopy(file, 10, body, 0, body.Length);
        }

        using var ms = new MemoryStream(body, writable: false);
        using var br = new BinaryReader(ms, Encoding.UTF8);

        int readerCount = Read7Bit(br);
        var readers = new string[readerCount];
        for (int i = 0; i < readerCount; i++)
        {
            readers[i] = br.ReadString();
            br.ReadInt32(); // reader version
        }

        Read7Bit(br);                  // shared resource count
        int primary = Read7Bit(br);    // 1-based, 0 = null
        string name = primary == 0 ? string.Empty : readers[primary - 1];

        return new XnbContainer(body, (int)ms.Position, name);
    }

    internal static int Read7Bit(BinaryReader br)
    {
        int result = 0, shift = 0;
        byte b;
        do
        {
            b = br.ReadByte();
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return result;
    }
}

// ---------------------------------------------------------------------------------------------
//  Minimal LZ4 *block* decoder (what MonoGame writes). No external dependency.
// ---------------------------------------------------------------------------------------------
internal static class Lz4
{
    public static void DecodeBlock(byte[] src, int srcOffset, int srcLength, byte[] dst)
    {
        int sp = srcOffset;
        int end = srcOffset + srcLength;
        int dp = 0;

        while (sp < end && dp < dst.Length)
        {
            int token = src[sp++];

            int literalLength = token >> 4;
            if (literalLength == 15)
            {
                int b;
                do { b = src[sp++]; literalLength += b; } while (b == 255);
            }

            Buffer.BlockCopy(src, sp, dst, dp, literalLength);
            sp += literalLength;
            dp += literalLength;

            if (sp >= end || dp >= dst.Length) break; // last sequence has literals only

            int offset = src[sp] | (src[sp + 1] << 8);
            sp += 2;

            int matchLength = token & 0x0F;
            if (matchLength == 15)
            {
                int b;
                do { b = src[sp++]; matchLength += b; } while (b == 255);
            }
            matchLength += 4;

            int mp = dp - offset;
            if (mp < 0) throw new InvalidDataException("Corrupt LZ4 stream.");
            for (int i = 0; i < matchLength && dp < dst.Length; i++)
                dst[dp++] = dst[mp++]; // byte-wise on purpose: matches may overlap
        }

        if (dp != dst.Length)
            throw new InvalidDataException($"LZ4 size mismatch (expected {dst.Length}, got {dp}).");
    }
}

// ---------------------------------------------------------------------------------------------
//  Result of parsing: raw glyph data for the core thread + RGBA atlas for the render thread.
// ---------------------------------------------------------------------------------------------
internal readonly struct XnbGlyph
{
    public XnbGlyph(char character, Rectangle bounds, Rectangle cropping, float leftBearing, float width, float rightBearing)
    {
        Character = character;
        Bounds = bounds;
        Cropping = cropping;
        LeftBearing = leftBearing;
        Width = width;
        RightBearing = rightBearing;
    }

    public char Character { get; }
    public Rectangle Bounds { get; }
    public Rectangle Cropping { get; }
    public float LeftBearing { get; }
    public float Width { get; }
    public float RightBearing { get; }
}

internal sealed class XnbFontResult
{
    public int LineSpacing { get; init; }
    public float Spacing { get; init; }
    public char? DefaultCharacter { get; init; }
    public XnbGlyph[] Glyphs { get; init; }
    public Color[] AtlasPixels { get; init; }
    public int AtlasWidth { get; init; }
    public int AtlasHeight { get; init; }
}

// ---------------------------------------------------------------------------------------------
//  The parser itself. Mirrors SpriteFontReader.Read() field-for-field.
// ---------------------------------------------------------------------------------------------
internal static class XnbFontParser
{
    public static XnbFontResult Parse(byte[] xnbFile)
    {
        var container = XnbContainer.Open(xnbFile);

        if (!container.PrimaryReader.Contains("SpriteFontReader", StringComparison.Ordinal))
            throw new InvalidDataException($"XNB primary object is '{container.PrimaryReader}', expected a SpriteFont.");

        using var ms = new MemoryStream(container.Body, writable: false);
        using var br = new BinaryReader(ms, Encoding.UTF8);
        ms.Position = container.ObjectOffset;

        // SpriteFontReader.Read order:
        var (atlasPixels, w, h) = ReadTexture2D(br);
        List<Rectangle> glyphRects = ReadRectList(br);
        List<Rectangle> croppings = ReadRectList(br);
        List<char> characters = ReadCharList(br);
        int lineSpacing = br.ReadInt32();
        float spacing = br.ReadSingle();
        List<Vector3> kerning = ReadVector3List(br);
        char? defaultChar = br.ReadBoolean() ? br.ReadChar() : null;

        if (glyphRects.Count != characters.Count || croppings.Count != characters.Count || kerning.Count != characters.Count)
            throw new InvalidDataException("Font tables have mismatching lengths.");

        var glyphs = new XnbGlyph[characters.Count];
        for (int i = 0; i < glyphs.Length; i++)
        {
            Vector3 k = kerning[i]; // X = left bearing, Y = width, Z = right bearing
            glyphs[i] = new XnbGlyph(characters[i], glyphRects[i], croppings[i], k.X, k.Y, k.Z);
        }

        return new XnbFontResult
        {
            LineSpacing = lineSpacing,
            Spacing = spacing,
            DefaultCharacter = defaultChar,
            Glyphs = glyphs,
            AtlasPixels = atlasPixels,
            AtlasWidth = w,
            AtlasHeight = h
        };
    }

    // ---------- object readers ----------

    private static (Color[] pixels, int width, int height) ReadTexture2D(BinaryReader br)
    {
        if (XnbContainer.Read7Bit(br) == 0)
            throw new InvalidDataException("Font has no texture.");

        int format = br.ReadInt32();
        int width = br.ReadInt32();
        int height = br.ReadInt32();
        int mipCount = br.ReadInt32();

        byte[] level0 = Array.Empty<byte>();
        for (int i = 0; i < mipCount; i++)
        {
            int size = br.ReadInt32();
            byte[] data = br.ReadBytes(size);
            if (i == 0) level0 = data;
        }

        var pixels = new Color[width * height];
        TextureDecoder.Decode(format, width, height, level0, pixels);
        return (pixels, width, height);
    }

    // List<T>: 7-bit type index, int32 count, then elements WITHOUT a per-element type index (value types).
    private static List<Rectangle> ReadRectList(BinaryReader br)
    {
        if (XnbContainer.Read7Bit(br) == 0) return new List<Rectangle>();
        int n = br.ReadInt32();
        var list = new List<Rectangle>(n);
        for (int i = 0; i < n; i++)
        {
            int x = br.ReadInt32(), y = br.ReadInt32(), w = br.ReadInt32(), h = br.ReadInt32();
            list.Add(new Rectangle(x, y, w, h));
        }
        return list;
    }

    private static List<char> ReadCharList(BinaryReader br)
    {
        if (XnbContainer.Read7Bit(br) == 0) return new List<char>();
        int n = br.ReadInt32();
        var list = new List<char>(n);
        for (int i = 0; i < n; i++) list.Add(br.ReadChar());
        return list;
    }

    private static List<Vector3> ReadVector3List(BinaryReader br)
    {
        if (XnbContainer.Read7Bit(br) == 0) return new List<Vector3>();
        int n = br.ReadInt32();
        var list = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
        {
            float x = br.ReadSingle(), y = br.ReadSingle(), z = br.ReadSingle();
            list.Add(new Vector3(x, y, z));
        }
        return list;
    }
}

// ---------------------------------------------------------------------------------------------
//  Pixel decoding to Color[] (RGBA, same alpha convention as stored in the XNB, normally premultiplied).
//  Format ids are the XNA SurfaceFormat values stored in the file.
// ---------------------------------------------------------------------------------------------
internal static class TextureDecoder
{
    private const int FmtColor = 0, FmtBgr565 = 1, FmtBgra5551 = 2, FmtBgra4444 = 3, FmtDxt1 = 4, FmtDxt3 = 5, FmtDxt5 = 6;

    public static void Decode(int format, int w, int h, byte[] src, Color[] dst)
    {
        switch (format)
        {
            case FmtColor:
                for (int i = 0; i < dst.Length; i++)
                {
                    int b = i * 4;
                    dst[i] = new Color(src[b], src[b + 1], src[b + 2], src[b + 3]);
                }
                break;

            case FmtBgr565:
                for (int i = 0; i < dst.Length; i++)
                {
                    int v = src[i * 2] | (src[i * 2 + 1] << 8);
                    dst[i] = new Color(E5((v >> 11) & 31), E6((v >> 5) & 63), E5(v & 31), (byte)255);
                }
                break;

            case FmtBgra5551:
                for (int i = 0; i < dst.Length; i++)
                {
                    int v = src[i * 2] | (src[i * 2 + 1] << 8);
                    dst[i] = new Color(E5((v >> 10) & 31), E5((v >> 5) & 31), E5(v & 31), (byte)(((v >> 15) & 1) * 255));
                }
                break;

            case FmtBgra4444:
                for (int i = 0; i < dst.Length; i++)
                {
                    int v = src[i * 2] | (src[i * 2 + 1] << 8);
                    dst[i] = new Color((byte)(((v >> 8) & 15) * 17), (byte)(((v >> 4) & 15) * 17),
                                       (byte)((v & 15) * 17), (byte)(((v >> 12) & 15) * 17));
                }
                break;

            case FmtDxt1:
            case FmtDxt3:
            case FmtDxt5:
                DecodeDxt(format, w, h, src, dst);
                break;

            default:
                throw new NotSupportedException($"Atlas surface format {format} is not supported.");
        }
    }

    private static byte E5(int v) => (byte)((v << 3) | (v >> 2));
    private static byte E6(int v) => (byte)((v << 2) | (v >> 4));

    private static void DecodeDxt(int format, int w, int h, byte[] src, Color[] dst)
    {
        int blocksX = (w + 3) / 4;
        int blocksY = (h + 3) / 4;

        var pr = new byte[4];
        var pg = new byte[4];
        var pb = new byte[4];
        var pa = new byte[4];
        var alpha = new byte[16];
        var alphaPal = new byte[8];

        int p = 0;
        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                int colorOffset = p;

                if (format == FmtDxt3)
                {
                    // 4 bits explicit alpha per pixel, row-major, low nibble first
                    for (int i = 0; i < 16; i++)
                        alpha[i] = (byte)(((src[p + (i >> 1)] >> ((i & 1) * 4)) & 0xF) * 17);
                    colorOffset = p + 8;
                }
                else if (format == FmtDxt5)
                {
                    byte a0 = src[p], a1 = src[p + 1];
                    ulong bits = 0;
                    for (int k = 0; k < 6; k++) bits |= (ulong)src[p + 2 + k] << (8 * k);

                    alphaPal[0] = a0;
                    alphaPal[1] = a1;
                    if (a0 > a1)
                        for (int i = 1; i <= 6; i++) alphaPal[i + 1] = (byte)(((7 - i) * a0 + i * a1) / 7);
                    else
                    {
                        for (int i = 1; i <= 4; i++) alphaPal[i + 1] = (byte)(((5 - i) * a0 + i * a1) / 5);
                        alphaPal[6] = 0;
                        alphaPal[7] = 255;
                    }

                    for (int i = 0; i < 16; i++) alpha[i] = alphaPal[(int)((bits >> (3 * i)) & 7)];
                    colorOffset = p + 8;
                }

                int c0 = src[colorOffset] | (src[colorOffset + 1] << 8);
                int c1 = src[colorOffset + 2] | (src[colorOffset + 3] << 8);
                uint indices = (uint)(src[colorOffset + 4] | (src[colorOffset + 5] << 8) |
                                      (src[colorOffset + 6] << 16) | (src[colorOffset + 7] << 24));

                pr[0] = E5((c0 >> 11) & 31); pg[0] = E6((c0 >> 5) & 63); pb[0] = E5(c0 & 31); pa[0] = 255;
                pr[1] = E5((c1 >> 11) & 31); pg[1] = E6((c1 >> 5) & 63); pb[1] = E5(c1 & 31); pa[1] = 255;

                if (format != FmtDxt1 || c0 > c1)
                {
                    pr[2] = (byte)((2 * pr[0] + pr[1]) / 3); pg[2] = (byte)((2 * pg[0] + pg[1]) / 3); pb[2] = (byte)((2 * pb[0] + pb[1]) / 3); pa[2] = 255;
                    pr[3] = (byte)((pr[0] + 2 * pr[1]) / 3); pg[3] = (byte)((pg[0] + 2 * pg[1]) / 3); pb[3] = (byte)((pb[0] + 2 * pb[1]) / 3); pa[3] = 255;
                }
                else
                {
                    pr[2] = (byte)((pr[0] + pr[1]) / 2); pg[2] = (byte)((pg[0] + pg[1]) / 2); pb[2] = (byte)((pb[0] + pb[1]) / 2); pa[2] = 255;
                    pr[3] = 0; pg[3] = 0; pb[3] = 0; pa[3] = 0; // DXT1 1-bit alpha
                }

                for (int i = 0; i < 16; i++)
                {
                    int x = bx * 4 + (i & 3);
                    int y = by * 4 + (i >> 2);
                    if (x >= w || y >= h) continue;

                    int idx = (int)((indices >> (2 * i)) & 3);
                    byte a = format == FmtDxt1 ? pa[idx] : alpha[i];
                    dst[y * w + x] = new Color(pr[idx], pg[idx], pb[idx], a);
                }

                p += format == FmtDxt1 ? 8 : 16;
            }
        }
    }
}