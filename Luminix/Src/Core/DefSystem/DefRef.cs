using System;

namespace Luminix.Json;

public class DefRef<T> where T : Def
{
    public string Name { get; }

    private T _resolved;
    private bool _isResolved;

    public DefRef(string name)
    {
        Name = name;
    }

    public T Value
    {
        get
        {
            if (!_isResolved)
            {
                Defs defs = Workspace.Current.GetService<Defs>();

                if (!defs.IsFinalized)
                    throw new InvalidOperationException(
                        $"Cannot resolve DefRef<{typeof(T).Name}>(\"{Name}\") before Defs is finalized.");

                _resolved = defs.GetDef<T>(Name);
                _isResolved = true;
            }
            return _resolved;
        }
    }

    public static implicit operator T(DefRef<T> r) => r is null ? null : r.Value;

    public override string ToString() => Name;
}