using System;
using Luminix;
using Luminix.Threading;

try
{
    var mainCore = new Luminix.MainCore();
    var coreThread = new TickedThread("CoreThread", mainCore.Initialize, () =>
    {
        mainCore.Update();
        mainCore.Render();
    });
    coreThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    coreThread.IsBackground = false;

    var renderThread = new UnmanagedThread("RenderThread", () =>
    {
        using var mainRender = new Luminix.MainRender(coreThread);
        mainRender.Run();
    });
    renderThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    renderThread.IsBackground = false;

    coreThread.Start();
    renderThread.Start();
}
catch(Exception ex)
{
    Console.WriteLine($"{ex}");
}
finally
{
    if (Thread.Threads != null)
        foreach (var thread in Thread.Threads)
            if(thread is ManagedThread managedThread)
                managedThread?.Stop();
    Logger.Get().Flush();
}