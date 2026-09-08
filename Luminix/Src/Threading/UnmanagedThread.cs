using System;
using System.Reflection;

namespace Luminix.Threading;

internal sealed class UnmanagedThread : Thread
{
    private Action threadRun;
    private Action threadStop;

    public UnmanagedThread(string name, Action run, Action stop)
    {
        if(thread.Value != null)
            throw new InvalidOperationException("The current thread already has an UnmanagedThread instance associated with it.");

        SystemThread = System.Threading.Thread.CurrentThread;
        this.Name = name;

        thread.Value = this;
        logger = Logger.Get();

        threadRun = run;
        threadStop = stop;
    }

    public override void Start() => threadRun();
    public override void Stop() => threadStop();
}