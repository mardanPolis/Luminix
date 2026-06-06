using Luminix.Rendering;

namespace Luminix.Threading.Bridges;

internal class RenderBridge
{
    private volatile RenderBuffer writeBuffer;
    private volatile RenderBuffer currReadBuffer;
    private volatile RenderBuffer prevReadBuffer;

    private volatile bool pendingStack = false;

    public RenderBuffer CurrentWriteBuffer => writeBuffer;
    public bool PendingBuffer => pendingStack;

    public RenderBridge()
    {
        writeBuffer = new RenderBuffer(new Identifiers.ID(1));
        currReadBuffer = new RenderBuffer(new Identifiers.ID(2));
        prevReadBuffer = new RenderBuffer(new Identifiers.ID(3));
    }

    public void Swap()
    {
        var temp = currReadBuffer;
        currReadBuffer = writeBuffer;
        writeBuffer = prevReadBuffer;
        prevReadBuffer = temp;

        pendingStack = true;
    }

    public RenderBuffer Read()
    {
        pendingStack = false;
        return currReadBuffer;
    }
}
