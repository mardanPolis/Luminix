using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Luminix.Bridge;
using Luminix.Workspaces;

namespace Luminix;

public sealed class Input : Workspace.Service
{
    public enum MouseButtons
    {
        LeftButton = 0,
        RightButton = 1,
        MiddleButton = 2,
        SideButton1 = 3,
        SideButton2 = 4
    }

    private Dictionary<Keys, Action[]> keyDownActions = new Dictionary<Keys, Action[]>();
    private Dictionary<Keys, Action[]> keyUpActions = new Dictionary<Keys, Action[]>();

    private Dictionary<byte, Action<short, short>[]> mouseButtonDownActions = new Dictionary<byte, Action<short, short>[]>();
    private Dictionary<byte, Action<short, short>[]> mouseButtonUpActions = new Dictionary<byte, Action<short, short>[]>();

    private long[] keyInput = new long[4];

    private short mouseX = 0;
    private short mouseY = 0;
    private byte mouseButtonStates = 0;

    internal void ReadFrom(InputBridge inputBridge)
    {
        inputBridge.Read(out long[] _readKeyBuffer, out ConcurrentQueue<byte> _pressedKeys, out ConcurrentQueue<byte> _releasedKeys,
        out InputBridge.MouseData mouseData);

        Array.Copy(_readKeyBuffer, keyInput, _readKeyBuffer.Length);
        mouseX = (short)(mouseData.position >> 16);
        mouseY = (short)mouseData.position;
        mouseButtonStates = mouseData.buttonStates;

        while(_pressedKeys.TryDequeue(out byte key))
        {
            if (keyDownActions.TryGetValue((Keys)key, out var actions))
            {
                for (int i = 0; i < actions.Length; i++)
                    actions[i]();
            }
        }

        while(_releasedKeys.TryDequeue(out byte key))
        {
            if (keyUpActions.TryGetValue((Keys)key, out var actions))
            {
                for (int i = 0; i < actions.Length; i++)
                    actions[i]();
            }
        }

        while(mouseData.pressedButtons.TryDequeue(out (byte, short, short) pressEvent))
        {
            if (mouseButtonDownActions.TryGetValue(pressEvent.Item1, out var actions))
            {
                var x = pressEvent.Item2;
                var y = pressEvent.Item3;
                for (int i = 0; i < actions.Length; i++)
                    actions[i](x, y);
            }
        }

        while(mouseData.releasedButtons.TryDequeue(out (byte, short, short) pressEvent))
        {
            if (mouseButtonUpActions.TryGetValue(pressEvent.Item1, out var actions))
            {
                var x = pressEvent.Item2;
                var y = pressEvent.Item3;
                for (int i = 0; i < actions.Length; i++)
                    actions[i](x, y);
            }
        }
    }

    public void Subscribe(Action action, Keys key)
    {
        
    }

    public bool IsKeyDown(Keys keys)
    {
        byte key = (byte)keys;
        return (keyInput[key >> 6] & (1L << (key & 63))) != 0;
    }
    public bool IsKeyUp(Keys keys)
    {
        byte key = (byte)keys;
        return (keyInput[key >> 6] & (1L << (key & 63))) == 0;
    }

    public bool IsMouseButtonDown(MouseButtons button)
    {
        return (mouseButtonStates & (1 << ((byte)button & 7))) != 0;
    }
    public bool IsMouseButtonUp(MouseButtons button)
    {
        return (mouseButtonStates & (1 << ((byte)button & 7))) == 0;
    }

    public short MouseX => mouseX;
    public short MouseY => mouseY;
}
