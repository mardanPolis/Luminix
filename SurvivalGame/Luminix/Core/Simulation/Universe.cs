using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Game;

namespace Luminix;

public sealed class Universe : Workspace.Service
{
    internal readonly struct Controller
    {
        public Controller(Universe universe)
        {
            if(universe.initialized)
                throw new ArgumentException("Cant create a Universe.Controller, the provided universe is already initialized!");
            
            this.controlledUniverse = universe;
        }

        private readonly Universe controlledUniverse;

        public uint TickRate { get => controlledUniverse.TickRate; set => controlledUniverse.TickRate = value; }
        public long TickCount { get => controlledUniverse.TickCount; set => controlledUniverse.TickCount = value; }
        public double TickTime { get => controlledUniverse.TickTime; set => controlledUniverse.TickTime = value; }
        public double TickDeltaTime { get => controlledUniverse.TickDeltaTime; set => controlledUniverse.TickDeltaTime = value; }
        public long TickAllocated { get => controlledUniverse.TickAllocated; set => controlledUniverse.TickAllocated = value; }
    }

    internal Universe(out Controller controller)
    {
        this.logger = Logger.Get();
        this.initialized = false;
        controller = new Controller(this);

        this.initialized = true;

        this.worlds = new Dictionary<string, Type>();
        this.worlds.Add(typeof(SimWorld).Name, typeof(SimWorld));
    }

    private readonly bool initialized;
    private readonly Logger logger;

    private Dictionary<string, Type> worlds;

    //Global Variables
    public uint TickRate { get; private set; }
    public long TickCount { get; private set; }
    public double TickTime { get; private set; }
    public double TickDeltaTime { get; private set; }
    public long TickAllocated { get; private set; }

    public World World { get; private set; }

    //Renderers


    //World

    public World LoadWorld(string worldName)
    {
        if(worlds.TryGetValue(worldName, out Type world))
        {
            World = (World)Activator.CreateInstance(world);
            return World;
        }
        return null;
    }
}