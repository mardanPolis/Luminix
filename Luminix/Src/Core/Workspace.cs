using System;
using System.Collections.Generic;
using System.Reflection;
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
            Workspace = Workspace.Current ?? throw new InvalidOperationException($"Service '{GetType().Name}' cannot be created: No active Workspace found.");
                
            var serviceType = GetType();
            if(Workspace.hasStarted)
                throw new InvalidOperationException($"Cannot add {serviceType.Name} to '{this}', reason: Workspace has been already started.");
            if(Workspace.services.ContainsKey(serviceType))
                throw new InvalidOperationException($"Cannot add {serviceType.Name} to '{this}', reason: Service with the same type exists.");

            Workspace.services[serviceType] = this;
        }

        protected virtual void Start() {}
        protected virtual void Shutdown() {}
        public override string ToString()
        {
            return $"Service {this.GetType().Name}";
        }
    }

    static Workspace()
    {
        var method = typeof(Service).GetMethod(
            "Start", BindingFlags.NonPublic | BindingFlags.Instance);

        var shutdownMethod = typeof(Service).GetMethod(
            "Shutdown", BindingFlags.NonPublic | BindingFlags.Instance);

        startService = (Action<Service>)Delegate.CreateDelegate(typeof(Action<Service>), method);
        shutdownService = (Action<Service>)Delegate.CreateDelegate(typeof(Action<Service>), shutdownMethod);
    }

    private static readonly Action<Service> startService;
    private static readonly Action<Service> shutdownService;
    private static ThreadLocal<Workspace> instance = new ThreadLocal<Workspace>(true);
    public static Workspace Current => instance.Value;

    internal Workspace()
    {
        try
        {
            var existing = instance.Value;
            
            if (existing != null)
                throw new InvalidOperationException("Workspace already exists!");

            instance.Value = this;
            services = new();
        }
        catch
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Failed to create a workspace!");
            throw;
        }
    }

    private readonly Dictionary<Type, Service> services;
    private bool hasStarted;    

    public bool HasStarted => hasStarted;

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
        return $"{TickedThread.CurrentThread.Name} workspace";
    }

    internal void Start()
    {
        if(hasStarted)
            return;

        hasStarted = true;
        foreach(var service in services.Values)
            startService(service);
    }

    internal void Shutdown()
    {
        if(hasStarted)
            return;
        
        foreach(var service in services.Values)
            shutdownService(service);
    }
}