using System;
using System.Threading;
using Microsoft.Xna.Framework.Input;
using Verse.UserInput;

namespace Verse;

public sealed class Input
{
    private static ThreadLocal<Input> instance = new ThreadLocal<Input>(true);

    internal static void Initialize()
    {
        instance.Value = new Input();
    }

    public static Input Get()
    {
        return instance.Value;
    }

    public static bool GetKeyDown(Keys keys)
    {
        return Get().IsKeyDown(keys);
    }

    public static bool GetKey(Keys keys)
    {
        return Get().IsKeyHeld(keys);
    }

    public static bool GetKeyUp(Keys keys)
    {
        return Get().IsKeyUp(keys);
    }

    private InputBuffer.PackedInput[] input = new InputBuffer.PackedInput[256];

    internal void Update(InputBuffer.PackedInput[] packedInputs)
    {
        
    }

    public bool IsKeyDown(Keys keys)
    {
        return input[(byte)keys].IsDown;
    }

    public bool IsKeyHeld(Keys keys)
    {
        return input[(byte)keys].IsHeld;
    }

    public bool IsKeyUp(Keys keys)
    {
        return input[(byte)keys].IsUp;
    }
}
