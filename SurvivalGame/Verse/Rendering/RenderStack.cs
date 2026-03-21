using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering.RenderData;
using Verse.Resources;

namespace Verse.Rendering;
public class RenderStack
{
    internal InstanceSprite[] SpriteRenderDataSet { get; private set; } = new InstanceSprite[1024];
    internal int SpriteCount = 0;
    internal InstanceLine[] LineRenderDataSet { get; private set; } = new InstanceLine[512];
    internal int LineCount = 0;

    public void PostSprite(Sprite sprite, Vector2 position, float rotation, Vector2 scale, float layerDepth = 0f)
    {
        ref var spriteInstance = ref SpriteRenderDataSet[SpriteCount];
        spriteInstance.Sprite = sprite;
        spriteInstance.Position = position;
        spriteInstance.Rotation = rotation;
        spriteInstance.Scale = scale;
        spriteInstance.LayerDepth = layerDepth;
        SpriteCount++;
    }

    public void PostLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        LineRenderDataSet[LineCount] = new InstanceLine(start, end, color, thickness);
        LineCount++;
    }

    internal void Optimize()
    {   
        var spriteCount = this.SpriteCount > 1024 ? this.SpriteCount : 1024;
        var lineCount = this.LineCount > 512 ? this.LineCount : 512;

        var newSpriteRenderDataSet = new InstanceSprite[spriteCount];
        Array.Copy(SpriteRenderDataSet, newSpriteRenderDataSet, spriteCount);
        SpriteRenderDataSet = newSpriteRenderDataSet;
        
        var newLineRenderDataSet = new InstanceLine[lineCount];
        Array.Copy(LineRenderDataSet, newLineRenderDataSet, lineCount);
        LineRenderDataSet = newLineRenderDataSet;
    }
    internal void Reset()
    {
        SpriteCount = 0;
        LineCount = 0;
    }
}
