using System;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using Microsoft.Xna.Framework.Input;

namespace Luminix.InputSystem.Handlers;

internal sealed class InputHandler
{
    internal InputHandler(DoubleBufferBridge<InputBuffer> inputBridge)
    {
        this.keyboardHandler = new KeyboardHandler();
        this.mouseHandler = new MouseHandler();

        this.inputBridge = inputBridge;
    }

    private KeyboardHandler keyboardHandler;
    private MouseHandler mouseHandler;

    private DoubleBufferBridge<InputBuffer> inputBridge;

    public void Update(bool WindowActive)
    {
        if(WindowActive)
        {
            keyboardHandler.Update();
            mouseHandler.Update();

            inputBridge.WriteBuffer.Push(keyboardHandler.keyboardStates, mouseHandler.mousePosition, mouseHandler.mouseButtonStates);
        }
        else
        {
            Array.Clear(keyboardHandler.keyboardStates, 0, 4);

            MouseState mouseState = Mouse.GetState();
            mouseHandler.MousePosition(ref mouseState);
            inputBridge.WriteBuffer.Push(keyboardHandler.keyboardStates, mouseHandler.mousePosition, 0);
        }
    }
}
