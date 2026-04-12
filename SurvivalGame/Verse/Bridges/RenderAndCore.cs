using Verse.Bridge;
using Verse.Rendering;

namespace Verse.Bridges;

internal static class CoreToRender
{
    private static volatile RenderStack writeStack;
    private static volatile RenderStack currReadStack;
    private static volatile RenderStack prevReadStack;

    private static volatile bool pendingStack = false;

    public static RenderStack CurrentWriteStack => writeStack;
    public static bool PendingStack => pendingStack;

    public static void Initialize()
    {
        RenderStack.StackSharedData sharedDataInterpSprite = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataSprite = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataLine = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataText = new RenderStack.StackSharedData();

        writeStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine, sharedDataText, new Identifiers.ID(1));
        currReadStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine, sharedDataText, new Identifiers.ID(2));
        prevReadStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine, sharedDataText, new Identifiers.ID(3));
    }

    public static void Swap()
    {
        var temp = currReadStack;
        currReadStack = writeStack;
        writeStack = prevReadStack;
        prevReadStack = temp;

        pendingStack = true;
    }

    public static RenderStack Read()
    {
        pendingStack = false;
        return currReadStack;
    }
}

internal static class RenderToCore
{
    internal static volatile bool WindowIsActive = false;
    private readonly static InputBridge inputBridge = new InputBridge();
    public static InputBridge InputBridge => inputBridge;
}