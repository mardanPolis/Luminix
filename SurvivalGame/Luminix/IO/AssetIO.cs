using System;
using System.IO;
using System.Collections.Generic;
using Luminix.Collections;
using Luminix.Workspaces;
using Luminix.Collections;
namespace Luminix.IO;

internal class AssetIO : Workspace.Service
{
    private DenseList<string> loadedPaths = new DenseList<string>();
    private List<LoadedAsset> pendingAssets = new List<LoadedAsset>();

    public void Load(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        string extension = Path.GetExtension(path).ToLower().Replace(".", "");
        string fileName = Path.GetFileNameWithoutExtension(path);

        if (string.IsNullOrEmpty(extension))
        {
            if(File.Exists(path))
                return;
            
            string[] extensions = { "png", "jpg", "xnb", "jpeg", "package" };
            for (int i = 0; i < extension.Length; i++) {
                string fullPath = path + "." + extensions[i];
                if (File.Exists(fullPath))
                {
                    extension = extensions[i];
                    break;
                }

                if(i == extension.Length - 1)
                {
                    Logger.Get().Wrn($"Could not find {path} with any known extension.");
                    return;
                }
            }
        }

        try
        {
            MemoryStream memoryStream;
            string header;

            if (string.IsNullOrEmpty(extension))
                LoadStream(path, out memoryStream, out header);
            else
                LoadStream(path, extension, out memoryStream, out header);

            pendingAssets.Add(new LoadedAsset(LoadedAsset.GenericTypes.Texture, memoryStream, fileName));
        }
        catch(Exception ex)
        {
            Logger.Get().Err($"Failed to load asset at location: {path}, exception: {ex}");
        }
    }

    private void LoadStream(string location, string extension, out MemoryStream _stream, out string _header)
    {
        LoadStream(location + "." + extension, out _stream, out _header);
    }

    private void LoadStream(string location, out MemoryStream _stream, out string _header)
    {
        try
        {
            using (FileStream fs = File.OpenRead(location))
            {
                byte[] headerBytes = new byte[4];
                int bytesRead = fs.Read(headerBytes, 0, 4);
                
                _header = System.Text.Encoding.ASCII.GetString(headerBytes, 0, bytesRead).Trim();

                fs.Position = 0;

                _stream = new MemoryStream();
                fs.CopyTo(_stream);
                _stream.Position = 0;
            }
        }
        catch (Exception ex)
        {
            Logger.Get().Err($"Failed to stream file, {location}: {ex.Message}");
            _header = null;
            _stream = null;
        }
    }

    internal LoadedAsset[] GetPendingAssets()
    {
        var array = pendingAssets.ToArray();
        pendingAssets.Clear();
        return array;
    }


    public IReadOnlyDenseList<string> LoadedPaths => loadedPaths;
}
