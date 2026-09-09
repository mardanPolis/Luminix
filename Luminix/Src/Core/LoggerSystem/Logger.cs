using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Luminix;

public sealed partial class Logger
{
    public readonly ref struct Log
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

    public static class Global
    {
        internal static readonly Logger Instance;

        static Global()
        {
            Instance = new Logger("Global");
            Instance.EchoToConsole = true;
        }

        public static void NewLog(Log.Level logLevel, string message) 
        {
            Console.WriteLine(message);
            var builder = new LogBuilder(Instance, logLevel);
            builder.AppendLiteral(message);
            builder.End();
        }
        public static void NewLog(Log.Level logLevel, [InterpolatedStringHandlerArgument("logLevel")] ref LogBuilder message)
        {
            message.End();
        }
        public static void NewLog<T>(Log.Level logLevel, IReadOnlyList<T> collection)
        {
            var builder = new LogBuilder(Instance, logLevel);

            builder.AppendFormatted(collection.GetType());
            if(collection.Count > 0)
            {
                builder.AppendFormatted(" contains: \n");
                for (int i = 0; i < collection.Count; i++)
                {
                    builder.AppendFormatted(i);
                    builder.AppendFormatted(" - ");

                    if (collection[i] is ISpanFormattable formattable)
                        builder.AppendFormatted(formattable);
                    else
                        builder.AppendFormatted(collection[i]?.ToString() ?? "null");

                    builder.AppendFormatted('\n');
                }
            }
            else
            {
                builder.AppendFormatted(" is empty");
            }
            builder.End();
        }
        public static void NewLog<TDictionary, T1, T2>(Log.Level logLevel, TDictionary collection) where TDictionary : IReadOnlyDictionary<T1, T2>
        {
            var builder = new LogBuilder(Instance, logLevel);

            var collectionType = typeof(TDictionary);
            builder.AppendFormatted(collectionType);
            if(collection.Count > 0)
            {
                builder.AppendFormatted(" contains:");
                foreach(var pair in collection)
                {
                    builder.AppendFormatted('\n');
                    builder.AppendFormatted("- (");
                    builder.AppendFormatted(typeof(T1));
                    builder.AppendFormatted(")");

                    if (pair.Key is ISpanFormattable key)
                        builder.AppendFormatted(key);
                    else
                        builder.AppendFormatted(pair.Key.ToString() ?? "null");

                    builder.AppendFormatted(" : (");
                    builder.AppendFormatted(typeof(T2));
                    builder.AppendFormatted(")[");

                    if (pair.Value is ISpanFormattable value)
                    {
                        builder.AppendFormatted(pair.Value.GetType());
                        builder.AppendFormatted(" - ");
                        builder.AppendFormatted(value);
                    }
                    else
                    {
                        builder.AppendFormatted(pair.Value.GetType());
                        builder.AppendFormatted(" - ");
                        builder.AppendFormatted(pair.Value?.ToString() ?? "null");
                    }
                    builder.AppendFormatted(']');
                }
            }
            else
            {
                builder.AppendFormatted(" is empty");
            }
            builder.End();
        }
    }

    private static readonly ThreadLocal<Logger> instance = new ThreadLocal<Logger>(true);
    internal static readonly string LogDir;

    static Logger()
    {
        LogDir = Path.Combine(AppContext.BaseDirectory, "logs");
    }

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
        if(instance.Value == null)
            throw new InvalidOperationException("Logger instance has not been initialized. Call Initialize() first.");

