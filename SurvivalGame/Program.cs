using System;
using Verse;
using Verse.Threading;

using var game = new Verse.Main();
try
{
    game.Run();
}
catch(Exception ex)
{
    Logger.Get().Err(ex);
    foreach(var workerThread in WorkerThread.Threads)
        workerThread.Stop();
}

Verse.Logger[] loggers = Logger.GetAll();
foreach(var logger in loggers)
    logger.Flush();