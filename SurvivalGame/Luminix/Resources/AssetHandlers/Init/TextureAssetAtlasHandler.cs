using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Luminix.Collections;
using Luminix.IO;
using Luminix.Resources;
using Luminix.Threading;
using Microsoft.Xna.Framework;

namespace Luminix.AssetHandlers;

internal class TextureAssetAtlasHandler
{

    private readonly WorkerThreadPool workerThreadPool;

    public TextureAssetAtlasHandler(WorkerThreadPool pool)
    {
        this.workerThreadPool = pool;
    }

    public DenseList<(string atlasName, AtlasMaker.AtlasResult atlasResult)> Run(
        DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)> loadedTextures)
    {
        var atlasGroups = new Dictionary<string, List<(string name, Color[] colors, Vector2 size)>>();

        foreach (var texture in loadedTextures)
        {
            string atlasName = ExtractAtlasName(texture.assetFile.RelativePath);
            string textureName = ExtractTextureName(texture.assetFile.RelativePath);
            if (!atlasGroups.TryGetValue(atlasName, out var group))
            {
                group = new List<(string, Color[], Vector2)>();
                atlasGroups[atlasName] = group;
            }
            group.Add((textureName, texture.colors, texture.size));
        }
        
        var groupKeys   = atlasGroups.Keys.ToArray();
        var groupValues = atlasGroups.Values.ToArray();

        var results = new DenseList<(string atlasName, AtlasMaker.AtlasResult atlasResult)>();
        var resultBag = new ConcurrentBag<(string atlasName, AtlasMaker.AtlasResult atlasResult)>();

        workerThreadPool.ParallelFor(atlasGroups.Count, index =>
        {
            var atlasMaker = new AtlasMaker();
            var atlasName = groupKeys[index];
            var textures  = groupValues[index];

            foreach(var texture in textures)
                atlasMaker.Add(texture.name, texture.colors, texture.size);

            var atlasResults = atlasMaker.Pack();
            
            if(atlasResults.Length == 1)
            {
                resultBag.Add((atlasName, atlasResults[0]));
            }
            else if(atlasResults.Length > 1)
            {
                for(int i = 0; i < atlasResults.Length; i++)
                    resultBag.Add((atlasName + "_" + i, atlasResults[i]));
            }
        });

        foreach(var result in resultBag)
            results.Add(result);

        return results;
    }

    private static string ExtractAtlasName(string relativePath)
    {
        string[] parts = relativePath.Replace('\\', '/').Split('/');
        return parts.Length > 2 ? parts[1] : Path.GetFileNameWithoutExtension(relativePath);
    }

    private static string ExtractTextureName(string relativePath)
    {
        return Path.GetFileNameWithoutExtension(relativePath);
    }
}