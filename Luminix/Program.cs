using System;
using Luminix;
using Luminix.Threading;

try
{
    Logger.Initialize();
    
    var mainCore = new Luminix.MainCore();
    var coreThread = new TickedThread("CoreThread", mainCore.Initialize, mainCore.Tick, mainCore.OnStop);
    coreThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    coreThread.IsBackground = false;

    var mainRender = new Luminix.MainRender(coreThread);
    var renderThread = new UnmanagedThread("RenderThread", mainRender.Run, mainRender.Stop);
    renderThread.Priority = System.Threading.ThreadPriority.AboveNormal;
    renderThread.IsBackground = false;

    coreThread.Start();
    renderThread.Start();
}
catch(Exception ex)
{
    Logger.Get().NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
}
finally
{
    Application.Exit();

    Logger.Shutdown();
}