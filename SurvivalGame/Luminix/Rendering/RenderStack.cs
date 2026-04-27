using System;
using Microsoft.Xna.Framework;
using Luminix.Simulation;
using Luminix.Identifiers;
using System.Collections.Generic;

namespace Luminix.Rendering;

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
    internal class DynamicSet<T>(int size, int minSize) where T : struct
    {
        internal const int OPTIMIZATION_DELAY_TICKS = 60;
        internal const int OPTIMIZING_THRESHOLD = 128;

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
   
    public readonly ID stackID = iD;

    internal CameraData Camera { get; private set; }

    internal void PostCameraData(Camera camera)
    {
        if(camera == null)
            return;

        Camera = new CameraData(
            camera.Position, camera.TPosition,
            camera.OrthographicSize, camera.TOrthographicSize
        );
    }

    //----------------------> TILE MAP <----------------------\\
    private record struct TileMapData(int StartIndex, int Length);
    private static Dictionary<string, int> tileMapIndexes = new Dictionary<string, int>();
    private static TileMapData[] tileMapDataSet = [];
    private static Vector3[] tilePositions = [];
    private Vector2[] tileSourceRect = [];

    internal static void AllocateTileMap(string mapName, Vector3[] positions)
    {
        int mapIndex = tileMapDataSet.Length;
        if(tileMapIndexes.TryAdd(mapName, mapIndex))
        {
            var newTileMapDataSet = new TileMapData[tileMapDataSet.Length + 1];
            Array.Copy(tileMapDataSet, newTileMapDataSet, tileMapDataSet.Length);
            newTileMapDataSet[mapIndex] = new TileMapData(tilePositions.Length, positions.Length);
            tileMapDataSet = newTileMapDataSet;

            var newTilePositions = new Vector3[tilePositions.Length + positions.Length];
            Array.Copy(tilePositions, newTilePositions, tilePositions.Length);
            Array.Copy(positions, 0, newTilePositions, newTileMapDataSet[mapIndex].StartIndex, positions.Length);
            tilePositions = newTilePositions;

            return;
        }

        Logger.Get().Log("Unable to allocate on RenderStack's a new TileMap");
    }

    internal void PostTileDataSet(string mapName, Vector2[] tileSourceRectangles)
    {
        if(tileSourceRect.Length != tilePositions.Length)
            tileSourceRect = new Vector2[tilePositions.Length];

        if(!tileMapIndexes.TryGetValue(mapName, out int index))
            throw new KeyNotFoundException($"No Allocated TileMap found: '{mapName}'; Allocate a TileMap with its positions first!");

        var data = tileMapDataSet[index];

        if(data.Length != tileSourceRectangles.Length)
            throw new ArgumentException($"Expected Tile data of length: '{data.Length}', but received Tile data of length: '{tileSourceRectangles.Length}'");
        
        Array.Copy(tileSourceRectangles, 0, tileSourceRect, data.StartIndex, data.Length);
    }

    //TO DO: implement ToggleTileMap method.


    // internal void Optimize()
    // {
    //     SpriteBatchRenderItemStack.Optimize();
    //     BatchPositions.Optimize();
    // }
    // internal void Reset()
    // {
    //     SpriteBatchRenderItemStack.Reset();
    //     BatchPositions.Reset();
    // }
    internal void Clear()
    {
        
    }
}

