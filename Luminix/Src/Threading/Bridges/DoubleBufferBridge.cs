using System;
using System.Threading;

namespace Luminix.Threading.Bridges;

internal sealed class DoubleBufferBridge<TBuffer> where TBuffer : class, new()
{
    private volatile TBuffer buffer1;
    private volatile TBuffer buffer2;

    public TBuffer WriteBuffer => buffer1;
    public TBuffer ReadBuffer => buffer2;

    public void PopulateIfEmpty()
    {
        Interlocked.CompareExchange(ref buffer1, new TBuffer(), null);
        Interlocked.CompareExchange(ref buffer2, new TBuffer(), null);
    }

    public TBuffer GetNewBuffer()
    {
        var newBuf = new TBuffer();
        if (Interlocked.CompareExchange(ref buffer1, newBuf, null) != null)
            throw new InvalidOperationException("All buffer slots are already populated. GetNewBuffer should only be called during initial setup. Use WriteBuffer/Swap for steady-state operation.");
        return newBuf;
    }

    public void Swap()
    {
        (buffer2, buffer1) = (buffer1, buffer2);
    }
}