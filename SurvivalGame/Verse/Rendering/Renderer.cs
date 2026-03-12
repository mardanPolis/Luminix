using System;
using Verse.Identifiers;

namespace Verse.Rendering;

public abstract class Renderer
{
    public abstract Type RenderDataType { get; }
    public ID ID { get; private set; }
    public void Assign(ID ID)
    {
        this.ID = ID;
    }
    internal abstract void SetContainers(RenderDataContainer[] newRenderDataContainer);
}

public abstract class Renderer<TRenderData> : Renderer where TRenderData : struct
{
    private RenderDataContainer<TRenderData>[] renderDataContainers;

    internal sealed override void SetContainers(RenderDataContainer[] newRenderDataContainers)
    {
        if(newRenderDataContainers is RenderDataContainer<TRenderData>[] renderDataContainers)
        {
            this.renderDataContainers = renderDataContainers;
        }
        else
        {
            foreach(var container in newRenderDataContainers)
            {
                if(container.RenderDataType != RenderDataType)
                    throw new ArgumentException($"Invalid RenderDataContainer type. Expected {RenderDataType}, got {container.RenderDataType}");
            }
        }
    }

    internal void RenderAll(float PartialTick)
    {
        var dataset = renderDataContainer.ToList();
        for (int i = 0; i < dataset.Count; i++)
            Render(PartialTick, dataset[i]);
    }

    public abstract void Render(float PartialTick, TRenderData renderData);
    public override Type RenderDataType => typeof(TRenderData);
}
