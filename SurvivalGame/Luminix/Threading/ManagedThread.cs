using System;
using System.Collections.Generic;
using System.Threading;

namespace Luminix.Threading;

public abstract class ManagedThread : Thread
{
    protected ManagedThread()
    {
        systemThread = new System.Threading.Thread(() =>
        {
            logger = Logger.Initialize();
            
            if(threadAction == null)
                throw new InvalidOperationException($"Thread action cannot be null, thread: {ThreadID} {Name} ");

            try
            {
                running = true;
                threadAction();
                OnStop();
            }
            catch(Exception ex)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
            }
            finally
            {
                NullifyThread();
                Logger.Shutdown();
                running = false;
            }
        });

    }

    protected Action threadAction;

    protected volatile bool running = false;

    public override bool IsAlive => running;

    public override void Start() => systemThread.Start();
    public override void Stop()
    {
        running = false;
        OnStop();
    }

    protected virtual void OnStop() { }
}