using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SurvivalGame.Core.EntitySystem;

namespace SurvivalGame.Core.Rendering;

public abstract class EntityRenderer
{
    protected Entity.Type entityType { private set; get; }
    internal void SetTarget(Entity.Type type)
    {
        entityType = type;
    }
    public abstract void RenderAll();
}

public abstract class DynamicEntityRenderer<TEntityRenderData> : EntityRenderer
{
    public List<TEntityRenderData> renderData = new();
    public void UpdateData()
    {
        
    }
    public override void RenderAll()
    {
        
    }
    public abstract void Render(TEntityRenderData renderData);
}

public class StaticEntityRenderer : EntityRenderer
{
    public override void RenderAll()
    {
        
    }
    public virtual void Render()
    {
        
    }
}