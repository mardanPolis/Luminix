using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Threading.Buffers;

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

    internal struct KeyboardData
    {
        public KeyboardData()
        {
            keys = new long[4];
        }

        public readonly long[] keys;

        public void CopyFrom(KeyboardData keyboardData)
        {
            Array.Copy(keyboardData.keys, this.keys, 4);
        }

        public void CopyFrom(long[] keys)
        {
            Array.Copy(keys, this.keys, 4);
        }
    }

    internal struct MouseData()
    {
        public byte buttons = 0;
        public int position = 0;
    }

    private KeyboardData prevKeyboardData = new KeyboardData();
    private KeyboardData currKeyboardData = new KeyboardData();
    private MouseData prevMouseData;
    private MouseData currMouseData;

    internal void ReadFrom(InputBuffer inputBridge)
    {
        inputBridge.Read(out long[] _newKeyboardData, out MouseData _mouseData);

        prevKeyboardData.CopyFrom(currKeyboardData);
        currKeyboardData.CopyFrom(_newKeyboardData);

        prevMouseData = currMouseData;
        currMouseData = _mouseData;

        inputBridge.Clear();
    }

    public bool GetKeyDown(Keys keys)
    {
        byte key = (byte)keys;
        int keyHouse = key >> 6;
        long keyRoom  = 1L << (key & 63);
        return (currKeyboardData.keys[keyHouse] & keyRoom) != (prevKeyboardData.keys[keyHouse] & keyRoom);
    }
    public bool GetKeyUp(Keys keys)
    {
        byte key = (byte)keys;
        int keyHouse = key >> 6;
        long keyRoom  = 1L << (key & 63);
        return (currKeyboardData.keys[keyHouse] & keyRoom) == (prevKeyboardData.keys[keyHouse] & keyRoom);
    }
    public bool GetKey(Keys keys)
    {
        byte key = (byte)keys;
        return (currKeyboardData.keys[key >> 6] & (1L << (key & 63))) != 0;
    }

    public bool GetMouseButtonDown(MouseButtons buttons)
    {
        byte button = (byte)buttons;
        int buttonRoom  = 1 << ((byte)button & 7);
        return (currMouseData.buttons & buttonRoom) != (prevMouseData.buttons & buttonRoom);
    }
    public bool GetMouseButtonUp(MouseButtons buttons)
    {
        byte button = (byte)buttons;
        int buttonRoom  = 1 << ((byte)button & 7);
        return (currMouseData.buttons & buttonRoom) == (prevMouseData.buttons & buttonRoom);
    }
    public bool GetMouseButton(MouseButtons buttons)
    {
        byte button = (byte)buttons;
        int buttonRoom  = 1 << ((byte)button & 7);
        return (currMouseData.buttons & buttonRoom) != 0;
    }

    public short MouseX => (byte)(currMouseData.position >> 16);
    public short MouseY => (byte)(currMouseData.position);
}
