using System;
using Luminix.Rendering;

namespace Luminix.Bridges;

internal class RenderBridge
{
    private volatile RenderBuffer writeStack;
    private volatile RenderBuffer currReadStack;
    private volatile RenderBuffer prevReadStack;

    private volatile bool pendingStack = false;

    public RenderBuffer CurrentWriteStack => writeStack;
    public bool PendingStack => pendingStack;

    public RenderBridge()
    {
        writeStack = new RenderBuffer(new Identifiers.ID(1));
        currReadStack = new RenderBuffer(new Identifiers.ID(2));
        prevReadStack = new RenderBuffer(new Identifiers.ID(3));
    }

    public void Swap()
    {
        var temp = currReadStack;
        currReadStack = writeStack;
        writeStack = prevReadStack;
        prevReadStack = temp;

        pendingStack = true;
    }

    public RenderBuffer Read()
    {
        pendingStack = false;
        return currReadStack;
    }
}
