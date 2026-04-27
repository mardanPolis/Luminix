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
    foreach(var workerThread in WorkerThread.Threads)
        workerThread.Stop();
}

Luminix.Logger[] loggers = Logger.GetAll();
foreach(var logger in loggers)
    logger.Flush();