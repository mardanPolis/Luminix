using System;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Core.InputHandlers;

internal class KeyboardHandler
{
    public long[] keyboardStates = new long[4];

    public void Update()
    {
        Array.Clear(keyboardStates);

        var keyboardState = Keyboard.GetState();
        if(keyboardState.GetPressedKeyCount() > 0)
        {
            for (int i = 0; i < 256; i++)
            {
                if (keyboardState.IsKeyDown((Keys)i))
                {
                    keyboardStates[i >> 6] |= 1L << (i & 63);
                }
            }
        }
    }
}
