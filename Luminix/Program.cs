using System;
using Luminix;
using Luminix.Threading;

try
{
    var mainCore = new Luminix.MainCore();
    var coreThread = new TickedThread("CoreThread", mainCore.Initialize, mainCore.Tick, mainCore.OnStop);
    coreThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    coreThread.IsBackground = false;

    var mainRender = new Luminix.MainRender();
    var renderThread = new UnmanagedThread("RenderThread", mainRender.Run, mainRender.Stop);
    renderThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    renderThread.IsBackground = false;

    var someThread = new TickedThread("CoreThread", mainCore.Initialize, mainCore.Tick, mainCore.OnStop);

    coreThread.Start();
    renderThread.Start();
}
catch(Exception ex)
{
    Logger.NewLog(Logger.Log.Level.Error, $"RenderThread encountered a fatal error: {ex}");
}
finally
{
    Application.Exit();

    Logger.Flush();
}