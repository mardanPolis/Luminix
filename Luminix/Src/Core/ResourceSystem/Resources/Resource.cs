using System;

namespace Luminix;

public abstract class Resource
{
    private protected Resource(string identifier)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));

        Identifier = identifier;
    }
    public bool IsDisposed { get; private set; }
    public string Identifier { get; }
    internal void Dispose()
    {
        OnDispose();
        IsDisposed = true;
    }

    private protected virtual void OnDispose() {}
}
