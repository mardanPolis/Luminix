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

    public DenseList<AtlasAsset> Run(IList<TextureAsset> loadedTextures)
    {
        var atlasGroups = new Dictionary<string, List<TextureAsset>>();

        //grouping
        foreach (var texture in loadedTextures)
        {
            string atlasName = "Atlas";

            if(texture.AssetFile.HasValue)
                atlasName = ExtractAtlasName(texture.AssetFile.Value);

            if (!atlasGroups.TryGetValue(atlasName, out var group))
            {
                group = new List<TextureAsset>();
                atlasGroups[atlasName] = group;
            }
            group.Add(texture);
        }
        
        var groupKeys   = atlasGroups.Keys.ToArray();
        var groupValues = atlasGroups.Values.ToArray();

        var resultBag = new ConcurrentBag<AtlasAsset>();

        workerThreadPool.ParallelFor(atlasGroups.Count, index =>
        {
            var atlasMaker = new AtlasMaker();
            var atlasName = groupKeys[index];
            var textures  = groupValues[index];

            foreach(var texture in textures)
                atlasMaker.Add(texture);

            var atlasResults = atlasMaker.Pack(atlasName);
            
            for(int i = 0; i < atlasResults.Length; i++)
                resultBag.Add(atlasResults[i]);
        });

        var results = new DenseList<AtlasAsset>();
        foreach(var result in resultBag)
            results.Add(result);

        return results;
    }

    private static string ExtractAtlasName(AssetIO.AssetFile assetFile)
    {
        string[] parts = assetFile.RelativePath.Replace('\\', '/').Split('/');
        return parts.Length > 2 ? parts[1] : "Atlas";
    }
}