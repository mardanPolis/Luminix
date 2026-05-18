using System;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Bridges;

internal class InputBridge
{
    //keyboard
    private volatile long[] writeKeyBuffer = new long[4];
    private volatile long[] readKeyBuffer = new long[4];

    private ConcurrentQueue<byte> PressedKeys = new ConcurrentQueue<byte>();
    private ConcurrentQueue<byte> ReleasedKeys = new ConcurrentQueue<byte>();
    
    //mouse
    private volatile byte mouseButtonStates = 0;
    private volatile int mousePosition = 0;

    private ConcurrentQueue<(byte, short, short)> pressedButtons = new ConcurrentQueue<(byte, short, short)>();
    private ConcurrentQueue<(byte, short, short)> releasedButtons = new ConcurrentQueue<(byte, short, short)>();

    public struct MouseData
    {
        public byte buttonStates;
        public int position;
        public ConcurrentQueue<(byte, short, short)> pressedButtons;
        public ConcurrentQueue<(byte, short, short)> releasedButtons;
    }

    public void Read(out long[] _readKeyBuffer, out ConcurrentQueue<byte> _pressedKeys, out ConcurrentQueue<byte> _releasedKeys,
    out MouseData _mouseData)
    {
        _readKeyBuffer = readKeyBuffer;
        _pressedKeys = PressedKeys;
        _releasedKeys = ReleasedKeys;

        _mouseData = new()
        {
            buttonStates = mouseButtonStates,
            position = mousePosition,
            pressedButtons = pressedButtons,
            releasedButtons = releasedButtons
        };
    }

    public void Write(long[] packedInputs, int mousePosition, byte mouseButtonStates)
    {
        Array.Copy(packedInputs, writeKeyBuffer, writeKeyBuffer.Length);
        this.mousePosition = mousePosition;
        this.mouseButtonStates = mouseButtonStates;
        (readKeyBuffer, writeKeyBuffer) = (writeKeyBuffer, readKeyBuffer);
    }

    public void PressKeyEvent(Keys key)
    {
        PressedKeys.Enqueue((byte)key);
    }
    public void ReleaseKeyEvent(Keys key)
    {
        ReleasedKeys.Enqueue((byte)key);
    }

    public void PressButtonEvent(byte button, short mouseX, short mouseY)
    {
        pressedButtons.Enqueue((button, mouseX, mouseY));
    }
    public void ReleaseButtonEvent(byte button, short mouseX, short mouseY)
    {
        releasedButtons.Enqueue((button, mouseX, mouseY));
    }

}
