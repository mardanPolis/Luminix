using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Verse.Rendering.RenderData;
using Verse.Resources;

namespace Verse.Rendering;
public sealed class RenderStack
{
    internal RenderStack(int[] interpSpritePrevCount, int[] spritePrevCount, int[] linePrevCount)
    {
        interpolatedSpriteRenderDataStack = new Stack<InstanceInterpolatedSprite>(1024);
        spriteRenderDataStack = new Stack<InstanceSprite>(1024);
        lineRenderDataStack = new Stack<InstanceLine>(512);

        interpolatedSpriteRenderDataStack.prevCounts = interpSpritePrevCount;
        spriteRenderDataStack.prevCounts = spritePrevCount;
        lineRenderDataStack.prevCounts = linePrevCount;
    }

    internal const int OPTIMIZATION_DELAY_TICKS = 60;
    internal const int OPTIMIZING_THRESHOLD = 128;

    internal class StackSharedData
    {
        public int[] prevCounts;
        int currentIndex = 0;
        int biggestCountIndex = -1;

        public void NextCount(int count)
        {
            void GetBiggest(int[] array, out int index)
            {
                int maxIndex = 0;

                for (int i = 0; i < array.Length; i++)
                    if (array[i] > array[maxIndex])
                        maxIndex = i;

                index = maxIndex;
            }
            

            prevCounts[currentIndex] = count;
            if (count > prevCounts[biggestCountIndex])
                biggestCountIndex = currentIndex;
            else if(currentIndex == biggestCountIndex)
                GetBiggest(prevCounts, out biggestCountIndex);

            currentIndex = (currentIndex + 1) % OPTIMIZATION_DELAY_TICKS;
        }
    }
    private struct Stack<T>(int size) where T : struct
    {
        public T[] DataSet = new T[size];
        public int Count = 0;

        public StackSharedData sharedData;

        int unResizedCount = 0;

        public void Next(ref T data)
        {
            if (DataSet.Length < Count + 1)
                Array.Resize(ref DataSet, DataSet.Length * 2);

            data = ref DataSet[Count];
            Count++;
        }

        public void TryOptimize()
        {
            unResizedCount++;
            if(unResizedCount < OPTIMIZATION_DELAY_TICKS)
                return;

            var maxPrevCapacity = prevCounts[biggestCountIndex];
            var maxCapacity = Count > maxPrevCapacity ? Count : maxPrevCapacity;

            var lowerArrayLength = DataSet.Length / 2;

            if(lowerArrayLength < maxCapacity + OPTIMIZING_THRESHOLD)
                DataSet = new T[lowerArrayLength];

            unResizedCount = 0;
        }
        internal void SkippedOptimization()
        {
            unResizedCount++;
        }
        internal void Reset()
        {
            sharedData.NextCount(Count);                                                                 
            Count = 0;
        }
    }

    private Stack<InstanceInterpolatedSprite> interpolatedSpriteRenderDataStack;
    private Stack<InstanceSprite> spriteRenderDataStack;
    private Stack<InstanceLine> lineRenderDataStack;

    public void PostSprite(Sprite sprite, Vector2 position0, Vector2 position, float rotation0, float rotation, Vector2 scale0, Vector2 scale, float layerDepth = 0f)
    {
        InstanceInterpolatedSprite instanceSprite = default;
        interpolatedSpriteRenderDataStack.Next(ref instanceSprite);

        instanceSprite.Sprite = sprite;
        instanceSprite.Position0 = position0;
        instanceSprite.Position = position;
        instanceSprite.Rotation0 = rotation0;
        instanceSprite.Rotation = rotation;
        instanceSprite.Scale0 = scale0;
        instanceSprite.Scale = scale;
        instanceSprite.LayerDepth = layerDepth;
    }
    public void PostSprite(Sprite sprite, Vector2 position, float rotation, Vector2 scale, float layerDepth = 0f)
    {
        InstanceSprite instanceSprite = default;
        spriteRenderDataStack.Next(ref instanceSprite);

        instanceSprite.Sprite = sprite;
        instanceSprite.Position = position;
        instanceSprite.Rotation = rotation;
        instanceSprite.Scale = scale;
        instanceSprite.LayerDepth = layerDepth;
    }
    public void PostLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        InstanceLine instanceLine = default;
        lineRenderDataStack.Next(ref instanceLine);

        instanceLine.Start = start;
        instanceLine.End = end;
        instanceLine.Color = color;
        instanceLine.Thickness = thickness;
    }

    public void Optimize()
    {
        interpolatedSpriteRenderDataStack.TryOptimize();
        spriteRenderDataStack.TryOptimize();
        lineRenderDataStack.TryOptimize();
    }

    public void SkipOptimization()
    {
        interpolatedSpriteRenderDataStack.SkippedOptimization();
        spriteRenderDataStack.SkippedOptimization();
        lineRenderDataStack.SkippedOptimization();
    }

    public void Reset()
    {
        interpolatedSpriteRenderDataStack.Reset();
        spriteRenderDataStack.Reset();
        lineRenderDataStack.Reset();
    }
}
