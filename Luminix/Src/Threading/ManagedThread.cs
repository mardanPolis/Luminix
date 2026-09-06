using System;
using System.Collections.Generic;
using System.Threading;

namespace Luminix.Threading;

public abstract class ManagedThread : Thread
{
    protected ManagedThread(string name)
    {
        this.SystemThread = new System.Threading.Thread(() =>
        {
            if(threadAction == null)
                throw new InvalidOperationException($"Thread action cannot be null, thread: {ThreadID} {Name} ");

            thread.Value = this;
            logger = Logger.Initialize();

            try
            {
                running = true;
                threadAction();
            }
            catch(Exception ex)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
            }
            finally
            {
                Logger.Shutdown();
                thread.Value = null;
                running = false;
            }
        });
        this.Name = name;
    }

    protected Action threadAction;

    protected volatile bool running = false;

    public override bool IsAlive => running;

    public override void Start() => SystemThread.Start();
    public void Stop()
    {
        running = false;
    }
}