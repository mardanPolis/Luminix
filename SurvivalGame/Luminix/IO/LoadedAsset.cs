using System;
using System.IO;

namespace Luminix.IO;

internal struct LoadedAsset(string assetType,  Stream assetStream, string assetName)
{
    public static class GenericTypes
    {
        public const string Texture = "Texture";
        public const string Effect = "Effect";
        public const string Font = "Font";
        public const string Audio = "Audio";

    }
    public readonly string AssetType = assetType;
    public readonly Stream AssetStream = assetStream;
    public readonly string AssetName = assetName;
}
