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
 
        //Getting XnbType (reads the header and type-reader table, also works for LZ4-compressed files)
        var xnb = XnbContainer.Open(Data);
 
        Type = xnb.PrimaryReader switch
        {
            var t when t.Contains("SpriteFontReader") => XnbType.Font,
            var t when t.Contains("EffectReader")     => XnbType.Shader,
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
 
