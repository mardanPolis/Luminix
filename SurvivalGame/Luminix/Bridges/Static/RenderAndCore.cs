using Luminix.Bridges;
using Luminix.Rendering;

namespace Luminix.Bridges;

internal static class CoreToRender
{
    private static RenderBridge renderBridge;
    public static RenderBridge RenderBridge => renderBridge;
    public static void Initialize()
    {
        renderBridge = new RenderBridge();
    }
}

internal static class RenderToCore
{
    internal static volatile bool WindowIsActive = false;

    private static InputBridge inputBridge;
    private static ResourceBridge resourceBridge;

    public static InputBridge InputBridge => inputBridge;
    public static ResourceBridge ResourceBridge => resourceBridge;

    public static void Initialize()
    {
        inputBridge = new InputBridge();
        resourceBridge = new ResourceBridge();
    }
}