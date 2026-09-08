using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Luminix.Threading;

public abstract class Thread
{
    protected static readonly ThreadLocal<Thread> thread = new(true);

    public static Thread CurrentThread => thread.Value;
    public static IList<Thread> Threads => thread.Values;

    protected Logger logger;

    private protected Thread() 
    {
        this.ThreadID = new ID((uint)Threads.Count);
    }

    private protected Workspace workspace;
    private protected Workspace.Controller workspaceController;

    protected System.Threading.Thread SystemThread { get; private protected init; }

    public ID ThreadID { get; private init; }
    public string Name { get => SystemThread.Name; set { SystemThread.Name = value; LogPath = Path.Combine(Logger.LogDir, $"thread_{value}.log"); } }
    public bool IsBackground { get => SystemThread.IsBackground; set => SystemThread.IsBackground = value; }
    public virtual bool IsAlive => SystemThread.IsAlive;
    public ThreadPriority Priority { get => SystemThread.Priority; set => SystemThread.Priority = value; }

    public string LogPath { get; private set; }

    public abstract void Start();

    public override string ToString() => Name;
}