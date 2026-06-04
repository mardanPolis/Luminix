using System;
using System.Threading;
using Luminix;
using Luminix.Threading;

Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
Thread.CurrentThread.Name = "RenderThread";

var logger = Logger.Initialize();
using var engine = new Luminix.Main();
try
{
    engine.Run();
}
catch(Exception ex)
{
    logger.Err(ex);
}
finally
{
    if (BaseThread.Threads != null)
        foreach (var thread in BaseThread.Threads)
            thread?.Stop();
    logger.Flush();
}