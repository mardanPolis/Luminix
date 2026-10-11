
using System;
using System.Runtime.CompilerServices;

namespace Luminix;

public static partial class Logger
{
    [InterpolatedStringHandler]
    public ref struct LogBuilder
    {
        private char[] buffer;
        private int pointer;

        private Log.Level level;

        internal LogBuilder(Log.Level level, int bufferSize)
        {
            this.buffer = Logger.GetBuffer(bufferSize);
            this.level = level;
        }

        public LogBuilder(Log.Level level)
        {
            this.buffer = Logger.GetBuffer();
            this.level = level;
        }

        public LogBuilder(int literalLength, int formattedCount, Log.Level level)
        {
            this.buffer = Logger.GetBuffer();
            this.level = level;
        }

        public bool AppendLiteral(string value)
        {
            return Add(value.AsSpan());
        }
        public bool AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            return Add(value, default);
        }
        public bool AppendFormatted<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            try
            {
                return Add(value, format);
            }
            catch (FormatException)
            {
                FormatError(format);
                return false; // stop appending further arguments once a format error hits
            }
        }
        public bool AppendFormatted(object value)
        {
            if(value == null)
                return Add("null".AsSpan());

            if(value is Type type)
                return AddType(type);

            if(value is Exception ex)
            {
                return Add(ex.GetType().Name) && Add(", ") && Add(ex.Message) && Add("\n") && Add(ex.StackTrace) && (ex.InnerException == null || (Add("Inner Exception: ") && AppendFormatted(ex.InnerException)));
            }

            return Add(value.ToString());
        }
        public bool AppendFormatted(ReadOnlySpan<char> value)
        {
            return Add(value);
        }
        public bool AppendFormatted(ReadOnlySpan<char> value, uint length)
        {
            if (length < value.Length)
                value = value[..(int)length];

            return Add(value);
        }

        public void End()
        {
            if (buffer is null)
                return;

            Logger.PushMessage(level, buffer.AsSpan(0, pointer));
            Dispose();
        }

        public void Dispose()
        {
            if (buffer is null)
                return;

            Logger.ReturnBuffer(buffer);
            buffer = null;
        }

        private bool Add(ReadOnlySpan<char> value)
        {
            if(buffer is null)
                return false;

            int remaining = Logger.GetBufferSize(buffer) - pointer;

            if (value.Length > remaining)
            {
                value.Slice(0, remaining).CopyTo(buffer.AsSpan(pointer));
                pointer += remaining;

                LengthError();
                return false;
            }

            value.CopyTo(buffer.AsSpan(pointer));
            pointer += value.Length;
            return true;
        }
        private bool Add<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            if(buffer is null)
                return false;

            Span<char> dest = buffer.AsSpan(pointer);

            if (value.TryFormat(dest, out int written, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                pointer += written;
                return true;
            }

            Span<char> scratch = stackalloc char[128];
            if (!value.TryFormat(scratch, out int fullWritten, format, System.Globalization.CultureInfo.InvariantCulture))
            {
                LengthError();
                return false;
            }

            int remaining = Logger.GetBufferSize(buffer) - pointer;
            int toCopy = Math.Min(fullWritten, remaining);

            if (toCopy > 0)
            {
                scratch.Slice(0, toCopy).CopyTo(buffer.AsSpan(pointer));
                pointer += toCopy;
            }

            LengthError();
            return false;
        }
        private bool AddType(Type type)
        {
            if(buffer is null)
                return false;

            if (TypeRegistry.IsGenericType(type))
            {
                HandleGenericType(type);
                return true;
            }

            return Add(type.Name);
        }
        private void HandleGenericType(Type type)
        {
            AppendFormatted(type.Name, (uint)(type.Name.Length - 2));
            AppendFormatted('<');

            var genericArgs = TypeRegistry.GetGenericArguments(type);
            for (int i = 0; i < genericArgs.Length; i++)
            {
                if (i > 0)
                    AppendFormatted(", ");

                Type argType = genericArgs[i];
                if (argType.IsGenericType)
                    HandleGenericType(argType); // recurse for nested generics
                else
                    AppendFormatted(argType.Name);
            }

            AppendFormatted('>');
        }

        private void LengthError()
        {
            if(buffer is null)
                return;

            ReadOnlySpan<char> chars = "...[Length Error -> log exceeded length of 16384 characters]";

            var length = chars.Length;
            chars.CopyTo(buffer.AsSpan(pointer - length));
            End();
        }
        private void FormatError(ReadOnlySpan<Char> format)
        {
            AppendFormatted(" [Format Error -> '");
            AppendFormatted(format);
            AppendFormatted("']");
        }
    }
}