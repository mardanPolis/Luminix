using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Luminix;

public static partial class Logger
{
    public readonly ref struct Log
    {
        internal Log(int start, int length, Level level)
        {
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
        private int startIndex { get; }
        public int Length { get; }
        public Log.Level LogLevel { get; }
        public ReadOnlySpan<char> Message => data.AsSpan(startIndex, Length);
    }

    internal static string LogDir { get; }

    static Logger()
    {
        LogDir = Path.Combine(AppContext.BaseDirectory, "logs");

        loggerLock = new();
        logs = new(500);
        data = new char[256000];
        logBuffer = new char[Math.Min(data.Length, 16384)];

        Directory.CreateDirectory(LogDir);

        logPath = Path.Combine(Logger.LogDir, $"{Application.StartTime.ToString("yyyy-MM-dd HH-mm-ss")}.log");
        File.WriteAllText(logPath, string.Empty);
    }

    private static readonly object loggerLock;
    private static readonly string logPath = null;

    private static readonly List<(int Start, int Length, Log.Level level)> logs;
    private static char[] data;
    private static char[] logBuffer; //FIXME : unsafe if log is being created while another log is being created.

    private static int pointer;

    public static string TimestampFormat { get; set; }
    public static bool EchoToConsole { get; set; }

    public static void NewLog(Log.Level logLevel, string message) 
    {
        var builder = new LogBuilder(logLevel);
        builder.AppendLiteral(message);
        builder.End();
    }
    public static void NewLog(Log.Level logLevel, [InterpolatedStringHandlerArgument("logLevel")] ref LogBuilder message)
    {
        message.End();
    }
    public static void NewLog<T>(Log.Level logLevel, ReadOnlySpan<T> collection)
    {
        var builder = new LogBuilder(logLevel);

        builder.AppendFormatted("ReadOnlySpan<");
        builder.AppendFormatted(typeof(T).Name);
        builder.AppendFormatted(">");

        if (collection.Length == 0)
        {
            builder.AppendFormatted(" is empty");
            builder.End();
            return;
        }

        builder.AppendFormatted(" contains:");

        for (int i = 0; i < collection.Length; i++)
        {
            builder.AppendFormatted('\n');

            builder.AppendFormatted(i);
            builder.AppendFormatted(" - ");

            if (collection[i] is ISpanFormattable formattable)
                builder.AppendFormatted(formattable);
            else
                builder.AppendFormatted(collection[i]);
        }

        builder.End();
    }

    public static void NewLog<T>(Log.Level logLevel, IEnumerable<T> collection)
    {
        var builder = new LogBuilder(logLevel);

        builder.AppendFormatted(collection.GetType());

        if (collection is IReadOnlyList<T> list)
        {
            if (list.Count == 0)
            {
                builder.AppendFormatted(" is empty");
                builder.End();
                return;
            }

            builder.AppendFormatted(" contains:");

            for (int i = 0; i < list.Count; i++)
            {
                builder.AppendFormatted('\n');

                builder.AppendFormatted(i);
                builder.AppendFormatted(" - ");

                if (list[i] is ISpanFormattable formattable)
                    builder.AppendFormatted(formattable);
                else
                    builder.AppendFormatted(list[i]);
            }
        }
        else
        {
            int i = 0;
            foreach (var item in collection)
            {
                if(i == 0)
                    builder.AppendFormatted(" contains: \n");

                builder.AppendFormatted(i++);
                builder.AppendFormatted(" - ");

                if (item is ISpanFormattable formattable)
                    builder.AppendFormatted(formattable);
                else
                    builder.AppendFormatted(item?.ToString() ?? "null");

                builder.AppendFormatted('\n');
            }

            if(i == 0)
                builder.AppendFormatted(" is empty");
        }

        builder.End();
    }


    public static void NewLog<TDictionary, T1, T2>(Log.Level logLevel, TDictionary collection) where TDictionary : IReadOnlyDictionary<T1, T2>
    {
        var builder = new LogBuilder(logLevel);

        var collectionType = typeof(TDictionary);
        builder.AppendFormatted(collectionType);
        if(collection == null || collection.Count <= 0)
        {
            builder.AppendFormatted(" is empty");
        }
        else
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
        builder.End();
    }

    public static void Flush()
    {
        lock(loggerLock)
        {
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                foreach(Log log in Logger.Logs)
                {
                    writer.WriteLine(log.Message);
                } 
            }
            pointer = 0;
            logs.Clear();
        }
    }
    public static void Clear()
    {
        lock(loggerLock)
        {
            Console.Clear();
            pointer = 0;
            logs.Clear();
        }
    }

    private static void PushMessage(Log.Level level, ReadOnlySpan<char> message)
    {
        lock(loggerLock)
        {
            DateTime currentTime = DateTime.Now;

            Span<char> header = stackalloc char[128];
            int headerLength = WriteHeader(header, currentTime, level);

            int totalLength = headerLength + message.Length;

            if(EchoToConsole)
            {
                switch(level)
                {
                    case Log.Level.Info:
                        Console.ResetColor();
                    break;
                    case Log.Level.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                    case Log.Level.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                    break;
                }

                Span<char> consoleLine = stackalloc char[headerLength + message.Length + Environment.NewLine.Length];
                header[0..headerLength].CopyTo(consoleLine);
                message.CopyTo(consoleLine.Slice(headerLength));
                Environment.NewLine.AsSpan().CopyTo(consoleLine.Slice(headerLength + message.Length));
                Console.Out.Write(consoleLine);
                Console.ResetColor();
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
    private static int WriteHeader(Span<char> dest, DateTime timestamp, Log.Level logLevel)
    {
        int written = 0;

        dest[written++] = '[';

        if (timestamp.TryFormat(dest.Slice(written), out int timeWritten, TimestampFormat, System.Globalization.CultureInfo.InvariantCulture))
            written += timeWritten;

        dest[written++] = ']';
        dest[written++] = ' ';

        var name = Luminix.Threading.Thread.CurrentThread?.Name ?? "EngineThread";
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
