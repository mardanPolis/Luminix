using System;
using System.IO;

namespace Luminix.IO;

internal static class InputOutputUtils
{
    public static bool DetectXMBAssetType(Stream stream, out StreamAsset.AssetType _assetType)
    {
        try
        {
            byte[] header = new byte[512];
            int bytesRead = stream.Read(header, 0, header.Length);
            stream.Position = 0;

            int i = 10;

            while (i < bytesRead && (header[i] & 0x80) != 0) i++;
            i++;

            int strLen = 0, shift = 0;
            while (i < bytesRead)
            {
                byte b = header[i++];
                strLen |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }

            string typeName = System.Text.Encoding.UTF8.GetString(header, i, Math.Min(strLen, bytesRead - i));

            _assetType = typeName switch
            {
                var t when t.Contains("Texture2DReader")   => StreamAsset.AssetType.Texture,
                var t when t.Contains("SpriteFontReader")  => StreamAsset.AssetType.Font,
                var t when t.Contains("SoundEffectReader") => StreamAsset.AssetType.Audio,
                var t when t.Contains("SongReader")        => StreamAsset.AssetType.Audio,
                var t when t.Contains("EffectReader")      => StreamAsset.AssetType.Shader,
                _ => (StreamAsset.AssetType)(-1)
            };

            return (int)_assetType != -1;
        }
        catch
        {
            _assetType = StreamAsset.AssetType.Texture;
            return false;
        }
    }
}