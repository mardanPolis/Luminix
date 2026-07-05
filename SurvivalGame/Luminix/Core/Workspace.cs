using System;
using System.Collections.Generic;
using System.Threading;
using Luminix.Threading;

namespace Luminix;

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
    private static string GetStateString(State value)
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

    private Workspace()
    {
        services = new();
    }

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
    
    private readonly Dictionary<Type, Service> services;
    private void AddService(Service service)
    {
        var serviceType = service.GetType();
        if(services.ContainsKey(serviceType))
            throw new InvalidOperationException($"Cannot add {serviceType.Name} to '{this}', reason: Service with the same type exists.");

        services[serviceType] = service;
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