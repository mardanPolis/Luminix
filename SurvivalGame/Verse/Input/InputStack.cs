using System;

namespace SurvivalGame.Verse.Input;

internal class InputStack
{
    public struct PackedInput
    {
        public short Data;

        public PackedInput(char key, bool down, bool hold, bool up)
        {
            // Pack: [Up Bit][Hold Bit][Down Bit][8-bit Key Code]
            int d = (down ? 1 : 0) << 8;
            int h = (hold ? 1 : 0) << 9;
            int u = (up ? 1 : 0) << 10;
            Data = (short)((byte)key | d | h | u);
        }

        public char Key => (char)(Data & 0xFF);
        public bool IsDown => (Data & (1 << 8)) != 0;
        public bool IsHold => (Data & (1 << 9)) != 0;
        public bool IsUp   => (Data & (1 << 10)) != 0;
    }
}
