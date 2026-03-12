using System;

namespace Verse.Rendering;

public interface IRenderable<TRenderData> where TRenderData : struct
{
    sealed Type GetRenderDataType() { return typeof(TRenderData); }
    void GetRenderData(ref TRenderData renderData);
}
