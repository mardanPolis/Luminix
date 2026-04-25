using System;
using System.IO;
using System.Collections.Generic;
using Verse.Collections;
namespace Verse.IO;

public class AssetSerializer
{
    private DenseList<string> loadedLocations = new DenseList<string>();
    private List<LoadedAsset> pendingAssets = new List<LoadedAsset>();

    public void LoadTexture(string location)
    {
        if (string.IsNullOrEmpty(location))
            throw new ArgumentNullException("location");

        string extension = Path.GetExtension(location).ToLower().Replace(".", "");
        string fileName = Path.GetFileNameWithoutExtension(location);

        try
        {
            MemoryStream memoryStream = null;
            switch (extension)
            {
                case "png":
                    memoryStream = LoadTexture(location, "png");
                    break;
                case "jpg":
                    memoryStream =  LoadTexture(location, "jpg");
                    break;
                case "jpeg":
                    memoryStream =  LoadTexture(location, "jpeg");
                    break;
                case "xnb":
                    memoryStream =  LoadTexture(location, "xnb");
                    break;
                case "asset":
                    memoryStream =  LoadTextureAsset(location);
                    break;
                default:
                    Logger.Get().Wrn($"Unsupported texture format: {extension} at {location}");
                    return;
            }

            pendingAssets.Add(new LoadedAsset(LoadedAsset.Type.Texture, memoryStream, fileName));
            loadedLocations.Add(location);
        }
        catch(Exception ex)
        {
            Logger.Get().Err($"Failed to load texture at location: {location}, exception: {ex}");
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


    internal LoadedAsset[] GetPendingAssets()
    {
        var array = pendingAssets.ToArray();
        pendingAssets.Clear();
        return array;
    }

}
