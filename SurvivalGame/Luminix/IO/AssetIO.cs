using System;
using System.IO;
using System.Collections.Generic;
using Luminix.Collections;
using System.Linq;
namespace Luminix.IO;

internal class AssetIO
{
    private DenseList<string> loadedPaths = new DenseList<string>();
    private List<LoadedAsset> pendingAssets = new List<LoadedAsset>();

    static readonly string[] allowedExtensions = { "png", "jpg", "xnb", "jpeg", "package" };
    static readonly string formattedList = string.Join(", ", allowedExtensions.Select(ext => $"'{ext}'"));
    static readonly HashSet<string> allowedExtensionsSet = allowedExtensions.ToHashSet();

    private bool TryGetLoadablePath(string path, out string _path)
    {
        _path = path;

        var extension = Path.GetExtension(path).ToLower().TrimStart('.') ?? "";

        if (string.IsNullOrEmpty(extension)) // getting extension
        {
            string fullPath = "";
            for (int i = 0; i < extension.Length; i++) {
                fullPath = path + "." + allowedExtensions[i];
                if (File.Exists(fullPath))
                {
                    _path = fullPath;
                    return true;
                }
            }

            Logger.Get().Err($"Could not find '{path}' with any known extension. Known extensions -> [{formattedList}] ");
            _path = "";
            return false;
        }
        else //validating extension
        {
            if (!allowedExtensionsSet.Contains(extension))
            {
                Logger.Get().Err($"Unsupported extension '.{extension}' for path: {path}");
                return false;
            }

            if (!File.Exists(path))
            {
                Logger.Get().Err($"File not found: {path}");
                return false;
            }

            
            _path = path;
            return true;
        }
    }

    public void Load(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if(!TryGetLoadablePath(path, out path))
            return;

        string fileName = Path.GetFileNameWithoutExtension(path);

        try
        {
            LoadStream(path, out MemoryStream memoryStream, out string header);
            pendingAssets.Add(new LoadedAsset(LoadedAsset.GenericTypes.Texture, memoryStream, fileName));
        }
        catch(Exception ex)
        {
            Logger.Get().Err($"Failed to load asset at location: {path}, exception: {ex}");
        }
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
