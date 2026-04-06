using System;
using System.Collections.Generic;
using Verse.Rendering;
using Verse.Threads;

namespace Verse.Simulation;

public class World
{
    private static List<WorldComponent> worldComponents = new List<WorldComponent>();
    private static HashSet<Type> initializedComponentTypes = new HashSet<Type>();
    private static CoreThread coreThread;
    
    public static IReadOnlyList<WorldComponent> WorldComponents => worldComponents;

    public static uint TickRate
    {
        set { coreThread.TickRate = value; }
        get { return coreThread.TickRate; }
    }

    public static long TickCount => coreThread.TickCount;

    public static double TickDeltaTime => coreThread.TickDeltaTimeSec;

    public static float PartialTick => coreThread.PartialTick;

    internal static void Initialize(CoreThread coreThread)
    {
        World.coreThread = coreThread;
    }
    internal static void NewWorldComponent(WorldComponent component)
    {
        if(initializedComponentTypes.Contains(component.GetType()))
        {
            Console.WriteLine($"Error: World component of type {component.GetType()} already exists. Cannot add multiple components of the same type.");
            return;
        }

        worldComponents.Add(component);
        initializedComponentTypes.Add(component.GetType());
    }

}
