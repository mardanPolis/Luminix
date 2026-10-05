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

        //Getting XnbType
        byte[] header = new byte[512];
        int bytesRead = Stream.Read(header, 0, header.Length);
        Stream.Position = 0;

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

        Type = typeName switch
        {
            var t when t.Contains("SpriteFontReader") => XnbAsset.XnbType.Font,
            var t when t.Contains("EffectReader") => XnbAsset.XnbType.Shader,
            _ => throw new InvalidDataException($"This file is not a valid XNB file or has an unrecognized format and only supports SpriteFonts or Effects")
        };
    }
    public XnbType Type { get; }
    public MemoryStream Stream { get; private set; }

    protected override void OnDispose()
    {
        Stream.Dispose();
        Stream = null;
    }
}

