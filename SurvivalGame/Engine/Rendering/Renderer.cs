using System;
using System.Collections.Generic;
using System.Linq;

namespace SurvivalGame.Engine.Rendering;

public interface IRenderer
{
    bool UpdateData();
    void RenderData(float partialTicks);
}
public abstract class Renderer<TRenderData> : IRenderer where TRenderData : struct
{
    public Type RenderDataStructType => typeof(TRenderData);

    // Double buffers
    private Dictionary<IRenderable<TRenderData>, TRenderData> writeBuffer = [];
    private Dictionary<IRenderable<TRenderData>, TRenderData> readBuffer = [];

    // -------- CORE THREAD --------
    private readonly Queue<IRenderable<TRenderData>> renderObjects = new();

    public void UpdateObjectData(IRenderable<TRenderData> renderable)
    {
        renderObjects.Enqueue(renderable);
    }

    public bool UpdateData()
    {
        writeBuffer.Clear();
        foreach (var pair in readBuffer)
            writeBuffer[pair.Key] = pair.Value;
            
        while (renderObjects.Count > 0)
        {
            var renderable = renderObjects.Dequeue();

            if (renderable.IsDestroyed())
            {
                writeBuffer.Remove(renderable);
                continue;
            }

            if (!writeBuffer.TryGetValue(renderable, out var data))
                data = default;

            renderable.GetRenderData(ref data);
            writeBuffer[renderable] = data;
        }

        var oldRead = readBuffer;
        readBuffer = writeBuffer;
        writeBuffer = oldRead;

        return true;
    }

    // -------- RENDER THREAD --------

    public void RenderData(float partialTicks)
    {
        foreach (var data in readBuffer.Values)
            Render(partialTicks, data);
    }

    public abstract void Render(float partialTicks, TRenderData renderData);
}