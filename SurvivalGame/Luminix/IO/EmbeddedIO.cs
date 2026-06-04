using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Luminix.Workspaces;

namespace Luminix.IO;

public sealed class EmbeddedIO : Workspace.Service
{
    public IEnumerable<StreamAsset> EnumerateAssets(Assembly assembly, bool silent = false)
    {
        if(assembly == null)
        {
            Logger.Get()?.Err("Unable to enumerate assets: null assembly.");
            yield break;
        }

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            string name = "";
            MemoryStream memStream = null;
            StreamAsset.AssetType assetType = StreamAsset.AssetType.Texture;

            try
            {
                Stream stream = OpenStream(assembly, resourceName);
                if (stream is null) continue;

                memStream = new MemoryStream((int)stream.Length);
                stream.CopyTo(memStream);
                memStream.Position = 0;

                if(!InputOutputUtils.DetectXMBAssetType(memStream, out assetType))
                {
                    if (!silent)
                        Logger.Get()?.Err($"Embedded resource '{resourceName}' in '{assembly.GetName().Name}' is not a valid XNB file or has an unrecognized format.");
                    continue;
                }

                name = resourceName.Contains('.')
                    ? resourceName[..resourceName.LastIndexOf('.')]
                        .Split('.')[^1]
                    : resourceName;
            }
            catch(Exception ex)
            {
                Logger.Get()?.Err($"Unable to load embedded resource '{resourceName}' from '{assembly.GetName().Name}': {ex.Message}");
                continue;
            }

            yield return new StreamAsset(name, memStream, assetType);
        }
    }

    public Stream OpenStream(Assembly assembly, string resourceName)
    {
        return assembly.GetManifestResourceStream(resourceName);
    }
}