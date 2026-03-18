using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Verse.Rendering.RenderData;

namespace Verse.Rendering;
public class RenderStack
{
    private List<SpriteRenderData> spriteRenderDataSet = new List<SpriteRenderData>();
    private List<LineRenderData> lineRenderDataSet = new List<LineRenderData>();

    public void PostSprite(Vector2 position, float rotation, Vector2 scale, Sprite sprite, Color color)
    {
        spriteRenderDataSet.Add(new SpriteRenderData(position, rotation, scale, sprite, color));
    }

    public void PostLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        lineRenderDataSet.Add(new LineRenderData(start, end, color, thickness));
    }

    internal List<SpriteRenderData> GetSpriteRenderDataSet()
    {
        return spriteRenderDataSet;
    }

    internal List<LineRenderData> GetLineRenderDataSet()
    {
        return lineRenderDataSet;
    }

    internal void Clear()
    {
        spriteRenderDataSet.Clear();
        lineRenderDataSet.Clear();
    }
}
