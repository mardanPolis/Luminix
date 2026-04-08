using System;
using Microsoft.Xna.Framework;
using Verse.Simulation;
using Verse.Rendering.RenderInstances;

namespace Verse.Rendering;

public sealed class RenderStack
{
    internal RenderStack(StackSharedData interpSpriteSharedData, StackSharedData spriteSharedData, StackSharedData lineSharedData, StackSharedData textSharedData)
    {
        interpolatedSpriteRenderDataStack = new Stack<InstanceInterpolatedSprite>(1024);
        spriteRenderDataStack = new Stack<InstanceSprite>(1024);
        lineRenderDataStack = new Stack<InstanceLine>(512);
        textRenderDataStack = new Stack<InstanceText>(128);

        interpolatedSpriteRenderDataStack.sharedData = interpSpriteSharedData;
        spriteRenderDataStack.sharedData = spriteSharedData;
        lineRenderDataStack.sharedData = lineSharedData;
        textRenderDataStack.sharedData = textSharedData;
    }

    internal class StackSharedData
    {
        public readonly int[] prevCounts = new int[OPTIMIZATION_DELAY_TICKS];
        int currentIndex = 0;
        int biggestCountIndex = 0;
        public int unResizedCount = 0;

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

        public int GetBiggestCount()
        {
            return prevCounts[biggestCountIndex];
        }
    }
    private struct Stack<T>(int size) where T : struct
    {
        public T[] DataSet = new T[size];
        public int Count = 0;

        public StackSharedData sharedData;

        public ref T Next()
        {
            if (DataSet.Length < Count + 1)
                Array.Resize(ref DataSet, DataSet.Length * 2);

            return ref DataSet[Count++];
        }

        public void TryOptimize()
        {
            sharedData.unResizedCount++;
            if(sharedData.unResizedCount < OPTIMIZATION_DELAY_TICKS)
                return;

            var maxPrevCapacity = sharedData.GetBiggestCount();
            var maxCapacity = Count > maxPrevCapacity ? Count : maxPrevCapacity;

            var lowerArrayLength = DataSet.Length / 2;

            if(lowerArrayLength < maxCapacity + OPTIMIZING_THRESHOLD)
                DataSet = new T[lowerArrayLength];

            sharedData.unResizedCount = 0;
        }
        internal void SkippedOptimization()
        {
            sharedData.unResizedCount++;
        }
        internal void Reset()
        {
            sharedData.NextCount(Count);                                                   
            Count = 0;
        }
    }

    internal const int OPTIMIZATION_DELAY_TICKS = 60;
    internal const int OPTIMIZING_THRESHOLD = 128;

    private Stack<InstanceInterpolatedSprite> interpolatedSpriteRenderDataStack;
    private Stack<InstanceSprite> spriteRenderDataStack;
    private Stack<InstanceLine> lineRenderDataStack;
    private Stack<InstanceText> textRenderDataStack;

    private bool cameraDataPosted = false;

    private Vector2 cameraPosition0;
    private Vector2 cameraPosition;

    private float OrthoSize0;
    private float OrthoSize;

    internal InstanceInterpolatedSprite[] InstanceInterpolatedSprites => interpolatedSpriteRenderDataStack.DataSet;
    internal int InterpolatedSpriteCount => interpolatedSpriteRenderDataStack.Count;
    internal InstanceSprite[] InstanceSprites => spriteRenderDataStack.DataSet;
    internal int SpriteCount => spriteRenderDataStack.Count;
    internal InstanceLine[] InstanceLines => lineRenderDataStack.DataSet;
    internal int LineCount => lineRenderDataStack.Count;
    internal InstanceText[] InstanceText => textRenderDataStack.DataSet;
    internal int TextCount => textRenderDataStack.Count;

    internal void PostCameraData(Camera camera)
    {
        if(camera == null)
            return;

        cameraDataPosted = true;

        cameraPosition0 = camera.Position;
        cameraPosition = camera.TPosition;
        OrthoSize0 = camera.OrthographicSize;
        OrthoSize = camera.TOrthographicSize;
    }

    public void PostSprite(Sprite sprite, Vector2 position0, Vector2 position, float rotation0, float rotation, Vector2 scale0, Vector2 scale, float layerDepth = 0f)
    {
        ref var instanceSprite = ref interpolatedSpriteRenderDataStack.Next();

        instanceSprite.Sprite = sprite;
        instanceSprite.Position0 = position0;
        instanceSprite.Position = position;
        instanceSprite.Rotation0 = rotation0;
        instanceSprite.Rotation = rotation;
        instanceSprite.Scale0 = new Vector2(scale0.X * sprite.TextureRegion.WidthCoefficient, scale0.Y * sprite.TextureRegion.HeightCoefficient);
        instanceSprite.Scale = new Vector2(scale.X * sprite.TextureRegion.WidthCoefficient, scale.Y * sprite.TextureRegion.HeightCoefficient);
        instanceSprite.LayerDepth = layerDepth;
    }
    public void PostSprite(Sprite sprite, Vector2 position, float rotation, Vector2 scale, float layerDepth = 0f)
    {
        ref var instanceSprite = ref spriteRenderDataStack.Next();

        instanceSprite.Sprite = sprite;
        instanceSprite.Position = position;
        instanceSprite.Rotation = rotation;
        instanceSprite.Scale = new Vector2(scale.X * sprite.TextureRegion.WidthCoefficient, scale.Y * sprite.TextureRegion.HeightCoefficient);
        instanceSprite.LayerDepth = layerDepth;
    }
    public void PostLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        ref var instanceLine = ref lineRenderDataStack.Next();

        instanceLine.Start = start;
        instanceLine.End = end;
        instanceLine.Color = color;
        instanceLine.Thickness = thickness;
    }

    internal void Optimize()
    {
        interpolatedSpriteRenderDataStack.TryOptimize();
        spriteRenderDataStack.TryOptimize();
        lineRenderDataStack.TryOptimize();
        textRenderDataStack.TryOptimize();
    }
    internal void SkipOptimization()
    {
        interpolatedSpriteRenderDataStack.SkippedOptimization();
        spriteRenderDataStack.SkippedOptimization();
        lineRenderDataStack.SkippedOptimization();
        textRenderDataStack.SkippedOptimization();
    }

    public void Reset()
    {
        cameraDataPosted = false;

        interpolatedSpriteRenderDataStack.Reset();
        spriteRenderDataStack.Reset();
        lineRenderDataStack.Reset();
        textRenderDataStack.Reset();
    }

    internal bool CanRender()
    {
        return cameraDataPosted;
    }

    internal Matrix GenerateCameraViewMatrix(float t, float viewportHeight, float viewportWidth)
    {
        var tarPos = cameraPosition0 + (cameraPosition - cameraPosition0) * t;
        var tarOrthoSize = OrthoSize0 + (OrthoSize - OrthoSize0) * t;

        var tarScale = viewportHeight / tarOrthoSize;

        return Matrix.CreateTranslation(new Vector3(-tarPos.X, tarPos.Y, 0f)) *
        Matrix.CreateScale(tarScale, tarScale, 1f) *
        Matrix.CreateTranslation(new Vector3(viewportWidth * 0.5f, viewportHeight * 0.5f, 0f));
    }
}