using Luminix.Threading.Buffers;

namespace Luminix.Threading.Bridges;

internal class InputBridge
{
    private volatile InputBuffer writeBuffer;
    private volatile InputBuffer readBuffer;

    public InputBuffer ReadBuffer => readBuffer;
    public InputBuffer WriteBuffer => writeBuffer;

    public InputBridge()
    {
        writeBuffer = new InputBuffer();
        readBuffer = new InputBuffer();
    }

    public void Swap()
    {
        var temp = readBuffer;
        readBuffer = writeBuffer;
        writeBuffer = temp;
    }
}