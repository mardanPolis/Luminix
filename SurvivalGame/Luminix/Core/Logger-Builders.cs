
using System;
using System.Runtime.CompilerServices;

namespace Luminix;

public sealed partial class Logger
{
    public readonly struct Log
    {
        internal Log(Logger logger, int start, int length)
        {
            this.logger = logger;
            this.startIndex = start;
            this.Length = length;
        }

        public enum Type
        {
            Info,
            Warning,
            Error,
        }

        private Logger logger { get; }

        private int startIndex { get; }
        public int Length { get; }

        public ReadOnlySpan<char> Message => logger.data.AsSpan(startIndex, Length);
    }

    [InterpolatedStringHandler]
    public ref struct LogBuilder
    {
        private readonly Logger logger;
        private char[] buffer;
        private int pointer;

        public LogBuilder(int literalLength, int formattedCount, Logger logger, Log.Type logType, out bool shouldAppend)
        {
            shouldAppend = false;

            if(logger == null)
                return;

            shouldAppend = logger.TryCreateLog(logType);

            this.logger = logger;
            this.buffer = logger.logBuffer;

            if(!shouldAppend)
                return;

            AppendFormatted('[');
            AppendFormatted(DateTime.Now, logger.TimestampFormat);
            AppendFormatted("] ");

            AppendFormatted(logger.threadName);
            AppendFormatted(' ');

            switch(logType)
            {
                case Log.Type.Info:
                    AppendFormatted("[INFO] : ");
                break;
                case Log.Type.Warning:
                    AppendFormatted("[WARNING] : ");
                break;
                case Log.Type.Error:
                    AppendFormatted("[ERROR] : ");
                break;
            }
        }

        public LogBuilder AppendLiteral(string value)
        {
            return AppendFormatted(value.AsSpan());
        }

        public LogBuilder AppendFormatted(ReadOnlySpan<Char> value)
        {
            if (logger == null) return this;

            Add(value);
            return this;
        }
        public LogBuilder AppendFormatted(object value)
        {
            if (logger == null) return this;
            
            switch (value)
            {
                case null:
                    break;
                case string s:
                    AppendFormatted(s.AsSpan());
                    break;
                case int i:
                    AppendFormatted((long)i);
                    break;
                case ISpanFormattable f:
                    AppendFormatted(f, default);
                    break;
                default:
                    AppendFormatted(value.ToString().AsSpan());
                    break;
            }

            return this;
        }

        public LogBuilder AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            if (logger == null) return this;
 
            Add(value, default);
            return this;
        }
        public LogBuilder AppendFormatted<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            if (logger == null) return this;
 
            try
            {
                Add(value, format);
                return this;
            }
            catch (FormatException)
            {
                AppendFormatted(value).FormatError(format);
                return this;
            }
        }
 
        public void End()
        {
            if(logger == null) return;

            logger.PushMessage(buffer.AsSpan(0, pointer));
            buffer = null;
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
            AppendFormatted(" [Format Error -> '").AppendFormatted(format).AppendFormatted("']");
        }
    }

    private enum LogDropReason
    {
        AlreadyCreatingLog,
        LoggerLocked,
    }
}