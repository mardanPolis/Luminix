using System;
using System.Collections.Generic;

namespace SurvivalGame.Verse.Input;

internal class InputStack
{
    private PackedInput[] input = new PackedInput[byte.MaxValue + 1];

    public struct PackedInput
    {
        public byte Data;

        public PackedInput()
        {
            Data = 0;
        }

        public PackedInput(byte down, byte hold, byte up, PackedInput prevInput)
        {
            Data = (byte)(down | (hold << 1) | (up << 2) | prevInput.Data);
        }
        public bool IsDown => (Data & 0b_0001) != 0;
        public bool IsHold => (Data & 0b_0010) != 0;
        public bool IsUp   => (Data & 0b_0100) != 0;
    }

    public void Clear()
    {
        for(int i = 0; i < input.Length; i++)
            input[i].Data = 0;
    }

    public void KeyData(byte key, out bool isDown, out bool isHeld, out bool isUp)
    {
        ref var keyData = ref input[key];
        isDown = keyData.IsDown;
        isHeld = keyData.IsHold;
        isUp = keyData.IsUp;
    }

    public void Push(byte key, byte isDown, byte isHeld, byte isUp)
    {
        input[key] = new PackedInput(isDown, isHeld, isUp, input[key]);
    }
}
