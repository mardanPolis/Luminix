using System;
using System.IO;
using Verse;

namespace Verse.IO;

public class AssetSerializer
{
    public MemoryStream LoadTexture(string location)
    {
        string extension = Path.GetExtension(location).ToLower().Replace(".", "");

        switch (extension)
        {
            case "png":
                return LoadTexture(location, "png");
            case "jpg":
                return LoadTexture(location, "jpg");
            case "jpeg":
                return LoadTexture(location, "jpeg");
            case "asset":
                return LoadTextureAsset(location);
            default:
                Logger.Get().Wrn($"Unsupported texture format: {extension} at {location}");
                return null;
        }
    }

    private MemoryStream LoadTexture(string location, string fileType)
    {
        try
        {
            using (FileStream fs = File.OpenRead(location + "." + fileType))
            {
                MemoryStream ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Position = 0;
                
                return ms;
            }
        }
        catch (Exception ex)
        {
            Logger.Get().Err($"Failed to stream file, {location + "." + fileType}: {ex.Message}");
            return null;
        }
    }

    private MemoryStream LoadTextureAsset(string location)
    {
        throw new NotImplementedException("LoadTextureAsset(string location)");
    }
}
