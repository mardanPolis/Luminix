using System;
using System.Collections.Generic;
using System.Threading;
using Luminix;
using Luminix.Identifiers;

public abstract class BaseThread
{
    private static readonly ThreadLocal<BaseThread> thread = new(true);

    public static BaseThread CurrentThread => thread.Value;
    public static IList<BaseThread> Threads => thread.Values;

    protected BaseThread()
    {
        systemThread = new Thread(() =>
        {
            Logger.Initialize();
            
            if(threadAction == null)
                throw new InvalidOperationException($"Thread action cannot be null, thread: {ThreadID} {Name} ");

            try
            {
                running = true;
                thread.Value = this;
                threadAction();
            }
            finally
            {
                thread.Value = null;
                Logger.Shutdown();
                running = false;
            }
        });

        ThreadID = new ID((uint)Threads.Count);
    }

    protected Action threadAction;

    private readonly Thread systemThread;
    protected volatile bool running = false;

    public ID ThreadID { get; private set; }
    public string Name { get => systemThread.Name; set => systemThread.Name = value; }
    public bool IsBackground { get => systemThread.IsBackground; set => systemThread.IsBackground = value; }
    public bool IsAlive => running;
    public ThreadPriority Priority { get => systemThread.Priority; set => systemThread.Priority = value; }


    public void Start() => systemThread.Start();
    public void Stop()
    {
        running = false;
        OnStop();
    }

    protected virtual void OnStop() { }
}