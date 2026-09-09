using System;
using System.Collections.Generic;
using System.Threading;
using Luminix.Threading;

namespace Luminix;

public sealed class Workspace
{
    internal readonly struct Controller
    {
        internal Controller(Workspace controlled)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a UIManager.Controller, the provided UI manager is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly Workspace controlled;

        public void Start()
        {
            var startMethods = controlled.startMethods;
            controlled.startMethods = null;
            foreach(var method in startMethods)
            {
                method();
            }
        }
    }

    public abstract class Service
    {
        public Workspace Workspace { get; private set; }

        public Service()
        {
            Workspace = Workspace.Current ?? throw new InvalidOperationException($"Service '{GetType().Name}' cannot be created: No active Workspace found.");
                
            var serviceType = GetType();
            if(Workspace.startMethods == null)
                throw new InvalidOperationException($"Cannot add {serviceType.Name} to '{this}', reason: Workspace has been already started.");
            if(Workspace.services.ContainsKey(serviceType))
                throw new InvalidOperationException($"Cannot add {serviceType.Name} to '{this}', reason: Service with the same type exists.");

            Workspace.services[serviceType] = this;
            Workspace.startMethods.Add(Start);
        }

        protected virtual void Start() {}

        public override string ToString()
        {
            return $"Service {this.GetType().Name}";
        }
    }

    private static ThreadLocal<Workspace> instance = new ThreadLocal<Workspace>(true);
    public static Workspace Current => instance.Value;

    internal Workspace(out Controller controller)
    {
        try
        {
            var existing = instance.Value;
            
            if (existing != null)
                throw new InvalidOperationException("Workspace already exists!");

            instance.Value = this;
            services = new();
            startMethods = new();

            controller = new Controller(this);
            controllerIssued = true;
        }
        catch
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Failed to create a workspace!");
            throw;
        }
    }

    private readonly bool controllerIssued;
    private readonly Dictionary<Type, Service> services;
    private List<Action> startMethods;
    
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
}