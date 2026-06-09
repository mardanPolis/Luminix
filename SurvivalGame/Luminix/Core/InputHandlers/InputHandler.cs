using System;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Core.InputHandlers;

internal class InputHandler
{
    internal InputHandler(InputBridge inputBridge)
    {
        this.keyboardHandler = new KeyboardHandler();
        this.mouseHandler = new MouseHandler();

        this.inputBridge = inputBridge;
    }

    private KeyboardHandler keyboardHandler;
    private MouseHandler mouseHandler;

    private InputBridge inputBridge;

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
