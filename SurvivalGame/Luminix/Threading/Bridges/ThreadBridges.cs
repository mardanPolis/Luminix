using System;
using System.Collections.Concurrent;

namespace Luminix.Threading.Bridges;

internal static class ThreadBridges
{
    private readonly struct ThreadPair : IEquatable<ThreadPair>
    {
        public string ThreadA { get; }
        public string ThreadB { get; }

        public ThreadPair(string threadA, string threadB)
        {
            if (string.CompareOrdinal(threadA, threadB) <= 0)
            {
                ThreadA = threadA;
                ThreadB = threadB;
            }
            else
            {
                ThreadA = threadB;
                ThreadB = threadA;
            }
        }

        public bool Equals(ThreadPair other) =>
            ThreadA == other.ThreadA && ThreadB == other.ThreadB;

        public override bool Equals(object obj) =>
            obj is ThreadPair other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(ThreadA, ThreadB);

        public override string ToString() => $"({ThreadA}, {ThreadB})";
    }

    private static readonly ConcurrentDictionary<(ThreadPair, Type), Lazy<object>> bridges = new ConcurrentDictionary<(ThreadPair, Type), Lazy<object>>();

    public static TBridge GetBridge<TBridge>(string targetThreadName) where TBridge : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetThreadName);

        var threadPair = new ThreadPair(Thread.CurrentThread.Name, targetThreadName);
        var bridgeType = typeof(TBridge);

        if(threadPair.ThreadA == threadPair.ThreadB)
            throw new ArgumentException($"Cannot create a bridge from thread '{Thread.CurrentThread.Name}' to itself.", nameof(targetThreadName));

        var lazyBridge = bridges.GetOrAdd(
            (threadPair, bridgeType),
            _ => new Lazy<object>(() => new TBridge(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication));


        return (TBridge)lazyBridge.Value;
    }
}