using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Threading;
using Luminix.Workspaces;

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
    }

    private readonly bool initialized;
    private readonly Logger logger;

    //Global Variables
    public uint TickRate { get; private set; }
    public long TickCount { get; private set; }
    public double DeltaTime { get; private set; }

    public Viewport Viewport { get; private set; } // TODO: create a way to get the viewport over the bridge

    //Renderers


    //Current World
    private World world;
    public World World => world;

    public void NewBlankWorld(string name)
    {
        if(world != null)
        {
            logger.Wrn("Unable to create a blank world!, because a world already exists!");
            return;
        }

        WorldComponent[] worldComponents = [];
        world = new World(name, worldComponents);
    }

    public void LoadWorld(string worldName)
    {
        
    }

    public void SaveWorld()
    {
        
    }

    public void ExitWorld()
    {
        if(world == null)
        {
            logger.Wrn("Unable to exit a world, no world to exit!");
            return;
        }

        world = null;
    }
}