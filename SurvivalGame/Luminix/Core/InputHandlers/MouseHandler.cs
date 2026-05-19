using System;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework.Input;

namespace Luminix.Core.InputHandlers;

internal class MouseHandler
{
    public int mousePosition = 0; // X, Y
    public byte mouseButtonStates = 0; 
    public long mouseScrollPositions = 0; // vX, vY, hX, hY

    public void Update()
    {
        var mouseState = Mouse.GetState();
        MousePosition(ref mouseState);
        MouseButtons(ref mouseState);
    }

    public void MousePosition(ref MouseState mouseState)
    {
        short mouseX = (short)mouseState.X;
        short mouseY = (short)mouseState.Y;

        mousePosition = (mouseX << 16) | (int)mouseY;
    }

    public void MouseButtons(ref MouseState mouseState)
    {
        byte currMouseButtonStates = 0;

        if (mouseState.LeftButton == ButtonState.Pressed) currMouseButtonStates = 1;
        if (mouseState.RightButton == ButtonState.Pressed) currMouseButtonStates |= 2;
        if (mouseState.MiddleButton == ButtonState.Pressed) currMouseButtonStates |= 4; 
        if (mouseState.XButton1 == ButtonState.Pressed) currMouseButtonStates |= 8;
        if (mouseState.XButton2 == ButtonState.Pressed) currMouseButtonStates |= 16;

        int changedButtons = currMouseButtonStates ^ mouseButtonStates;

        if (changedButtons != 0)
        {
            short X = (short)mouseState.X;
            short Y = (short)mouseState.Y;

            for (byte i = 0; i < 5; i++)
            {
                int mask = 1 << i;
                if ((changedButtons & mask) != 0)
                {
                    bool isDown = (currMouseButtonStates & mask) == 0;
                    DispatchMouseEvent(i, X, Y, isDown);
                }
            }
        }

        mouseButtonStates = currMouseButtonStates;
    }

    private void DispatchMouseEvent(byte button, short X, short Y, bool down)
    {
        if(down)
            RenderToCore.InputBridge.PressButtonEvent(button, X, Y);
        else
            RenderToCore.InputBridge.ReleaseButtonEvent(button, X, Y);
    }
}
