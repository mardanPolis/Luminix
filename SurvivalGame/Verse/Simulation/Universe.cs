using System;
using System.Collections.Generic;
using Verse.Threading;

namespace Verse.Simulation;

public class Universe
{
    private static World world = null;
    public static ref readonly World World => ref world;

    internal static void NewComponent(WorldComponent component)
    {
        
    }

    internal static void NewWorld(string name, int width, int height)
    {
        
    }
}