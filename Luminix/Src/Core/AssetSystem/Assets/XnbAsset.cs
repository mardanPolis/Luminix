using System;
using System.IO;

namespace Luminix;

public sealed class XnbAsset : Asset
{
    public enum XnbType
    {
        Font,
        Shader
    }

    internal XnbAsset(string identifier, Stream stream) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if (stream is MemoryStream ms)
        {
            Data = ms.ToArray();
        }
        else
        {
            using MemoryStream buffer = new MemoryStream();
            stream.CopyTo(buffer);
            Data = buffer.ToArray();
        }

        //Getting XnbType
        int bytesRead = Math.Min(512, Data.Length);

        int i = 10;
        while (i < bytesRead && (Data[i] & 0x80) != 0) i++;
        i++;

        int strLen = 0, shift = 0;
        while (i < bytesRead)
        {
            byte b = Data[i++];
            strLen |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }

        string typeName = System.Text.Encoding.UTF8.GetString(Data, i, Math.Min(strLen, bytesRead - i));

        Type = typeName switch
        {
            var t when t.Contains("SpriteFontReader") => XnbAsset.XnbType.Font,
            var t when t.Contains("EffectReader") => XnbAsset.XnbType.Shader,
            _ => throw new InvalidDataException("This file is not a valid XNB file or has an unrecognized format and only supports SpriteFonts or Effects")
        };
    }
    public XnbType Type { get; }
    public byte[] Data { get; private set; }

    protected override void OnDispose()
    {
        Data = null;
    }
}

