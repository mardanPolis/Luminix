using System;
using System.Threading;
using Luminix;
using Luminix.Threading;

Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
Thread.CurrentThread.Name = "RenderThread";

Logger.Initialize();
using var engine = new Luminix.Main();
try
{
    engine.Run();
}
catch(Exception ex)
{
    Logger.Get().Err(ex);
}
finally
{
    foreach(var threads in BaseThread.Threads)
        threads.Stop();
}

Luminix.Logger[] loggers = Logger.GetAll();
foreach(var logger in loggers)
    logger.Flush();