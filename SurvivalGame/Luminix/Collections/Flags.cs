using System.Collections.Concurrent;

namespace Luminix.Collections;

public class Flags : IReadOnlyFlags
{
    public Flags()
    {
        flags = new ConcurrentDictionary<string, bool>();
    }

    private ConcurrentDictionary<string, bool> flags;

    public bool this[string flag]
    {
        get => flags.TryGetValue(flag, out bool value) && value;
        set => flags[flag] = value;
    }

    public bool AppendFlag(string flag, bool value) => flags.TryAdd(flag, value);
    public bool RemoveFlag(string flag) => flags.TryRemove(flag, out _);
    
    public bool HasFlag(string flag) => flags.TryGetValue(flag, out bool value) && value;
    public bool? GetFlag(string flag) => flags.TryGetValue(flag, out bool value) ? value : null;
    public void SetFlag(string flag, bool value)
    {
        if(flags.ContainsKey(flag))
            flags[flag] = value;
    }

    public void Clear() => flags.Clear();
}