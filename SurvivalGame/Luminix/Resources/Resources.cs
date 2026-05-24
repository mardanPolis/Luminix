
using System;
using Luminix.Identifiers;

namespace Luminix.Resources;

public abstract class Resource
{
    public readonly string Name;
    internal readonly ID ID;
    internal Resource(string name, ID id)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        Name = name;
        ID = id;
    }
}

public class TextureResource : Resource
{
    public readonly int Height;
    public readonly int Width;

    internal TextureResource(string name, ID id, int width, int height) 
        : base(name, id)
    {
        Width = width;
        Height = height;
    }
}

public class FontResource : Resource
{
    internal FontResource(string name, ID id)
        : base(name, id) {}
}

public class ShaderResource : Resource
{
    internal ShaderResource(string name, ID id)
        : base(name, id) {}
}

public class AudioResource : Resource
{
    public readonly double Duration;
    internal AudioResource(string name, ID id, double duration) 
        : base(name, id)
    {
        Duration = duration;
    }
}