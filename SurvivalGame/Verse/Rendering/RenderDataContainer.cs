using System;
using System.Collections;
using System.Collections.Generic;
using Verse.Collections;

namespace Verse.Rendering;

internal abstract class RenderDataContainer
{
    public RenderDataContainer(ICollection renderables) {}

    public abstract Type RenderDataType { get; }
    public abstract int Count { get; }
    public abstract void UpdateData();
}

internal sealed class RenderDataContainer<TRenderData> : RenderDataContainer where TRenderData : struct
{
    private readonly DenseList<IRenderable<TRenderData>> renderables;
    private readonly List<TRenderData> renderDataset = new List<TRenderData>();

    public RenderDataContainer(ICollection renderables) : base(renderables)
    {
        if (renderables is DenseList<IRenderable<TRenderData>> denseList)
        {
            this.renderables = denseList;
            return;
        }

        throw new ArgumentException($"Expected a DenseList<IRenderable<{typeof(TRenderData).Name}>>, but got {renderables.GetType().Name}.");
    }

    public override Type RenderDataType => typeof(TRenderData);
    public override int Count => renderDataset.Count;
    
    public override void UpdateData()
    {
        renderDataset.Clear();
        foreach (var renderable in renderables)
        {
            TRenderData data = default;
            renderable.GetRenderData(ref data);
            renderDataset.Add(data);
        }
    }
    public List<TRenderData> GetDataset() => renderDataset;
}