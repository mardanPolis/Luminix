using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Luminix.IO;

public abstract class Asset
{
    internal Asset(AssetIO.AssetFile assetFile)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(assetFile.FullPath, nameof(assetFile));

        File = assetFile;

    }
    public AssetIO.AssetFile File { get; }
}

public sealed class TextureAsset : Asset
{
    internal TextureAsset(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size) : base(assetFile)
    {
        Colors = colors;
        Size = size;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
}

public sealed class AudioAsset : Asset
{
    internal AudioAsset(AssetIO.AssetFile assetFile, SoundEffect soundEffect) : base(assetFile)
    {
        ArgumentNullException.ThrowIfNull(soundEffect);

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

    internal StreamAsset(AssetIO.AssetFile assetFile, Stream stream, AssetType type) : base(assetFile)
    {
        ArgumentNullException.ThrowIfNull(stream);

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
    public PackageAsset(AssetIO.AssetFile assetFile) : base(assetFile)
    {
        
    }
}