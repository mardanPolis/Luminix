using System;

namespace SurvivalGame.Engine.Rendering;

public interface IRenderable<TRenderData> : IDestroyable
{
    void GetRenderData(ref TRenderData renderData);
}

