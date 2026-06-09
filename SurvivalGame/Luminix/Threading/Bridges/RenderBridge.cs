using Luminix.Threading.Buffers;

namespace Luminix.Threading.Bridges;

internal interface IRenderBridge
{
    RenderBuffer WriteBuffer { get; }
    RenderBuffer ReadBuffer { get; }

    RenderBuffer CreateBuffer();
    void SwapBuffers();
}

internal sealed class RenderBridge<T> : IRenderBridge where T : RenderBuffer, new()
{
    private volatile T writeBuffer = null;
    private volatile T currReadBuffer = null;
    private volatile T prevReadBuffer = null;

    public RenderBuffer WriteBuffer => writeBuffer;
    public RenderBuffer ReadBuffer => currReadBuffer;
    public T CurrentWriteBuffer => writeBuffer;
    public T CurrentReadBuffer => currReadBuffer;

    public RenderBuffer CreateBuffer()
    {
        writeBuffer = new T();
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