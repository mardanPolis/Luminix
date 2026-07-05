using Luminix.Threading.Buffers;

namespace Luminix.Threading.Bridges;

internal sealed class RenderBridge
{
    private volatile RenderBuffer writeBuffer = null;
    private volatile RenderBuffer currReadBuffer = null;
    private volatile RenderBuffer prevReadBuffer = null;

    public RenderBuffer CurrentWriteBuffer => writeBuffer;
    public RenderBuffer CurrentReadBuffer => currReadBuffer;

    public RenderBuffer CreateBuffer()
    {
        writeBuffer = new RenderBuffer();
        return writeBuffer;
    }

    public void SwapBuffers()
    {
        var temp = currReadBuffer;
        currReadBuffer = writeBuffer;
        writeBuffer = prevReadBuffer;
        prevReadBuffer = temp;
    }
} 