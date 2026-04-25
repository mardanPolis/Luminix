using System;
using System.IO;

namespace Verse.IO;

public struct LoadedAsset(LoadedAsset.Type type,  Stream assetStream, string assetName)
{
    public enum Type
    {
        Texture,
        Audio,
        Effect,
        Font,
    }

    public readonly Type AssetType = type;
    public readonly Stream AssetStream = assetStream;
    public readonly string AssetName = assetName;
}
