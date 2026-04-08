using System;
using System.Threading;

namespace Verse.UserInput;

internal class InputBuffer
{
    private volatile PackedInput[] writeInput = new PackedInput[256];
    private volatile PackedInput[] readInput = new PackedInput[256];

    private int state;

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
        public bool IsHeld => (Data & 0b_0010) != 0;
        public bool IsUp   => (Data & 0b_0100) != 0;
    }

    public void Swap(out PackedInput[] newReadInput)
    {
        //a spin-lock :(
        while (Interlocked.CompareExchange(ref state, 1, 0) != 0)
        {
            Thread.SpinWait(1); 
        }

        try 
        {
            Array.Clear(readInput, 0, readInput.Length);
            
            var temp = writeInput;
            writeInput = readInput;
            readInput = temp;

            newReadInput = temp;
        }
        finally 
        {
            Interlocked.Exchange(ref state, 0);
        }
    }

    public void Write(PackedInput[] packedInputs)
    {
        //a spin-lock :(
        try
        {
            while (Interlocked.CompareExchange(ref state, 1, 0) != 0)
            {
                Thread.SpinWait(1); 
            }

            Array.Copy(packedInputs, writeInput, 256);
        }
        finally
        {
            Interlocked.Exchange(ref state, 0);
        }
    }

}
