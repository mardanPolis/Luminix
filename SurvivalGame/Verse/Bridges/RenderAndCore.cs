using SurvivalGame.Verse.Input;
using Verse.Rendering;

namespace Verse.Bridges;

internal static class CoreToRender
{
    private static RenderStack writeStack;
    private static volatile RenderStack currReadStack;
    private static RenderStack prevReadStack;

    private static volatile bool pendingStack = false;

    public static RenderStack CurrentWriteStack => writeStack;
    public static bool PendingStack => pendingStack;

    public static void Initialize()
    {
        RenderStack.StackSharedData sharedDataInterpSprite = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataSprite = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataLine = new RenderStack.StackSharedData();
        RenderStack.StackSharedData sharedDataText = new RenderStack.StackSharedData();

        writeStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine, sharedDataText);
        currReadStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine, sharedDataText);
        prevReadStack = new RenderStack(sharedDataInterpSprite, sharedDataSprite, sharedDataLine, sharedDataText);
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
    private static InputStack inputStack;
    public static InputStack communicationInputStack => inputStack;
}