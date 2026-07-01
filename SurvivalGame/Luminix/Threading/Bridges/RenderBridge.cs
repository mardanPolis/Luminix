using Luminix.Threading.Buffers;

namespace Luminix.Threading.Bridges;

internal interface IRenderBridge
{
    BaseRenderBuffer WriteBuffer { get; }
    BaseRenderBuffer ReadBuffer { get; }

    BaseRenderBuffer CreateBuffer();
    void SwapBuffers();
}

internal sealed class RenderBridge<T> : IRenderBridge where T : BaseRenderBuffer, new()
{
    private volatile T writeBuffer = null;
    private volatile T currReadBuffer = null;
    private volatile T prevReadBuffer = null;

    public BaseRenderBuffer WriteBuffer => writeBuffer;
    public BaseRenderBuffer ReadBuffer => currReadBuffer;
    public T CurrentWriteBuffer => writeBuffer;
    public T CurrentReadBuffer => currReadBuffer;

    public BaseRenderBuffer CreateBuffer()
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