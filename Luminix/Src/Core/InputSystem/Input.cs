using System;
using Microsoft.Xna.Framework.Input;
using Luminix.Threading.Bridges;
using System.Numerics;
using Luminix.Threading.Buffers;

namespace Luminix;

public sealed class Input : Workspace.Service
{
    internal readonly struct Controller
    {
        internal Controller(Input controlled, DoubleBufferBridge<InputBuffer> inputBridge)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a Input.Controller, the provided input is already initialized!");
            
            this.controlled = controlled;
            this.bridge = inputBridge;
        }

        private readonly Input controlled;
        private readonly DoubleBufferBridge<InputBuffer> bridge;

        internal void UpdateData()
        {
            bridge.ReadBuffer.Read(out long[] _newKeyboardData, out MouseData _mouseData);

            ref MouseData mouseData = ref _mouseData;

            controlled.prevKeyboardData.CopyFrom(controlled.currKeyboardData);
            controlled.currKeyboardData.CopyFrom(_newKeyboardData);

            controlled.prevMouseData = controlled.currMouseData;
            controlled.currMouseData = mouseData;

            controlled.MousePosition = new Vector2(mouseData.Position >> 16, (short)mouseData.Position);

            bridge.ReadBuffer.Clear();

            bridge.Swap();
        }

        internal bool IsMouseOverUI { set { controlled.IsMouseOverUI = value; } }
    }
    public enum MouseButtons
    {
        LeftButton = 0,
        RightButton = 1,
        MiddleButton = 2,
        SideButton1 = 3,
        SideButton2 = 4
    }

    internal readonly record struct KeyboardData()
    {
        public readonly long[] Keys = new long[4];

        public void CopyFrom(KeyboardData keyboardData)
        {
            Array.Copy(keyboardData.Keys, this.Keys, 4);
        }

        public void CopyFrom(long[] keys)
        {
            Array.Copy(keys, this.Keys, 4);
        }
    }
    internal record struct MouseData(byte Buttons = 0, int Position = 0);

    internal Input(DoubleBufferBridge<InputBuffer> inputBridge, out Controller controller)
    {
        this.controllerIssued = false;
        controller = new Controller(this, inputBridge);
        this.controllerIssued = true;
    }

    private readonly bool controllerIssued;

    private KeyboardData prevKeyboardData = new KeyboardData();
    private KeyboardData currKeyboardData = new KeyboardData();
    private MouseData prevMouseData;
    private MouseData currMouseData;

    public Vector2 MousePosition { get; private set; }
    public bool IsMouseOverUI { get; private set; }

    public bool GetKeyDown(Keys keys)
    {
        byte key = (byte)keys;
        int keyHouse = key >> 6;
        long keyRoom  = 1L << (key & 63);

        bool isDownNow = (currKeyboardData.Keys[keyHouse] & keyRoom) != 0;
        bool wasDownBefore = (prevKeyboardData.Keys[keyHouse] & keyRoom) != 0;

        return isDownNow && !wasDownBefore;
    }
    public bool GetKeyUp(Keys keys)
    {
        byte key = (byte)keys;
        int keyHouse = key >> 6;
        long keyRoom  = 1L << (key & 63);

        bool isDownNow = (currKeyboardData.Keys[keyHouse] & keyRoom) != 0;
        bool wasDownBefore = (prevKeyboardData.Keys[keyHouse] & keyRoom) != 0;

        return !isDownNow && wasDownBefore;
    }
    public bool GetKey(Keys keys)
    {
        byte key = (byte)keys;
        return (currKeyboardData.Keys[key >> 6] & (1L << (key & 63))) != 0;
    }

    public bool GetMouseButtonDown(MouseButtons buttons)
    {
        byte button = (byte)buttons;
        int buttonRoom  = 1 << ((byte)button & 7);

        bool isDownNow = (currMouseData.Buttons & buttonRoom) != 0;
        bool wasDownBefore = (prevMouseData.Buttons & buttonRoom) != 0;

        return isDownNow && !wasDownBefore;
    }
    public bool GetMouseButtonUp(MouseButtons buttons)
    {
        byte button = (byte)buttons;
        int buttonRoom  = 1 << ((byte)button & 7);

        bool isDownNow = (currMouseData.Buttons & buttonRoom) != 0;
        bool wasDownBefore = (prevMouseData.Buttons & buttonRoom) != 0;

        return !isDownNow && wasDownBefore;
    }
    public bool GetMouseButton(MouseButtons buttons)
    {
        byte button = (byte)buttons;
        int buttonRoom  = 1 << ((byte)button & 7);
        return (currMouseData.Buttons & buttonRoom) != 0;
    }
}
