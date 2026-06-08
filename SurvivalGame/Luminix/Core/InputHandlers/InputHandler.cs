using System;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Core.InputHandlers;

internal class InputHandler
{
    internal InputHandler(InputBridge[] inputBridges)
    {
        this.keyboardHandler = new KeyboardHandler();
        this.mouseHandler = new MouseHandler();

        this.inputBridges = inputBridges;
    }

    private KeyboardHandler keyboardHandler;
    private MouseHandler mouseHandler;

    private InputBridge[] inputBridges;

    public void Update(bool WindowActive)
    {
        if(WindowActive)
        {
            keyboardHandler.Update();
            mouseHandler.Update();

            for(int i = 0; i < inputBridges.Length; i++)
                inputBridges[i].WriteBuffer.Push(keyboardHandler.keyboardStates, mouseHandler.mousePosition, mouseHandler.mouseButtonStates);
        }
        else
        {
            Array.Clear(keyboardHandler.keyboardStates, 0, 4);

            MouseState mouseState = Mouse.GetState();
            mouseHandler.MousePosition(ref mouseState);
            for(int i = 0; i < inputBridges.Length; i++)
                inputBridges[i].WriteBuffer.Push(keyboardHandler.keyboardStates, mouseHandler.mousePosition, 0);
        }
    }
}
