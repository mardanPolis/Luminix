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
        SpriteRenderDataSet = (InstanceSprite[])EnsureArrayCapacity(SpriteRenderDataSet, SpriteCount + 1);

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
        LineRenderDataSet = (InstanceLine[])EnsureArrayCapacity(LineRenderDataSet, LineCount + 1);

        ref var lineInstance = ref LineRenderDataSet[LineCount];
        lineInstance.Start = start;
        lineInstance.End = end;
        lineInstance.Color = color;
        lineInstance.Thickness = thickness;
        LineCount++;
    }

    private Array EnsureArrayCapacity(Array array, int requiredCapacity)
    {
        Array newArray = array;
        if (array.Length < requiredCapacity)
        {
            newArray = Array.CreateInstance(array.GetType().GetElementType(), array.Length * 2);
            Array.Copy(array, newArray, array.Length);
        }

        return newArray;
    }

    internal void Reset()
    {
        SpriteCount = 0;
        LineCount = 0;
    }

    // used only after reset to ensure the arrays are large enough to hold new render data
    internal void Resize(int SpriteCapacity, int LineCapacity)
    {
        if(SpriteCapacity > SpriteRenderDataSet.Length)
            SpriteRenderDataSet = new InstanceSprite[SpriteCapacity];

        if(LineCapacity > LineRenderDataSet.Length)
            LineRenderDataSet = new InstanceLine[LineCapacity];
    }

    internal void Optimize()
    {   
        var spriteCount = this.SpriteCount > 1024 ? this.SpriteCount : 1024;
        var lineCount = this.LineCount > 512 ? this.LineCount : 512;

        SpriteRenderDataSet = new InstanceSprite[spriteCount];
        LineRenderDataSet = new InstanceLine[lineCount];
    }
}
