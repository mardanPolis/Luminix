using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading;
using Game.Simulation;

namespace Luminix.Simulation;

public sealed class Universe : Workspace.Service
{
    public readonly struct Controller
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
        public double DeltaTime { get => controlledUniverse.DeltaTime; set => controlledUniverse.DeltaTime = value; }

        public Viewport Viewport { get => controlledUniverse.Viewport; set => controlledUniverse.Viewport = value; }
    }

    public Universe(out Controller controller)
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
    public double DeltaTime { get; private set; }

    public Viewport Viewport { get; private set; } // TODO: create a way to get the viewport over the bridge

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

    public void SaveWorld()
    {
        
    }
}