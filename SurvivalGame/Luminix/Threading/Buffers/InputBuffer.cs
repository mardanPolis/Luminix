using System;

namespace Luminix.Threading.Buffers;

internal sealed class InputBuffer
{
    private long[] keys = new long[4];
    private byte mouseButton = 0;
    private int mousePosition = 0;

    public void Read(out long[] _keyboardData, out Input.MouseData _mouseData)
    {
        _keyboardData = keys;
        _mouseData.buttons = mouseButton;
        _mouseData.position = mousePosition;
    }

    public void Clear()
    {
        Array.Clear(keys);
        mouseButton = 0;
        mousePosition = 0;
    }

    public void Push(long[] packedInputs, int mousePosition, byte mouseButtonStates)
    {
        for(int i = 0; i < 4; i++)
            keys[i] |= packedInputs[i];

        this.mousePosition = this.mousePosition | mousePosition;
        this.mouseButton = (byte)(this.mouseButton | mouseButtonStates);
    }
}
