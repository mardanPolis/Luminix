using System;
using System.Collections.Generic;
using System.Linq;
using Luminix.IO;
using Microsoft.Xna.Framework;
using RectpackSharp;

namespace Luminix.Resources;

public sealed class AtlasMaker
{
    private readonly record struct Entry(string Name, Color[] Colors, Vector2 Size);
    private readonly List<Entry> entries = new();

    public void Add(TextureAsset asset)
    {
        PadColors(asset.Colors, asset.Size, out Color[] paddedColors, out Vector2 paddedSize);
        entries.Add(new Entry(asset.Name, paddedColors, paddedSize));
    }

    public AtlasAsset[] Pack(string atlasName)
    {
        if (entries.Count == 0) return Array.Empty<AtlasAsset>();

        var results = new List<AtlasAsset>();
        var remaining = new List<(Entry entry, int originalId)>(
            entries.Select((e, i) => (e, i))
        );

        int atlasCount = 0;

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
                    $"'{remaining[0].entry.Name}' ({remaining[0].entry.Size.X}x{remaining[0].entry.Size.Y})");

            results.Add(BuildAtlas(atlasName + "_" + atlasCount, fittingRects, fittingBounds, remaining));
            remaining.RemoveRange(0, fittingCount);
            atlasCount++;
        }

        entries.Clear();
        return results.ToArray();
    }

    private static void PadColors(Color[] colors, Vector2 size, out Color[] _resultColor, out Vector2 _resultSize)
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

        _resultColor = paddedColors;
        _resultSize = paddedColorsSize;
    }

    private static PackingRectangle[] BuildRects(List<(Entry entry, int originalId)> source, int count)
    {
        var rects = new PackingRectangle[count];
        for (int i = 0; i < count; i++)
            rects[i] = new PackingRectangle(0, 0,
                (uint)source[i].entry.Size.X,
                (uint)source[i].entry.Size.Y,
                i);
        return rects;
    }

    private static AtlasAsset BuildAtlas(
        string name,
        PackingRectangle[] rects,
        PackingRectangle bounds,
        List<(Entry entry, int originalId)> source)
    {

        int atlasW = (int)System.Numerics.BitOperations.RoundUpToPowerOf2(bounds.Width);
        int atlasH = (int)System.Numerics.BitOperations.RoundUpToPowerOf2(bounds.Width);

        var atlasColors  = new Color[atlasW * atlasH];
        var regions = new AtlasAsset.Region[rects.Length];

        foreach (var rect in rects)
        {
            int sliceIdx = rect.Id;
            var (entry, _) = source[sliceIdx];

            int width = (int)entry.Size.X;
            int height = (int)entry.Size.Y;

            regions[sliceIdx] = new(
                entry.Name,
                new Rectangle((int)rect.X + 1, (int)rect.Y + 1,
                            width - 2,  height - 2));

            for (int row = 0; row < height; row++)
            {
                int srcOffset  = row * width;
                int destOffset = ((int)rect.Y + row) * atlasW + (int)rect.X;
                entry.Colors.AsSpan(srcOffset, width)
                            .CopyTo(atlasColors.AsSpan(destOffset, width));
            }
        }

        return new AtlasAsset(name, atlasColors, new Vector2(atlasW, atlasH), regions);
    }
}