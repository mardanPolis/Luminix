using Luminix;

namespace Luminix.Rendering;

public struct RenderSettings
{
    public Graphics.Resolution Resolution { get; set; }
    public int FrameRate { get; set; }
    public bool VSync { get; set; }
}
  