using System;
using System.Linq;
using System.Reflection;

namespace Luminix;

public sealed class Universe : Workspace.Service
{
    private readonly Defs defs;

    internal readonly struct Controller 
    {
        internal Controller(Universe controlled)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a Universe.Controller, the provided universe is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly Universe controlled;

        public uint TickRate { get => controlled.TickRate; set => controlled.TickRate = value; }
        public long TickCount { get => controlled.TickCount; set => controlled.TickCount = value; }
        public double TickTime { get => controlled.TickTime; set => controlled.TickTime = value; }
        public double TickDeltaTime { get => controlled.TickDeltaTime; set => controlled.TickDeltaTime = value; }
        public long TickAllocated { get => controlled.TickAllocated; set => controlled.TickAllocated = value; }

        public readonly void StartCreatedEntities()
        {
            
        }

        public readonly void UpdateEntities()
        {
            
        }

        public readonly void RenderEntities()
        {
            
        }

        public readonly void DestroyEntities()
        {
            
        }
    }

    internal Universe(out Controller controller)
    {
        controller = new Controller(this);
        this.controllerIssued = true;
    }

    private readonly bool controllerIssued;

    //Global Variables
    public uint TickRate { get; set; }
    public long TickCount { get; private set; }
    public double TickTime { get; private set; }
    public double TickDeltaTime { get; private set; }
    public long TickAllocated { get; private set; }

    private int lastEntityID;

    protected override void Start()
    {
        this.lastEntityID = 0;

    }

    public ID CreateEntity(EntityType entityType)
    {
    }

    public void DestroyEntity(ID entityID)
    {
    }

    public bool EntityExists(ID entityID)
    {
    }

    public bool EntityHasComponent<T>(ID entityID) where T : struct
    {
    }
    public ref T GetEntityComponent<T>(ID entityID) where T : struct
    {
    }

}