using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Luminix;

public sealed partial class Logger
{
    private static readonly ThreadLocal<Logger> instance = new ThreadLocal<Logger>(true);

    internal static Logger Initialize()
    {
        if(instance.IsValueCreated)
            return instance.Value;

        var logger = new Logger();
        instance.Value = logger;
        return logger;
    }
    internal static void Shutdown()
    {
        var logger = instance.Value;
        if(logger != null)
        {
            logger.Flush();
            instance.Value = null;
        }
    }

    public static Logger Get()
    {
        return instance.Value;
    }
    public static Logger[] GetAll()
    {
        return instance.Values.ToArray();
    }

    private Logger(int logMaximumLength = 16384)
    {
        logs = new(500);
        data = new char[256000];
        logBuffer = new char[Math.Min(data.Length, logMaximumLength)];
        logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        
        string name = Thread.CurrentThread.Name;
        path =  Path.Combine(
            logDir,
            $"thread_{name}.log"
        );

        name = Thread.CurrentThread.Name;
        if(string.IsNullOrWhiteSpace(name))
            threadName = "[Engine Thread]";
        else
            threadName = $"[{name}]";

        File.WriteAllText(path, string.Empty);
    }

    private readonly string threadName;

    private readonly string logDir;
    private readonly string path;
    private readonly List<(int Start, int Length)> logs;
    private char[] data;
    private char[] logBuffer;

    private int pointer;

    private readonly List<(LogDropReason reason, DateTime time)> droppedLogs = new();

    private bool logInCreation;
    private bool locked;

    public string TimestampFormat { get; set; }
    public Log.Type MinimumLevel { get; set; } = Log.Type.Info;
    public bool EchoToConsole { get; set; }

    public Log NewLog()
    {
        return NewLog(true, true, Log.Type.Info);
    }
    public Log NewWrn()
    {
        return NewLog(true, true, Log.Type.Warning);
    }
    public Log NewErr()
    {
        return NewLog(true, true, Log.Type.Error);
    }
    
    public void NewLog([InterpolatedStringHandlerArgument("")] ref LogBuilder message)
    {
        message.Push();
    }

    public void NewWrn([InterpolatedStringHandlerArgument("")] ref WrnBuilder message)
    {
        message.Push();
    }

    public void NewErr([InterpolatedStringHandlerArgument("")] ref ErrBuilder message)
    {
        message.Push();
    }

    public Log NewLogIf(bool condition, bool timestamp, bool threadInfo, Log.Type type)
    {
        if(condition)
            return NewLog(timestamp, threadInfo, type);

        return new Log(this, null);
    }
    public Log NewLog(bool timestamp, bool threadInfo, Log.Type type)
    {
        if(logInCreation) { droppedLogs.Add((LogDropReason.AlreadyCreatingLog, DateTime.Now)); return new Log(this, null); }
        if(locked) { droppedLogs.Add((LogDropReason.LoggerLocked, DateTime.Now)); return new Log(this, null); }
        if(type < MinimumLevel) return new Log(this, null);

        logInCreation = true;
        Log log = new Log(this, logBuffer);

        if(timestamp)
            log.AppendTimestamp(TimestampFormat);

        if(threadInfo)
            log.AppendThreadName(threadName);

        log.AppendType(type);

        return log;
    }

    public void Flush()
    {
        if(locked) return;

        using (StreamWriter writer = new StreamWriter(path, append: true))
        {
            writer.Write(data, 0, pointer);
        }
        pointer = 0;
        logs.Clear();

        logInCreation = false;
    }
    public void Clear()
    {
        if(locked) return;

        Console.Clear();
        pointer = 0;
        logs.Clear();
    }

    public void Refresh()
    {
        logInCreation = false;
        FlushDroppedAttempts();
    }

    internal bool TryCreateLog(Log.Type type, out Log log)
    {
        if (logInCreation || locked || type < MinimumLevel)
        {
            log = new Log(this, null);
            return false;
        }

        logInCreation = true;
        log = new Log(this, logBuffer);
        return true;
    }

    private void PushMessage(ReadOnlySpan<char> message)
    {
        logInCreation = false;

        if(EchoToConsole)
        {
            Span<char> consoleLine = stackalloc char[message.Length + Environment.NewLine.Length];
            message.CopyTo(consoleLine);
            Environment.NewLine.AsSpan().CopyTo(consoleLine.Slice(message.Length));
            Console.Out.Write(consoleLine);
        }

        if(message.TryCopyTo(data.AsSpan(pointer)))
        {
            logs.Add((pointer, message.Length));
            pointer += message.Length;
            return;
        }

        Flush();
        message.CopyTo(data.AsSpan(pointer));
        logs.Add(new (0, message.Length));
        pointer = message.Length;

        FlushDroppedAttempts();
    }
    private void FlushDroppedAttempts()
    {
        if (droppedLogs.Count == 0) return;

        var log = NewLog(true, true, Log.Type.Warning);
        log.Append("Dropped ").Append(droppedLogs.Count).Append(" log attempt(s) while unavailable:");

        foreach (var (reason, time) in droppedLogs)
        {
            switch(reason)
            {
                case LogDropReason.AlreadyCreatingLog:
                    log.Append(" - [").Append(time, "HH:mm:ss.fff").Append(" - was already creating a log ]");
                break;
                case LogDropReason.LoggerLocked: 
                    log.Append(" - [").Append(time, "HH:mm:ss.fff").Append(" - logger was locked ]");
                break;
            }
        }

        log.End();
        droppedLogs.Clear();
    }
}
