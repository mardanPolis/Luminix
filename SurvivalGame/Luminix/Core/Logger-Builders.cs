
using System;
using System.Runtime.CompilerServices;

namespace Luminix;

public sealed partial class Logger
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
    
    [InterpolatedStringHandler]
    public ref struct LogBuilder
    {
        private readonly bool shouldLog;
        private Log log;

        public LogBuilder(int literalLength, int formattedCount, Logger logger, out bool shouldAppend)
        {
            shouldLog = logger.TryCreateLog(Log.Type.Info, out log);
            shouldAppend = shouldLog;

            if (shouldLog)
            {
                log.AppendTimestamp(logger.TimestampFormat);
                log.AppendThreadName(logger.threadName);
                log.AppendType(Log.Type.Info);
            }
        }

        public void AppendLiteral(string value)
        {
            log.Append(value.AsSpan());
        }

        public void AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            log.Append(value, default);
        }

        public void AppendFormatted<T>(T value, string format) where T : ISpanFormattable
        {
            log.Append(value, format);
        }

        public void AppendFormatted(string value)
        {
            log.Append(value.AsSpan());
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            log.Append(value);
        }

        internal void Push()
        {
            if (shouldLog) log.End();
        }
    }

    [InterpolatedStringHandler]
    public ref struct WrnBuilder
    {
        private readonly bool shouldLog;
        private Log log;

        public WrnBuilder(int literalLength, int formattedCount, Logger logger, out bool shouldAppend)
        {
            shouldLog = logger.TryCreateLog(Log.Type.Warning, out log);
            shouldAppend = shouldLog;

            if (shouldLog)
            {
                log.AppendTimestamp(logger.TimestampFormat);
                log.AppendThreadName(logger.threadName);
                log.AppendType(Log.Type.Warning);
            }
        }

        public void AppendLiteral(string value)
        {
            log.Append(value.AsSpan());
        }

        public void AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            log.Append(value, default);
        }

        public void AppendFormatted<T>(T value, string format) where T : ISpanFormattable
        {
            log.Append(value, format);
        }

        public void AppendFormatted(string value)
        {
            log.Append(value.AsSpan());
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            log.Append(value);
        }

        internal void Push()
        {
            if (shouldLog) log.End();
        }
    }

    [InterpolatedStringHandler]
    public ref struct ErrBuilder
    {
        private readonly bool shouldLog;
        private Log log;

        public ErrBuilder(int literalLength, int formattedCount, Logger logger, out bool shouldAppend)
        {
            shouldLog = logger.TryCreateLog(Log.Type.Error, out log);
            shouldAppend = shouldLog;

            if (shouldLog)
            {
                log.AppendTimestamp(logger.TimestampFormat);
                log.AppendThreadName(logger.threadName);
                log.AppendType(Log.Type.Error);
            }
        }

        public void AppendLiteral(string value)
        {
            log.Append(value.AsSpan());
        }

        public void AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            log.Append(value, default);
        }

        public void AppendFormatted<T>(T value, string format) where T : ISpanFormattable
        {
            log.Append(value, format);
        }

        public void AppendFormatted(string value)
        {
            log.Append(value.AsSpan());
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            log.Append(value);
        }

        internal void Push()
        {
            if (shouldLog) log.End();
        }
    }

    private enum LogDropReason
    {
        AlreadyCreatingLog,
        LoggerLocked,
    }
}