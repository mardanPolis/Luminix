using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Luminix;

public sealed partial class Logger
{
    public LogEnumerator GetEnumerator() => new LogEnumerator(this);

    public ref struct LogEnumerator : IEnumerator<Log>
    {
        internal LogEnumerator(Logger logger)
        {
            this.logger = logger;
            this.index = -1;
            this.current = default;
            Monitor.Enter(logger.loggerLock);
        }

        private Logger logger;
        private int index;
        private Log current;

        public readonly Log Current => current;
        readonly object IEnumerator.Current => throw new NotSupportedException(nameof(IEnumerator.Current));

        public bool MoveNext()
        {
            if (index + 1 >= logger.logs.Count)
                return false;

            index++;
            (int start, int length, Log.Level level) log = logger.logs[index];
            current = new Log(logger, log.start, log.length, log.level);
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default;
        }

        public void Dispose()
        {
            Monitor.Exit(logger.loggerLock);
        }
    }

}