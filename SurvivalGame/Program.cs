using System;
using Luminix;
using Luminix.Threading;

var logger = Logger.Initialize();
try
{
    var mainCore = new Luminix.MainCore();
    var coreThread = new TickedThread(mainCore.Initialize, () =>
    {
        mainCore.Update();
        mainCore.Render();
    });
    coreThread.Name = "CoreThread";
    coreThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    coreThread.IsBackground = true;

    using var mainRender = new Luminix.MainRender(coreThread);
    var renderThread = new RenderThread(mainRender);

    coreThread.Start();
    mainRender.Run();
}
catch(Exception ex)
{
    logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
}
finally
{
    if (Thread.Threads != null)
        foreach (var thread in Thread.Threads)
            thread?.Stop();
    logger.Flush();
}