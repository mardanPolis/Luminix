using Luminix.Threading.Buffers;

namespace Luminix.Threading.Bridges;

internal class RenderBridge
{
    private volatile RenderBuffer writeBuffer = null;
    private volatile RenderBuffer currReadBuffer = null;
    private volatile RenderBuffer prevReadBuffer = null;

    private volatile bool pendingStack = false;

    public RenderBuffer CurrentWriteBuffer => writeBuffer;
    public bool PendingBuffer => pendingStack;

    public RenderBuffer CreateBuffer()
    {
        writeBuffer = new RenderBuffer();
        return writeBuffer;
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
