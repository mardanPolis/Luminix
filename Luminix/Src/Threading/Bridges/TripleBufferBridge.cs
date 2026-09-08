using System;

namespace Luminix.Threading.Bridges;

internal sealed class TripleBufferBridge<TBuffer> where TBuffer : class, new()
{
    private TBuffer buffer1;
    private TBuffer buffer2;
    private TBuffer buffer3;

    public TBuffer WriteBuffer => buffer1;
    public TBuffer ReadBuffer => buffer2;

    public void Populate()
    {
        buffer1 = new TBuffer();
        buffer2 = new TBuffer();
        buffer3 = new TBuffer();
    }

    public TBuffer GetNewBuffer()
    {
        if (buffer1 != null)
            throw new InvalidOperationException("All buffer slots are already populated. SwapAndCreateBuffer should only be called during initial setup. Use WriteBuffer/Swap for steady-state operation.");

        buffer1 = new TBuffer();
        return buffer1;
    }

    public void Swap()
    {
        var temp = buffer1;
        buffer1 = buffer3;
        buffer3 = buffer2;
        buffer2 = temp;
    }
}