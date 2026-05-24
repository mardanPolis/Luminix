using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RectpackSharp;

namespace Luminix.Resources;

public sealed class AtlasMaker
{
    private readonly record struct Entry(string Name, Color[] Colors, int Width, int Height);
    private readonly List<Entry> entries = new();

    public readonly record struct AtlasResult(
        Color[]     AtlasColors,
        Vector2     AtlasSize,
        AtlasResult.TextureRegion[] TextureRegions
    )
    {
        public readonly record struct TextureRegion(string Name, Rectangle Rectangle);
    }

    public void Add(string name, Color[] colors, Vector2 size)
    {
        PadColors(colors, size, out Color[] paddedColors, out Vector2 paddedSize);
        entries.Add(new Entry(name, paddedColors, (int)paddedSize.X, (int)paddedSize.Y));
    }

    public AtlasResult[] Pack()
    {
        if (entries.Count == 0) return Array.Empty<AtlasResult>();

        var results = new List<AtlasResult>();
        var remaining = new List<(Entry entry, int originalId)>(
            entries.Select((e, i) => (e, i))
        );

        while (remaining.Count > 0)
        {
            int lo = 1, hi = remaining.Count, fittingCount = 0;
            PackingRectangle[] fittingRects = Array.Empty<PackingRectangle>();
            PackingRectangle fittingBounds = default;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var candidate = BuildRects(remaining, mid);
                try
                {
                    RectanglePacker.Pack(candidate, out PackingRectangle bounds,
                        maxBoundsWidth: 4096, maxBoundsHeight: 4096);
                    fittingCount = mid;
                    fittingRects = candidate;
                    fittingBounds = bounds;
                    lo = mid + 1;
                }
                catch
                {
                    hi = mid - 1;
                }
            }

            if (fittingCount == 0)
                throw new InvalidOperationException(
                    $"AtlasMaker: a single texture is too large to fit in a 4096x4096 atlas: " +
                    $"'{remaining[0].entry.Name}' ({remaining[0].entry.Width}x{remaining[0].entry.Height})");

            results.Add(BuildAtlas(fittingRects, fittingBounds, remaining));
            remaining.RemoveRange(0, fittingCount);
        }

        entries.Clear();
        return results.ToArray();
    }

    private static void PadColors(Color[] colors, Vector2 size, out Color[] resultColor, out Vector2 resultSize)
    {
        Vector2 paddedColorsSize = new Vector2(size.X + 2, size.Y + 2);
        Color[] paddedColors = new Color[(int)((size.X + 2) * (size.Y + 2))];

        //ADDING PADDED COLORS
        int padTopLeft = 0;
        int padTopRight = (int)paddedColorsSize.X - 1;
        int padBtmLeft = paddedColors.Length - (int)paddedColorsSize.X;
        int padBtmRight = paddedColors.Length - 1;

        int topLeft = 0;
        int topRight = (int)size.X - 1;
        int btmLeft = colors.Length - (int)size.X;
        int btmRight = colors.Length - 1;

        // interior rows
        for (int r = 0; r < (int)size.Y; r++)
        {
            int srcOffset  = r * (int)size.X;
            int destOffset = (r + 1) * (int)paddedColorsSize.X + 1;
            colors.AsSpan(srcOffset, (int)size.X)
                .CopyTo(paddedColors.AsSpan(destOffset, (int)size.X));
        }
        
        //corners
        paddedColors[padTopLeft] = colors[topLeft];
        paddedColors[padTopRight] = colors[topRight];
        paddedColors[padBtmLeft] = colors[btmLeft];
        paddedColors[padBtmRight] = colors[btmRight];

        //rows
        colors.AsSpan(topLeft, (int)size.X).CopyTo(paddedColors.AsSpan(padTopLeft + 1, (int)size.X));
        colors.AsSpan(btmLeft, (int)size.X).CopyTo(paddedColors.AsSpan(padBtmLeft + 1, (int)size.X));

        // columns
        for (int r = 0; r < (int)size.Y; r++)
        {
            int srcLeft  = r * (int)size.X;
            int destLeft  = (r + 1) * (int)paddedColorsSize.X;
            paddedColors[destLeft]  = colors[srcLeft];

            int srcRight = r * (int)size.X + (int)size.X - 1;
            int destRight = (r + 1) * (int)paddedColorsSize.X + (int)paddedColorsSize.X - 1;
            paddedColors[destRight] = colors[srcRight];
        }

        resultColor = paddedColors;
        resultSize = paddedColorsSize;
    }

    private static PackingRectangle[] BuildRects(List<(Entry entry, int originalId)> source, int count)
    {
        var rects = new PackingRectangle[count];
        for (int i = 0; i < count; i++)
            rects[i] = new PackingRectangle(0, 0,
                (uint)source[i].entry.Width,
                (uint)source[i].entry.Height,
                i);
        return rects;
    }

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 0) return 1;
        int p = 1;
        while (p < value) p <<= 1;
        return p;
    }

    private static AtlasResult BuildAtlas(
        PackingRectangle[] rects,
        PackingRectangle bounds,
        List<(Entry entry, int originalId)> source)
    {
        int atlasW = NextPowerOfTwo((int)bounds.Width);
        int atlasH = NextPowerOfTwo((int)bounds.Height);

        var atlasColors  = new Color[atlasW * atlasH];
        var textureRegions = new AtlasResult.TextureRegion[rects.Length];

        foreach (var rect in rects)
        {
            int sliceIdx = rect.Id;
            var (entry, _) = source[sliceIdx];

            textureRegions[sliceIdx] = new(
                entry.Name,
                new Rectangle((int)rect.X + 1, (int)rect.Y + 1,
                            entry.Width - 2,  entry.Height - 2));

            for (int row = 0; row < entry.Height; row++)
            {
                int srcOffset  = row * entry.Width;
                int destOffset = ((int)rect.Y + row) * atlasW + (int)rect.X;
                entry.Colors.AsSpan(srcOffset, entry.Width)
                            .CopyTo(atlasColors.AsSpan(destOffset, entry.Width));
            }
        }

        return new AtlasResult(atlasColors, new Vector2(atlasW, atlasH), textureRegions);
    }
}