using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Luminix;

public sealed class Logger
{
    public readonly ref struct Log
    {
        public enum Type
        {
            Info,
            Warning,
            Error,
        }

        private readonly int logIndex;
        private readonly Logger logger;

        internal Log(Logger logger, int logIndex)
        {
            this.logger = logger;
            this.logIndex = logIndex;
        }

        public Log Append(string value)
        {
            if (logger == null) return this;
            if (value == null) return this;

            ReadOnlySpan<char> src = value.AsSpan();
            src.CopyTo(logger.data.AsSpan(logger.pointer)); // no alloc — spans over existing memory
            logger.pointer += src.Length;
            AddLength(src.Length);

            return this;
        }

        public Log Append(double value)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, default, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }
        public Log Append(double value, ReadOnlySpan<char> format)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }

        public Log Append(long value)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, default, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }
        public Log Append(long value, ReadOnlySpan<char> format)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }

        public Log Append(ulong value)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, default, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }
        public Log Append(ulong value, ReadOnlySpan<char> format)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }

        public Log Append(DateTime value)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, default, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }
        public Log Append(DateTime value, ReadOnlySpan<char> format)
        {
            if (logger == null) return this;

            Span<char> dest = logger.data.AsSpan(logger.pointer);
            if (value.TryFormat(dest, out int written, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                logger.pointer += written;
                AddLength(written);
            }

            return this;
        }

        public Log Append(char value)
        {
            if (logger == null) return this;

            logger.data[logger.pointer++] = value;
            AddLength(1);

            return this;
        }

        public Log Append(bool value)
        {
            if (logger == null) return this;

            if (value)
            {
                logger.data[logger.pointer++] = 'T';
                logger.data[logger.pointer++] = 'r';
                logger.data[logger.pointer++] = 'u';
                logger.data[logger.pointer++] = 'e';
                AddLength(4);
            }
            else
            {
                logger.data[logger.pointer++] = 'F';
                logger.data[logger.pointer++] = 'a';
                logger.data[logger.pointer++] = 'l';
                logger.data[logger.pointer++] = 's';
                logger.data[logger.pointer++] = 'e';
                AddLength(5);
            }

            return this;
        }

        public Log Append(object value)
        {
            if (logger == null) return this;
            
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
                case long l:
                    Append(l);
                    break;
                case ulong ul:
                    Append(ul);
                    break;
                case double d:
                    Append(d);
                    break;
                case bool b:
                    Append(b);
                    break;
                case char c:
                    Append(c);
                    break;
                case DateTime dt:
                    Append(dt);
                    break;
                default:
                    Append(value.ToString());
                    break;
            }

            return this;
        }

        public void End()
        {
            Append('\n');
        }

        private void AddLength(int count)
        {
            var entry = logger.logs[logIndex];
            logger.logs[logIndex] = (entry.Start, entry.Length + count);
        }
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


    private Logger()
    {
        logs = new(500);
        data = new char[250000];
        buffer = new char[250000];
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
    private char[] buffer;

    private int pointer;

    private string dateTimeFormat;

    public void SetDateTimeFormat(string format)
    {
        dateTimeFormat = format;
    }

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

    public Log NewLog(bool timestamp, bool threadInfo, Log.Type type)
    {
        int logIndex = logs.Count;
        logs.Add((pointer, 0));
        Log log = new Log(this, logIndex);

        if(timestamp)
        {
            log.Append('[');
            log.Append(DateTime.Now, dateTimeFormat);
            log.Append("] ");
        }

        if(threadInfo)
        {
            log.Append(threadName);
            log.Append(' ');
        }

        switch(type)
        {
            case Log.Type.Info:
                log.Append("[INFO] : ");
            break;
            case Log.Type.Warning:
                log.Append("[WARNING] : ");
            break;
            case Log.Type.Error:
                log.Append("[ERROR] : ");
            break;
        }

        return log;
    }

    public Log NewLogIf(bool condition, bool timestamp, bool threadInfo, Log.Type type)
    {
        if(condition)
            return NewLog(timestamp, threadInfo, type);

        return new Log(null, 0);
    }


    public void Flush()
    {
        using (StreamWriter writer = new StreamWriter(path, append: true))
        {
            writer.Write(data, 0, pointer);
        }
        pointer = 0;
        logs.Clear();
    }

    public void Clear()
    {
        Console.Clear();
        pointer = 0;
        logs.Clear();
    }
}
