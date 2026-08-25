using System;
using System.Collections;
using System.Collections.Generic;

namespace Luminix;

public sealed partial class Logger
{
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

}