using System;
using System.Collections.Generic;
using System.Threading;
using Luminix.Threading;

namespace Luminix.Workspaces;

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

        public virtual void Update() {}

        public override string ToString()
        {
            return $"Service {this.GetType().Name}";
        }
    }

    private Workspace() { }

    private static ThreadLocal<Workspace> instance = new ThreadLocal<Workspace>(true);
    public static Workspace Current => instance.Value;

    public static Workspace Create()
    {
        var existing = instance.Value ?? WorkerThread.CurrentThread?.Workspace;
        
        if (existing != null)
        {
            instance.Value = existing;
            Logger.Get().Wrn("Workspace already exists; returning existing instance.");
            return existing;
        }

        try
        {
            var workspace = new Workspace();
            
            if (WorkerThread.CurrentThread != null)
                WorkerThread.CurrentThread.AssignWorkspace(workspace);

            instance.Value = workspace;
            return workspace;
        }
        catch (Exception ex)
        {
            Logger.Get().Err($"Critical failure creating workspace: {ex.Message}");
            return null; 
        }
    }

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