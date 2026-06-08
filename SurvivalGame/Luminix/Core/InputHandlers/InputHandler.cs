using System;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Core.InputHandlers;

internal class InputHandler
{
    private KeyboardHandler keyboardHandler;
    private MouseHandler mouseHandler;

    internal InputHandler()
    {
        keyboardHandler = new KeyboardHandler();
        mouseHandler = new MouseHandler();
    }

    public void Update(bool WindowActive)
    {
        if(WindowActive)
        {
            keyboardHandler.Update();
            mouseHandler.Update();

            RenderToCore.InputBridge.WriteBuffer.Push(keyboardHandler.keyboardStates, mouseHandler.mousePosition, mouseHandler.mouseButtonStates);
        }
        else
        {
            Array.Clear(keyboardHandler.keyboardStates, 0, 4);

            MouseState mouseState = Mouse.GetState();
            mouseHandler.MousePosition(ref mouseState);
            RenderToCore.InputBridge.WriteBuffer.Push(keyboardHandler.keyboardStates, mouseHandler.mousePosition, 0);
        }
    }
}
