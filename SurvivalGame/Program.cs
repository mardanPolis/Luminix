using System;
using Luminix;
using Luminix.Threading;

using var engine = new Luminix.Main();
try
{
    engine.Run();
}
catch(Exception ex)
{
    Logger.Get().Err(ex);
    foreach(var threads in BaseThread.Threads)
        threads.Stop();
}

Luminix.Logger[] loggers = Logger.GetAll();
foreach(var logger in loggers)
    logger.Flush();