using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Luminix.Threading;

public abstract class Thread
{
    private static readonly ConcurrentDictionary<string, Thread> threads = new();
    protected static readonly ThreadLocal<Thread> thread = new(true);

    public static Thread CurrentThread => thread.Value;
    public static IList<Thread> Threads => thread.Values;

    private protected Thread() 
    {
        this.ThreadID = new ID((uint)Threads.Count);
    }

    protected System.Threading.Thread SystemThread { get; private protected init; }

    public ID ThreadID { get; private init; }
    public string Name 
    {   
        get => SystemThread.Name; 
        protected set 
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Name));

            if(!threads.TryAdd(value, this))
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Cant name {this} thread to '{value}', reason: An another thread is already named '{value}'");
                return;
            }

            if (Name is not null)
            {
                threads.TryRemove(Name, out _);
            }

            SystemThread.Name = value;
        }
    }
    public bool IsBackground { get => SystemThread.IsBackground; set => SystemThread.IsBackground = value; }
    public virtual bool IsAlive => SystemThread.IsAlive;
    public ThreadPriority Priority { get => SystemThread.Priority; set => SystemThread.Priority = value; }

    public static Thread GetThreadByName(string name)
    {
        if(threads.TryGetValue(name, out Thread thread))
            return thread;
        
        throw new KeyNotFoundException($"No thread found with name '{name}'.");
    }
    public static bool TryGetThreadByName(string name, out Thread thread) => threads.TryGetValue(name, out thread);

    public void Start()
    {
        OnStart();
    }
    public void Stop()
    {
        OnStop();
    }

    protected abstract void OnStart();
    protected abstract void OnStop();

    public override string ToString() => Name;
}