using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Luminix;

public static partial class Logger
{
    public static LogEnumerator GetEnumerator() => new();
    public static Enumerable Logs => default;

    public readonly ref struct Enumerable
    {
        public LogEnumerator GetEnumerator() => Logger.GetEnumerator();
    }

    public ref struct LogEnumerator : IEnumerator<Log>
    {
        public LogEnumerator()
        {
            this.index = -1;
            this.current = default;
            Monitor.Enter(loggerLock);
        }

        private int index;
        private Log current;

        public readonly Log Current => current;
        readonly object IEnumerator.Current => throw new NotSupportedException(nameof(IEnumerator.Current));

        public bool MoveNext()
        {
            if (index + 1 >= logs.Count)
                return false;

            index++;
            (int start, int length, Log.Level level) log = logs[index];
            current = new Log(log.start, log.length, log.level);
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default;
        }

        public void Dispose()
        {
            Monitor.Exit(loggerLock);
        }
    }
}