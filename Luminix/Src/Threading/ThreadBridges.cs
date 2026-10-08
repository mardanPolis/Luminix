using System;
using System.Collections.Concurrent;
using System.Threading;

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

    private static readonly ConcurrentDictionary<(ThreadPair, Type), object> bridges = new();
    private static readonly Lock bridgesLock = new();

    private static class GlobalBridge<TBridge> where TBridge : class
    {
        public static TBridge GlobalInstance;
        public static readonly Lock GlobalLock = new();
    }

    public static TBridge GetBridge<TBridge>(string targetThreadName) where TBridge : class, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetThreadName);

        var threadPair = new ThreadPair(Thread.CurrentThread.Name, targetThreadName);
        var bridgeType = typeof(TBridge);

        if(threadPair.ThreadA == threadPair.ThreadB)
            throw new ArgumentException($"Cannot create a bridge from thread '{Thread.CurrentThread.Name}' to itself.", nameof(targetThreadName));

        var key = (threadPair, bridgeType);

        if(bridges.TryGetValue(key, out var bridge))
            return (TBridge)bridge;

        lock (bridgesLock)
        {
            if(bridges.TryGetValue(key, out bridge))
                return (TBridge)bridge;

            bridge = new TBridge();
            bridges[key] = bridge;
            return (TBridge)bridge;
        }
    }
    public static TBridge GetGlobalBridge<TBridge>() where TBridge : class, new()
    {
        if (Volatile.Read(ref GlobalBridge<TBridge>.GlobalInstance) is { } bridge)
        {
            return bridge;
        }

        lock (GlobalBridge<TBridge>.GlobalLock)
        {
            if (GlobalBridge<TBridge>.GlobalInstance is null)
            {
                var newBridge = new TBridge();
                Volatile.Write(ref GlobalBridge<TBridge>.GlobalInstance, newBridge);
            }

            return GlobalBridge<TBridge>.GlobalInstance;
        }
    }
}

//Thread specific
//RenderBridge      : TripleBufferBridge
//RenderInfoBridge  : DoubleBufferBridge
//InputBridge       : DoubleBufferBridge
//ResourceBridge    : DoubleCommandBufferBridge
//WindowBridge      : WindowBridge

//Global
//AudioBridge : AudioBridge