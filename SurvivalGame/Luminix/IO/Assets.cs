using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Luminix.IO;

public abstract class Asset
{
    internal protected Asset(string name, AssetIO.AssetFile? assetFile)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name;
        AssetFile = assetFile;
    }
    public string Name { get; }
    public AssetIO.AssetFile? AssetFile { get; }

    public bool IsGenerated => !AssetFile.HasValue;
}

public sealed class TextureAsset : Asset
{
    internal TextureAsset(string name, Color[] colors, Vector2 size) : this(name, null, colors, size) {}
    internal TextureAsset(string name, AssetIO.AssetFile? assetFile, Color[] colors, Vector2 size) : base(name, assetFile)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));

        Colors = colors;
        Size = size;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
}

public sealed class AtlasAsset : Asset
{
    public readonly record struct Region(string Name, Rectangle Rectangle);

    internal AtlasAsset(string name, Color[] colors, Vector2 size, Region[] regions) : this(name, null, colors, size, regions) {}
    internal AtlasAsset(string name, AssetIO.AssetFile? assetFile, Color[] colors, Vector2 size, Region[] regions) : base(name, assetFile)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        ArgumentNullException.ThrowIfNull(regions, nameof(regions));

        Colors = colors;
        Size = size;
        Regions = regions;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
    public Region[] Regions { get; }
}

public sealed class AudioAsset : Asset
{
    internal AudioAsset(string name, SoundEffect soundEffect) : this(name, null, soundEffect) {}
    internal AudioAsset(string name, AssetIO.AssetFile? assetFile, SoundEffect soundEffect) : base(name, assetFile)
    {
        ArgumentNullException.ThrowIfNull(soundEffect, nameof(soundEffect));

        SoundEffect = soundEffect;
    }

    public SoundEffect SoundEffect { get; }
}

public sealed class StreamAsset : Asset
{
    public enum AssetType
    {
        Texture,
        Font,
        Audio,
        Shader
    }

    internal StreamAsset(string name, Stream stream, AssetType type) : this(name, null, stream, type) {}
    internal StreamAsset(string name, AssetIO.AssetFile? assetFile, Stream stream, AssetType type) : base(name, assetFile)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if (stream is MemoryStream memoryStream)
        {
            Stream = memoryStream;
        }
        else
        {
            Stream = new MemoryStream();
            stream.CopyTo(Stream);
            Stream.Position = 0; 
        }

        Type = type;
    }
    public AssetType Type { get; }
    public MemoryStream Stream { get; }
}

public sealed class PackageAsset : Asset
{
    public PackageAsset(string name, AssetIO.AssetFile? assetFile) : base(name, null)
    {
        throw new NotImplementedException(nameof(PackageAsset));
    }
}

public abstract class DataAsset : Asset
{
    public DataAsset(string name, AssetIO.AssetFile? assetFile) : base(name, assetFile)
    {
    }
}