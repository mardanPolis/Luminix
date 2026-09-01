using System;

namespace Luminix;

public abstract class Asset
{
    internal protected Asset(string identifier)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));

        Identifier = identifier.ToLower();
    }
    public string Identifier { get; }
}