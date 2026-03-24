using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Verse.Rendering.RenderData;
using Verse.Resources;

namespace Verse.Rendering;
public sealed class RenderStack
{
    const int OPTIMIZATION_DELAY_TICKS = 60;
    const int OPTIMIZING_THRESHOLD = 128;

    internal InstanceSprite[] SpriteRenderDataSet { get; private set; } = new InstanceSprite[1024];
    internal int SpriteCount = 0;
    internal InstanceLine[] LineRenderDataSet { get; private set; } = new InstanceLine[512];
    internal int LineCount = 0;

    int[] prevSpriteRenderableCount = new int[OPTIMIZATION_DELAY_TICKS];
    int[] prevLineRenderableCount = new int[OPTIMIZATION_DELAY_TICKS];
    int currentIndex = 0;

    int unResizedCountSprite = 0;
    int unResizedCountLine = 0;

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

    internal void Reset()
    {
        prevSpriteRenderableCount[currentIndex] = SpriteCount;
        prevLineRenderableCount[currentIndex] = LineCount;

        currentIndex = (currentIndex + 1) % OPTIMIZATION_DELAY_TICKS;

        SpriteCount = 0;
        LineCount = 0;
    }

    internal void Optimize()
    {
        //Sprite
        unResizedCountSprite++;
        if(unResizedCountSprite > OPTIMIZATION_DELAY_TICKS)
        {
            if(TryOptimize(SpriteRenderDataSet, SpriteCount, prevSpriteRenderableCount, 1024, out Array result))
                SpriteRenderDataSet = (InstanceSprite[])result;

            unResizedCountSprite = 0;
        }


        //Line
        unResizedCountLine++;
        if(unResizedCountLine > OPTIMIZATION_DELAY_TICKS)
        {
            if(TryOptimize(LineRenderDataSet, LineCount, prevLineRenderableCount, 512, out Array result))
                LineRenderDataSet = (InstanceLine[])result;
            unResizedCountLine = 0;
        }
    }

    internal void SkipOptimization()
    {
        unResizedCountSprite++;
        unResizedCountLine++;
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
    private bool TryOptimize(Array array, int currentCount, int[] prevCapacities, int minLength, out Array result)
    {
        result = array;

        var maxPrevCapacity = prevCapacities.Max();
        var maxCapacity = currentCount > maxPrevCapacity ? currentCount : maxPrevCapacity;

        var lowerArrayLength = array.Length / 2;

        if(lowerArrayLength < maxCapacity + OPTIMIZING_THRESHOLD) // return if lowerArrayLength is lower than current array count;
            return false;

        result = Array.CreateInstance(array.GetType().GetElementType(), lowerArrayLength);
        return true;
    }
}
