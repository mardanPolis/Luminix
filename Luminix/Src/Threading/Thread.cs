using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Luminix.Threading;

public abstract class Thread
{
    private static readonly HashSet<string> threadNames = new();
    private static readonly object threadNameLock = new();
    protected static readonly ThreadLocal<Thread> thread = new(true);

    public static Thread CurrentThread => thread.Value;
    public static IList<Thread> Threads => thread.Values;

    private protected Thread() 
    {
        this.ThreadID = new ID((uint)Threads.Count);
    }

    private protected Workspace workspace;
    private protected Workspace.Controller workspaceController;

    protected System.Threading.Thread SystemThread { get; private protected init; }

    public ID ThreadID { get; private init; }
    public string Name 
    {   
        get => SystemThread.Name; 
        protected set 
        {
            lock(threadNameLock)
            {
                if(threadNames.Contains(value))
                {
                    Logger.Global.NewLog(Logger.Log.Level.Error, $"Cant name {this} thread to '{value}', reason: An another thread is already named '{value}'");
                    return;
                }

                threadNames.Remove(Name);
                threadNames.Add(value);

                SystemThread.Name = value;
            }
        }
    }
    public bool IsBackground { get => SystemThread.IsBackground; set => SystemThread.IsBackground = value; }
    public virtual bool IsAlive => SystemThread.IsAlive;
    public ThreadPriority Priority { get => SystemThread.Priority; set => SystemThread.Priority = value; }

    public abstract void Start();
    public abstract void Stop();

    public override string ToString() => Name;
}