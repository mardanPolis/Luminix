using System;

namespace Verse.Rendering;

public interface IRenderable<TRenderData> where TRenderData : struct
{
    void GetRenderData(ref TRenderData renderData);
}
