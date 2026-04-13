using System;
using Microsoft.Xna.Framework;
using Verse.Simulation;
using Verse.Rendering.RenderItems;
using Verse.Identifiers;

namespace Verse.Rendering;

public sealed class RenderStack(ID iD)
{
    internal record struct CameraData(Vector2 Position0, Vector2 Position, float OrthographicSize0, float OrthographicSize)
    {
        public Matrix GetViewMatrix(float t, float viewportHeight, float viewportWidth)
        {
            var tarPos = Position0 + (Position - Position0) * t;
            var tarOrthoSize = OrthographicSize0 + (OrthographicSize - OrthographicSize0) * t;

            var tarScale = viewportHeight / tarOrthoSize;

            return Matrix.CreateTranslation(new Vector3(-tarPos.X, tarPos.Y, 0f)) *
            Matrix.CreateScale(tarScale, tarScale, 1f) *
            Matrix.CreateTranslation(new Vector3(viewportWidth * 0.5f, viewportHeight * 0.5f, 0f));
        }
    }
    
    internal class Stack<T>(int size, int minSize = 128) where T : struct
    {
        public T[] DataSet = new T[size];
        public int Count = 0;

        private readonly int[] prevCounts = new int[128];
        int currentIndex = 0;
        int biggestCountIndex = 0;


        public ref T Next()
        {
            if (DataSet.Length < Count + 1)
                Array.Resize(ref DataSet, DataSet.Length * 2);

            return ref DataSet[Count++];
        }
        public void BatchNext(T[] values)
        {
            int desiredSpace = Count + values.Length;
            if(DataSet.Length < desiredSpace)
                Array.Resize(ref DataSet, (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)desiredSpace));

            Array.Copy(values, 0, DataSet, Count, values.Length);
            Count += values.Length;
        }

        public void Optimize()
        {
            int targetSize = DataSet.Length / 2;

            if (targetSize < minSize)
                return;

            if (targetSize < prevCounts[biggestCountIndex] + OPTIMIZING_THRESHOLD)
                return;
            
            if (targetSize < Count + OPTIMIZING_THRESHOLD)
                return;

            DataSet = new T[targetSize];
        }
        public void Reset()
        {
            void GetBiggest(int[] array, out int index)
            {
                index = 0;
                for (int i = 0; i < array.Length; i++)
                    if (array[i] > array[index])
                        index = i;
            }
            
            prevCounts[currentIndex] = Count;
            if (Count > prevCounts[biggestCountIndex])
                biggestCountIndex = currentIndex;
            else if(currentIndex == biggestCountIndex)
                GetBiggest(prevCounts, out biggestCountIndex);

            currentIndex = (currentIndex + 1) & 127;

            Count = 0;
        }
    
    }

    internal const int OPTIMIZATION_DELAY_TICKS = 60;
    internal const int OPTIMIZING_THRESHOLD = 128;

    public readonly ID stackID = iD;

    internal CameraData Camera { get; private set; }
    internal Stack<RenderItemInterpSprite> InterpolatedSpriteRenderItemStack { get; private set; } = new Stack<RenderItemInterpSprite>(1024);
    internal Stack<RenderItemSprite> SpriteRenderItemStack { get; private set; } = new Stack<RenderItemSprite>(1024);
    internal Stack<RenderItemLine> LineRenderItemStack { get; private set; } = new Stack<RenderItemLine>(512);
    internal Stack<RenderItemText> TextRenderItemStack { get; private set; } = new Stack<RenderItemText>(128);

    internal Stack<RenderItemSpriteBatch> SpriteBatchRenderItemStack { get; private set; } = new Stack<RenderItemSpriteBatch>(32, 32);
    internal Stack<Vector3> BatchPositions { get; private set; } = new Stack<Vector3>(128);

    internal void PostCameraData(Camera camera)
    {
        if(camera == null)
            return;

        Camera = new CameraData(
            camera.Position, camera.TPosition,
            camera.OrthographicSize, camera.TOrthographicSize
        );
    }

    public void PostSpriteBatch(Sprite sprite, Vector3[] positions, Vector2 scale, float layerDepth = 0)
    {
        ref var renderItem = ref SpriteBatchRenderItemStack.Next();
        
        renderItem.Sprite = sprite;
        renderItem.Scale = new Vector2(scale.X * sprite.TextureRegion.WidthCoefficient, scale.Y * sprite.TextureRegion.HeightCoefficient);
        renderItem.StartIndex = BatchPositions.Count;
        renderItem.Length = positions.Length;
        renderItem.LayerDepth = layerDepth;

        BatchPositions.BatchNext(positions);
    }
    public void PostSprite(Sprite sprite, Vector2 position0, Vector2 position, float rotation0, float rotation, Vector2 scale0, Vector2 scale, float layerDepth = 0f)
    {
        ref var renderItem = ref InterpolatedSpriteRenderItemStack.Next();

        renderItem.Sprite = sprite;
        renderItem.Position0 = position0;
        renderItem.Position = position;
        renderItem.Rotation0 = rotation0;
        renderItem.Rotation = rotation;
        renderItem.Scale0 = new Vector2(scale0.X * sprite.TextureRegion.WidthCoefficient, scale0.Y * sprite.TextureRegion.HeightCoefficient);
        renderItem.Scale = new Vector2(scale.X * sprite.TextureRegion.WidthCoefficient, scale.Y * sprite.TextureRegion.HeightCoefficient);
        renderItem.LayerDepth = layerDepth;
    }
    public void PostSprite(Sprite sprite, Vector2 position, float rotation, Vector2 scale, float layerDepth = 0f)
    {
        ref var renderItem = ref SpriteRenderItemStack.Next();

        renderItem.Sprite = sprite;
        renderItem.Position = position;
        renderItem.Rotation = rotation;
        renderItem.Scale = new Vector2(scale.X * sprite.TextureRegion.WidthCoefficient, scale.Y * sprite.TextureRegion.HeightCoefficient);
        renderItem.LayerDepth = layerDepth;
    }
    public void PostLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        ref var renderItem = ref LineRenderItemStack.Next();

        renderItem.Start = start;
        renderItem.End = end;
        renderItem.Color = color;
        renderItem.Thickness = thickness;
    }

    internal void Optimize()
    {
        InterpolatedSpriteRenderItemStack.Optimize();
        SpriteBatchRenderItemStack.Optimize();
        BatchPositions.Optimize();
        SpriteRenderItemStack.Optimize();
        LineRenderItemStack.Optimize();
        TextRenderItemStack.Optimize();
    }
    public void Reset()
    {
        InterpolatedSpriteRenderItemStack.Reset();
        SpriteBatchRenderItemStack.Reset();
        BatchPositions.Reset();
        SpriteRenderItemStack.Reset();
        LineRenderItemStack.Reset();
        TextRenderItemStack.Reset();
    }
}