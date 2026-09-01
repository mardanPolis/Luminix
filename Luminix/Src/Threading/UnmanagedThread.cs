using System;

namespace Luminix.Threading;

internal sealed class UnmanagedThread : Thread
{
    private Action threadAction;

    public UnmanagedThread(string name, Action action)
    {
        if(thread.Value != null)
            throw new InvalidOperationException("The current thread already has an UnmanagedThread instance associated with it.");

        SystemThread = System.Threading.Thread.CurrentThread;
        this.Name = name;

        thread.Value = this;
        logger = Logger.Initialize();

        threadAction = action;
    }

    public override void Start() => threadAction();
}