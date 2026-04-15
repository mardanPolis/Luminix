using System;
using System.Collections.Generic;
using System.Threading;
using Verse.Threading;

namespace Verse.Workspaces;

public sealed class Workspace
{
    public abstract class Service
    {
        public Workspace Workspace { get; internal set; }

        public virtual void Initialize() {}

        public override string ToString()
        {
            return $"Service {this.GetType().Name}";
        }
    }

    public Workspace()
    {
        if(WorkerThread.CurrentThread == null)
            throw new InvalidOperationException("Workspaces can be created only for WorkerThreads.");

        WorkerThread.CurrentThread.AssignWorkspace(this);
    }

    public static Workspace Current => WorkerThread.CurrentThread.Workspace;

    private readonly Dictionary<Type, Service> services = new();
    public void AddService<T>(T service) where T : Service 
    {
        if(service.Workspace != null && service.Workspace != this)
            throw new InvalidOperationException($"Cannot add {service.GetType().Name} to this workspace because it is already assigned to '{service.Workspace}'. Services cannot be shared across workspaces.");

        service.Workspace = this;
        services[typeof(T)] = service;
        service.Initialize();
    }
    public bool TryGetService<T>(out T _service) where T : Service 
    {
        _service = null;

        if (!services.TryGetValue(typeof(T), out var service))
            return false;

        _service = (T)service;
        return true;
    }

    public override string ToString()
    {
        return $"{WorkerThread.CurrentThread.Name} workspace";
    }
}