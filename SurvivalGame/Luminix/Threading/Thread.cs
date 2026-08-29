using System;
using System.Collections.Generic;
using System.Threading;

namespace Luminix.Threading;

public abstract class Thread
{
    private static readonly ThreadLocal<Thread> thread = new(true);

    public static Thread CurrentThread => thread.Value;
    public static IList<Thread> Threads => thread.Values;

    protected Logger logger;

    protected Thread()
    {
        thread.Value = this;
        systemThread = System.Threading.Thread.CurrentThread;
        ThreadID = new ID((uint)Threads.Count);
    }

    protected System.Threading.Thread systemThread { get; init; }

    public ID ThreadID { get; private set; }
    public string Name { get => systemThread.Name; set => systemThread.Name = value; }
    public bool IsBackground { get => systemThread.IsBackground; set => systemThread.IsBackground = value; }
    public virtual bool IsAlive => systemThread.IsAlive;
    public ThreadPriority Priority { get => systemThread.Priority; set => systemThread.Priority = value; }

    protected void NullifyThread()
    {
        thread.Value = null;
    }

    public abstract void Start();
    public abstract void Stop();
}