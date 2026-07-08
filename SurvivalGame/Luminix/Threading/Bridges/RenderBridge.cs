using Luminix.Threading.Buffers;

namespace Luminix.Threading.Bridges;

internal sealed class RenderBridge
{
    private volatile RenderBuffer writeBuffer = null;
    private volatile RenderBuffer currReadBuffer = null;
    private volatile RenderBuffer prevReadBuffer = null;

    private volatile RenderInfoBuffer infoBufferWrite = null;
    private volatile RenderInfoBuffer infoBufferRead = null;

    public RenderBuffer CurrentWriteBuffer => writeBuffer;
    public RenderBuffer CurrentReadBuffer => currReadBuffer;

    public RenderInfoBuffer CurrentWriteInfoBuffer => infoBufferWrite;
    public RenderInfoBuffer CurrentReadInfoBuffer => infoBufferRead;

    public RenderBuffer CreateBuffer()
    {
        writeBuffer = new RenderBuffer();
        return writeBuffer;
    }

    public RenderInfoBuffer CreateInfoBuffer()
    {
        infoBufferWrite = new RenderInfoBuffer();
        return infoBufferWrite;
    }

    public void SwapBuffers()
    {
        var temp = currReadBuffer;
        currReadBuffer = writeBuffer;
        writeBuffer = prevReadBuffer;
        prevReadBuffer = temp;

        var infoTemp = infoBufferWrite;
        infoBufferWrite = infoBufferRead;
        infoBufferRead = infoTemp;
    }
} 