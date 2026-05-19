using Luminix.Bridges;
using Luminix.Rendering;

namespace Luminix.Threading.Bridges;

internal static class CoreToRender
{
    private static RenderBridge renderBridge;
    private static AssetBridge assetBridge;

    public static RenderBridge RenderBridge => renderBridge;
    public static AssetBridge AssetBridge => assetBridge;
    public static void Initialize()
    {
        renderBridge = new RenderBridge();
        assetBridge = new AssetBridge();
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