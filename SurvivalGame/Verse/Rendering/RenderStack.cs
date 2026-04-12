using System;
using Microsoft.Xna.Framework;
using Verse.Simulation;
using Verse.Rendering.RenderInstances;
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
    
    private struct Stack<T>(int size) where T : struct
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

        public void Optimize()
        {
            int targetSize = DataSet.Length / 2;

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

    private CameraData camera;
    private Stack<InstanceInterpolatedSprite> interpolatedSpriteRenderDataStack = new Stack<InstanceInterpolatedSprite>(1024);
    private Stack<InstanceSprite> spriteRenderDataStack = new Stack<InstanceSprite>(1024);
    private Stack<InstanceLine> lineRenderDataStack = new Stack<InstanceLine>(512);
    private Stack<InstanceText> textRenderDataStack = new Stack<InstanceText>(128);

    internal CameraData Camera => camera;
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

        this.camera = new CameraData(
            camera.Position, camera.TPosition,
            camera.OrthographicSize, camera.TOrthographicSize
        );
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
        interpolatedSpriteRenderDataStack.Optimize();
        spriteRenderDataStack.Optimize();
        lineRenderDataStack.Optimize();
        textRenderDataStack.Optimize();
    }
    public void Reset()
    {
        interpolatedSpriteRenderDataStack.Reset();
        spriteRenderDataStack.Reset();
        lineRenderDataStack.Reset();
        textRenderDataStack.Reset();
    }
}