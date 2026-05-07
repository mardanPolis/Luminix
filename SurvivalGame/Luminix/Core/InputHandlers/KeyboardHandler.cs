using System;
using Luminix.Bridges;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Core.InputHandlers;

internal class KeyboardHandler
{
    public long[] prevKeyboardStates = new long[4];
    public long[] currKeyboardStates = new long[4];

    public void Update()
    {
        Array.Copy(currKeyboardStates, prevKeyboardStates, 4);
        Array.Clear(currKeyboardStates, 0, 4);

        var keyboardState = Keyboard.GetState();
        
        if(keyboardState.GetPressedKeyCount() > 0)
        {
            for (int i = 0; i < 256; i++)
            {
                if (keyboardState.IsKeyDown((Keys)i))
                {
                    currKeyboardStates[i >> 6] |= 1L << (i & 63);
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            long current = currKeyboardStates[i];
            long previous = prevKeyboardStates[i];
            
            long changed = current ^ previous;
            if (changed == 0) continue;

            long pressed = changed & current;
            long released = changed & previous;

            if (pressed != 0) DispatchEvents(i, pressed, true);
            if (released != 0) DispatchEvents(i, released, false);
        }
    }

    private void DispatchEvents(int chunkIndex, long mask, bool isDown)
    {
        int offset = chunkIndex << 6;
        for (int bit = 0; bit < 64; bit++)
        {
            if ((mask & (1L << bit)) != 0)
            {
                Keys key = (Keys)(offset | bit);
                if (isDown) RenderToCore.InputBridge.PressKeyEvent(key);
                else RenderToCore.InputBridge.ReleaseKeyEvent(key);
            }
        }
    }
}