        return instance.Value;
    }
    public static Logger[] GetAll()
    {
        return instance.Values.ToArray();
    }

    private Logger(string name, int logMaximumLength = 16384) : this(logMaximumLength)
    {
        this.name = name;
    }
    private Logger(int logMaximumLength = 16384) : this()
    {
        loggerLock = new();
        logs = new(500);
        data = new char[256000];
        logBuffer = new char[Math.Min(data.Length, logMaximumLength)];
    }

    private Logger(string logPath)
    {
        Directory.CreateDirectory(LogDir);

        logPath ??= Path.Combine(Logger.LogDir, $"unknown_{instance.Values.Count}.log");
        File.WriteAllText(logPath, string.Empty);
    }

    private readonly object loggerLock;
    private readonly string name = null;
    private readonly string logPath = null;

    private readonly List<(int Start, int Length, Log.Level level)> logs;
    private char[] data;
    private char[] logBuffer;

    private int pointer;

    public string TimestampFormat { get; set; }
    public bool EchoToConsole { get; set; }

    public void NewLog(Log.Level logLevel, string message) 
    {
        var builder = new LogBuilder(this, logLevel);
        builder.AppendLiteral(message);
        builder.End();
    }
    public void NewLog(Log.Level logLevel, [InterpolatedStringHandlerArgument("", "logLevel")] ref LogBuilder message)
    {
        message.End();
    }
    public void NewLog<T>(Log.Level logLevel, IReadOnlyList<T> collection)
    {
        var builder = new LogBuilder(this, logLevel);

        builder.AppendFormatted(collection.GetType());
        if(collection.Count > 0)
        {
            builder.AppendFormatted(" contains: \n");
            for (int i = 0; i < collection.Count; i++)
            {
                builder.AppendFormatted(i);
                builder.AppendFormatted(" - ");

                if (collection[i] is ISpanFormattable formattable)
                    builder.AppendFormatted(formattable);
                else
                    builder.AppendFormatted(collection[i]?.ToString() ?? "null");

                builder.AppendFormatted('\n');
            }
        }
        else
        {
            builder.AppendFormatted(" is empty");
        }
        builder.End();
    }
    public void NewLog<TDictionary, T1, T2>(Log.Level logLevel, TDictionary collection) where TDictionary : IReadOnlyDictionary<T1, T2>
    {
        var builder = new LogBuilder(this, logLevel);

        var collectionType = typeof(TDictionary);
        builder.AppendFormatted(collectionType);
        if(collection.Count > 0)
        {
            builder.AppendFormatted(" contains:");
            foreach(var pair in collection)
            {
                builder.AppendFormatted('\n');
                builder.AppendFormatted("- (");
                builder.AppendFormatted(typeof(T1));
                builder.AppendFormatted(")");

                if (pair.Key is ISpanFormattable key)
                    builder.AppendFormatted(key);
                else
                    builder.AppendFormatted(pair.Key.ToString() ?? "null");

                builder.AppendFormatted(" : (");
                builder.AppendFormatted(typeof(T2));
                builder.AppendFormatted(")[");

                if (pair.Value is ISpanFormattable value)
                {
                    builder.AppendFormatted(pair.Value.GetType());
                    builder.AppendFormatted(" - ");
                    builder.AppendFormatted(value);
                }
                else
                {
                    builder.AppendFormatted(pair.Value.GetType());
                    builder.AppendFormatted(" - ");
                    builder.AppendFormatted(pair.Value?.ToString() ?? "null");
                }
                builder.AppendFormatted(']');
            }
        }
        else
        {
            builder.AppendFormatted(" is empty");
        }
        builder.End();
    }

    public void Flush()
    {
        lock(loggerLock)
        {
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                foreach(Log log in this)
                {
                    writer.WriteLine(log.Message);
                } 
            }
            pointer = 0;
            logs.Clear();
        }
    }
    public void Clear()
    {
        lock(loggerLock)
        {
            Console.Clear();
            pointer = 0;
            logs.Clear();
        }
    }

    private void PushMessage(Log.Level level, ReadOnlySpan<char> message)
    {
        lock(loggerLock)
        {
            DateTime currentTime = DateTime.Now;

            Span<char> header = stackalloc char[128];
            int headerLength = WriteHeader(header, currentTime, level);

            int totalLength = headerLength + message.Length;

            if(EchoToConsole)
            {
                Span<char> consoleLine = stackalloc char[headerLength + message.Length + Environment.NewLine.Length];
                header[0..headerLength].CopyTo(consoleLine);
                message.CopyTo(consoleLine.Slice(headerLength));
                Environment.NewLine.AsSpan().CopyTo(consoleLine.Slice(headerLength + message.Length));
                Console.Out.Write(consoleLine);
            }

            if (pointer + totalLength <= data.Length)
            {
                header[..headerLength].CopyTo(data.AsSpan(pointer));
                message.CopyTo(data.AsSpan(pointer + headerLength));
                logs.Add((pointer, totalLength, level));
                pointer += totalLength;
            }
            else
            {
                Flush();
                header[..headerLength].CopyTo(data.AsSpan(0));
                message.CopyTo(data.AsSpan(headerLength));
                logs.Add((0, totalLength, level));
                pointer = totalLength;
            }
        }
    }
    private int WriteHeader(Span<char> dest, DateTime timestamp, Log.Level logLevel)
    {
        int written = 0;

        dest[written++] = '[';

        if (timestamp.TryFormat(dest.Slice(written), out int timeWritten, TimestampFormat, System.Globalization.CultureInfo.InvariantCulture))
            written += timeWritten;

        dest[written++] = ']';
        dest[written++] = ' ';

        var name = this.name ?? Luminix.Threading.Thread.CurrentThread?.Name ?? "EngineThread";
        Span<char> threadName = stackalloc char[name.Length + 2];
        threadName[0] = '[';
        name.CopyTo(threadName[1..]);
        threadName[^1] = ']';
        
        threadName.CopyTo(dest.Slice(written));
        written += threadName.Length;

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
