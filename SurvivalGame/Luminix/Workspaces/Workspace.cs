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
            if (Workspace.Current == null)
                throw new InvalidOperationException(
                    $"Service '{GetType().Name}' cannot be created: No active Workspace found.");

            Workspace = Workspace.Current;
            Workspace.AddService(this);
        }

        // Call this after construction is fully complete
        public static T Create<T>() where T : Service, new()
        {
            var service = new T();
            return service;
        }

        public override string ToString()
        {
            return $"Service {this.GetType().Name}";
        }
    }
    public enum State
    {
        Initialize,
        Running,
        Stopped,
    }

    public static string GetStateString(State value)
    {
        switch(value)
        {
            case State.Running:
                return "Running";
            case State.Initialize:
                return "Initialize";
            default:
                return "Stopped";
        }
    }

    private static ThreadLocal<Workspace> instance = new ThreadLocal<Workspace>(true);
    public static Workspace Current => instance.Value;

    public static Workspace Create()
    {
        var existing = instance.Value;
        
        if (existing != null)
        {
            instance.Value = existing;
            Logger.Get().Wrn("Workspace already exists; returning existing instance.");
            return existing;
        }

        try
        {
            var workspace = new Workspace();
            
            workspace.SetState(Workspace.State.Initialize);

            instance.Value = workspace;
            return workspace;
        }
        catch (Exception ex)
        {
            Logger.Get().Err($"Critical failure creating workspace: {ex.Message}");
            return null; 
        }
    }

    private Workspace() { }

    private State state;
    private string subState;

    public string FullCurrentState
    {
        get
        {
            if(string.IsNullOrWhiteSpace(subState))
                return GetStateString(state);

            return GetStateString(state) + " | " + subState;
        }
    }
    
    private readonly Dictionary<Type, Service> services = new();
    private void AddService(Service service)
    {
        if(services.ContainsKey(service.GetType()))
            throw new InvalidOperationException($"Cannot add {service.GetType().Name} to this workspace because '{this}' already contains '{service.Workspace}'.");

        services[service.GetType()] = service;
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

    internal void SetState(State value)
    {
        state = value;
        subState = null;
    }

    public bool IsState(State value)
    {
        return state == value;
    }

    public State GetState()
    {
        return state;
    }

    public void SetSubState(string value)
    {
        subState = value;
    }
    public bool IsSubState(string value)
    {
        return subState == value;
    }
    public string GetSubState()
    {
        return subState;
    }

    public override string ToString()
    {
        return $"{TickedThread.CurrentThread.Name} workspace";
    }
}