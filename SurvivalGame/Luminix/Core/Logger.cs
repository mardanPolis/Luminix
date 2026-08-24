using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Luminix;

public sealed class Logger
{
    public ref struct Log
    {
        public enum Type
        {
            Info,
            Warning,
            Error,
        }

        private readonly Logger logger;
        private char[] buffer;
        private int pointer;

        internal Log(Logger logger, char[] buffer)
        {
            this.logger = logger;
            this.buffer = buffer;
        }

        public Log Append(ReadOnlySpan<Char> value)
        {
            if (buffer == null) return this;

            Add(value);
            return this;
        }
        public Log Append(object value)
        {
            if (buffer == null) return this;
            
            switch (value)
            {
                case null:
                    break;
                case string s:
                    Append(s);
                    break;
                case int i:
                    Append((long)i);
                    break;
                case ISpanFormattable f:
                    Add(f, default);
                    break;
                default:
                    Append(value.ToString());
                    break;
            }

            return this;
        }

        public Log Append<T>(T value) where T : ISpanFormattable
        {
            if (buffer == null) return this;
 
            Add(value, default);
            return this;
        }
        public Log Append<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            if (buffer == null) return this;
 
            try
            {
                Add(value, format);
                return this;
            }
            catch (FormatException)
            {
                Append(value).FormatError(format);
                return this;
            }
        }
 
        public void End()
        {
            if(buffer == null) return;

            logger.PushMessage(buffer.AsSpan(0, pointer));
            buffer = null;
        }

        internal void AppendTimestamp(ReadOnlySpan<char> format)
        {
            Append('[');
            Append(DateTime.Now, format);
            Append("] ");
        }
        internal void AppendThreadName(ReadOnlySpan<char> threadName)
        {
            Append(threadName);
            Append(' ');
        }
        internal void AppendType(Log.Type type)
        {
            switch(type)
            {
                case Log.Type.Info:
                    Append("[INFO] : ");
                break;
                case Log.Type.Warning:
                    Append("[WARNING] : ");
                break;
                case Log.Type.Error:
                    Append("[ERROR] : ");
                break;
            }
        }


        private void Add(ReadOnlySpan<char> value)
        {
            int remaining = buffer.Length - pointer;

            if (value.Length > remaining)
            {
                value.Slice(0, remaining).CopyTo(buffer.AsSpan(pointer));
                pointer += remaining;

                LengthError();
                return;
            }

            value.CopyTo(buffer.AsSpan(pointer));
            pointer += value.Length;
        }
        private void Add<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            Span<char> dest = buffer.AsSpan(pointer);

            if (value.TryFormat(dest, out int written, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                pointer += written;
                return;
            }

            Span<char> scratch = stackalloc char[128];
            if (!value.TryFormat(scratch, out int fullWritten, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                LengthError();
                return;
            }

            int remaining = buffer.Length - pointer;
            int toCopy = Math.Min(fullWritten, remaining);

            if (toCopy > 0)
            {
                scratch.Slice(0, toCopy).CopyTo(buffer.AsSpan(pointer));
                pointer += toCopy;
            }

            LengthError();
        }
        
        private void LengthError()
        {
            ReadOnlySpan<char> chars = "...[Length Error -> log exceeded length of 16384 characters]";

            var length = chars.Length;
            chars.CopyTo(buffer.AsSpan(pointer - length));
            End();
        }
        private void FormatError(ReadOnlySpan<Char> format)
        {
            Append(" [Format Error -> '").Append(format).Append("']");
        }
    }
    
    private enum LogDropReason
    {
        AlreadyCreatingLog,
        LoggerLocked,
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

    public LogEnumerator GetEnumerator() => new LogEnumerator(this);

    public ref struct LogEnumerator : IEnumerator<ReadOnlySpan<char>>
    {
        internal LogEnumerator(Logger logger)
        {
            this.logger = logger;
            this.index = -1;
            this.current = default;
            logger.locked = true;
        }

        private Logger logger;
        private int index;
        private ReadOnlySpan<char> current;

        public readonly ReadOnlySpan<char> Current => current;
        readonly object IEnumerator.Current => current.ToArray();

        public bool MoveNext()
        {
            if (index + 1 >= logger.logs.Count)
                return false;

            index++;
            (int start, int length) logInfo = logger.logs[index];
            current = logger.data.AsSpan(logInfo.start, logInfo.length);
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default;
        }

        public void Dispose()
        {
            logger.locked = false;
            logger.FlushDroppedAttempts();
        }
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
