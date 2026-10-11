using System;
using Luminix.Extensions;

namespace Luminix;

public abstract class Def
{
    protected virtual void Initialize() {}

    private string name = string.Empty;

    public required string Name
    {
        get => name;
        init => name = !string.IsNullOrEmpty(value) && !name.HasWhitespace()
            ? value
            : throw new ArgumentException("Name cannot be null, empty, or have whitespaces.", nameof(value));
    }
}