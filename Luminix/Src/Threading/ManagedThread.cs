using System;

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

            try
            {
                _ = new Workspace();
                running = true;
                threadAction();
                Workspace.Current.Shutdown();
            }
            finally
            {
                Application.Exit();

                Logger.Flush();
                thread.Value = null;
                running = false;
            }
        });

        this.Name = name;
    }

    protected Action threadAction;

    protected volatile bool running = false;

    public override bool IsAlive => running;

    public sealed override void Start() => SystemThread.Start();
    public sealed override void Stop() => running = false;
}