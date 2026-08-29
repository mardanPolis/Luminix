using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Luminix;

public sealed partial class Logger
{
    public readonly struct Log
    {
        internal Log(Logger logger, int start, int length, Level level)
        {
            this.logger = logger;
            this.startIndex = start;
            this.Length = length;
            this.LogLevel = level;
        }

        public enum Level
        {
            Info,
            Warning,
            Error,
        }

        private Logger logger { get; }

        private int startIndex { get; }
        public int Length { get; }
        public Log.Level LogLevel { get; }
        public ReadOnlySpan<char> Message => logger.data.AsSpan(startIndex, Length);
    }

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
    private readonly List<(int Start, int Length, Log.Level level)> logs;
    private char[] data;
    private char[] logBuffer;

    private int pointer;

    private readonly List<(LogDropReason reason, DateTime time)> droppedLogs = new();

    private bool logInCreation;
    private bool locked;

    public string TimestampFormat { get; set; }
    public Log.Level MinimumLevel { get; set; } = Log.Level.Info;
    public bool EchoToConsole { get; set; }

    public void NewLog(Log.Level logLevel, string message) 
    {
        var builder = new LogBuilder(0, 0, this, logLevel);
        builder.AppendLiteral(message);
        builder.End();
    }
    public void NewLog(Log.Level logLevel, [InterpolatedStringHandlerArgument("", "logLevel")] ref LogBuilder message)
    {
        message.End();
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

    internal bool TryCreateLog(Log.Level type)
    {
        if(logInCreation) { droppedLogs.Add((LogDropReason.AlreadyCreatingLog, DateTime.Now)); return false; }
        if(locked) { droppedLogs.Add((LogDropReason.LoggerLocked, DateTime.Now)); return false; }
        if(type < MinimumLevel) return false;

        logInCreation = true;
        return true;
    }

    private void PushMessage(Log.Level level, ReadOnlySpan<char> message)
    {
        logInCreation = false;

        DateTime currentTime = DateTime.Now;

        Span<char> header = stackalloc char[128];
        int headerLength = WriteHeader(header, currentTime, level);


        int totalLength = headerLength + message.Length;

        if(EchoToConsole)
        {
            Span<char> consoleLine = stackalloc char[message.Length + Environment.NewLine.Length];
            message.CopyTo(consoleLine);
            Environment.NewLine.AsSpan().CopyTo(consoleLine.Slice(message.Length));
            Console.Out.Write(consoleLine);
        }

        if (pointer + totalLength <= data.Length)
        {
            header.Slice(0, headerLength).CopyTo(data.AsSpan(pointer));
            message.CopyTo(data.AsSpan(pointer + headerLength));
            logs.Add((pointer, totalLength, level));
            pointer += totalLength;
        }
        else
        {
            Flush();
            header.Slice(0, headerLength).CopyTo(data.AsSpan(0));
            message.CopyTo(data.AsSpan(headerLength));
            logs.Add((0, totalLength, level));
            pointer = totalLength;
        }

        FlushDroppedAttempts();
    }
    private void FlushDroppedAttempts()
    {
        if (droppedLogs.Count == 0) return;

        var logBuilder = new LogBuilder(this, Log.Level.Warning, out bool canLog);
        if(!canLog) return;

        logBuilder.AppendLiteral("Dropped ");
        logBuilder.AppendFormatted(droppedLogs.Count);
        logBuilder.AppendLiteral(" log attempt(s) while unavailable:");

        foreach (var (reason, time) in droppedLogs)
        {
            switch(reason)
            {
                case LogDropReason.AlreadyCreatingLog:
                    logBuilder.AppendLiteral(" - [");
                    logBuilder.AppendFormatted(time, "HH:mm:ss.fff");
                    logBuilder.AppendLiteral(" - was already creating a log ]");
                break;
                case LogDropReason.LoggerLocked: 
                    logBuilder.AppendLiteral(" - [");
                    logBuilder.AppendFormatted(time, "HH:mm:ss.fff");
                    logBuilder.AppendLiteral(" - logger was locked ]");
                break;
            }
        }

        logBuilder.End();
        droppedLogs.Clear();
    }

    private int WriteHeader(Span<char> dest, DateTime timestamp, Log.Level logLevel)
    {
        int written = 0;

        dest[written++] = '[';

        if (timestamp.TryFormat(dest.Slice(written), out int timeWritten, TimestampFormat, System.Globalization.CultureInfo.InvariantCulture))
            written += timeWritten;

        dest[written++] = ']';
        dest[written++] = ' ';

        ReadOnlySpan<char> thread = threadName;
        thread.CopyTo(dest.Slice(written));
        written += thread.Length;

        dest[written++] = ' ';

        ReadOnlySpan<char> typeLabel = logLevel switch
        {
            Log.Level.Info => "[INFO] : ",
            Log.Level.Warning => "[WARNING] : ",
            Log.Level.Error => "[ERROR] : ",
            _ => ReadOnlySpan<char>.Empty,
        };

        typeLabel.CopyTo(dest.Slice(written));
        written += typeLabel.Length;

        return written;
    }
}
