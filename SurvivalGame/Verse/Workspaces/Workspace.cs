using System;
using System.Collections.Generic;
using Verse.Threading;

namespace Verse.Workspaces;

public sealed class Workspace
{
    public abstract class Service
    {
        public Workspace Workspace { get; private set; }

        public Service()
        {
            if(Workspace.Current == null)
                throw new InvalidOperationException("Service cannot be initialized: No active Workspace found.");

            Workspace = Workspace.Current;
            Workspace.AddService(this);
        }

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
    private void AddService(Service service)
    {
        if(services.ContainsKey(service.GetType()))
            throw new InvalidOperationException($"Cannot add {service.GetType().Name} to this workspace because '{this}' already contains '{service.Workspace}'.");

        services[service.GetType()] = service;
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
    public T GetService<T>() where T : Service
    {
        if (!services.TryGetValue(typeof(T), out var service))
            throw new KeyNotFoundException($"Service of type {typeof(T).Name} is not registered.");

        return (T)service;
    }

    public override string ToString()
    {
        return $"{WorkerThread.CurrentThread.Name} workspace";
    }
}