using System;

namespace Verse.Rendering;

public abstract class Renderer
{
    public abstract Type RenderDataType { get; }
    internal abstract void RenderAll(float PartialTick, RenderDataContainer newRenderDataContainer);
}

public abstract class Renderer<TRenderData> : Renderer where TRenderData : struct
{
    private RenderDataContainer renderDataContainer;

    internal sealed override void RenderAll(float PartialTick, RenderDataContainer newRenderDataContainer)
    {
        if (newRenderDataContainer.RenderDataType != RenderDataType)
            throw new ArgumentException($"Invalid new RenderDataContainer type. Expected {RenderDataType}, got {newRenderDataContainer.RenderDataType}");
        
        this.renderDataContainer = newRenderDataContainer;

        RenderAll(PartialTick);
    }

    internal void RenderAll(float PartialTick)
    {
        var dataset = ((RenderDataContainer<TRenderData>) renderDataContainer).GetDataset();
        for (int i = 0; i < dataset.Count; i++)
            Render(PartialTick, dataset[i]);
    }

    public abstract void Render(float PartialTick, TRenderData renderData);
    public override Type RenderDataType => typeof(TRenderData);
}
