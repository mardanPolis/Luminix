using System;

namespace Luminix;

public abstract class Asset : IDisposable
{
    private protected Asset(string identifier)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));

        Identifier = identifier.ToLower();
    }
    public bool IsDisposed { get; private set; }
    public string Identifier { get; }
    public void Dispose()
    {
        OnDispose();
        IsDisposed = true;
    }

    protected abstract void OnDispose();
}