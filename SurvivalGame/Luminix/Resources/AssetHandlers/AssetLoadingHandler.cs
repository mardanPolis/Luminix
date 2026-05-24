using System;
using System.Collections.Generic;
using Luminix.Threading;
using Luminix.IO;
using Luminix.Collections;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace Luminix.Resources.AssetHandlers;

internal class AssetLoadingHandler
{
    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
    private static readonly HashSet<string> SoundExtensions = new(StringComparer.OrdinalIgnoreCase) { ".wav" };

    private readonly WorkerThreadPool workerThreadPool;
    private readonly AssetIO assetIO;

    private readonly DenseList<string> loadedBootAssets;
    private readonly DenseList<string> loadedAssets;

    private readonly DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)> loadedBootTextures;
    private readonly DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)> loadedTextures;

    public AssetLoadingHandler(WorkerThreadPool pool, AssetIO io)
    {
        this.workerThreadPool = pool;
        this.assetIO          = io;
        this.loadedBootAssets = new DenseList<string>();
        this.loadedAssets     = new DenseList<string>();

        this.loadedBootTextures = new DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)>();
        this.loadedTextures = new DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)>();
    }

    public void RunBootLoadingPhase()
    {
        var loaded = new ConcurrentBag<string>();
        var loadedTextures = new ConcurrentBag<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)>();

        var assetFiles = assetIO.EnumerateAssets(boot: true).ToList();
        workerThreadPool.ParallelFor(assetFiles, assetFile =>
        {
            var extension = Path.GetExtension(assetFile.RelativePath);

            if(extension == Main.ENGINE_PACKAGE_ASSET_EXTENSION) HandlePackageFileAsset(assetFile);
            else HandleGenericFileAsset(assetFile, extension, loadedTextures);

            loaded.Add(assetFile.FullPath);
        });

        while(loaded.TryTake(out string path))
            loadedBootAssets.Add(path);

        while(loadedTextures.TryTake(out var loadedTexture))
            this.loadedBootTextures.Add(loadedTexture);
    }

    public void RunLoadingPhase()
    {
        var loaded = new ConcurrentBag<string>();
        var loadedTextures = new ConcurrentBag<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)>();

        var assetFiles    = assetIO.EnumerateAssets(boot: false).ToList();
        var totalAssets   = assetFiles.Count;

        workerThreadPool.ParallelFor(assetFiles, assetFile =>
        {
            var extension = Path.GetExtension(assetFile.RelativePath);

            if(extension == Main.ENGINE_PACKAGE_ASSET_EXTENSION) HandlePackageFileAsset(assetFile);
            else HandleGenericFileAsset(assetFile, extension, loadedTextures);

            loaded.Add(assetFile.FullPath);
        });

        while(loaded.TryTake(out string path))
            loadedAssets.Add(path);

        while(loadedTextures.TryTake(out var loadedTexture))
            this.loadedTextures.Add(loadedTexture);
    }
    
    public HashSet<string> GetLoadedBootAssets() => loadedBootAssets.ToHashSet();
    public HashSet<string> GetLoadedAssets() => loadedAssets.ToHashSet();

    public DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)> GetLoadedBootTextures() => loadedBootTextures;
    public DenseList<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)> GetLoadedTextures() => loadedTextures;

    private void HandleGenericFileAsset(AssetIO.AssetFile assetFile, string extension, ConcurrentBag<(AssetIO.AssetFile assetFile, Color[] colors, Vector2 size)> loadedTextures)
    {
        if (TextureExtensions.Contains(extension) && assetFile.RelativePath.StartsWith(AssetIO.TEXTURES_DIRECTORY))
        {
            assetIO.LoadTexture(assetFile, out Color[] colors, out Vector2 size);
            loadedTextures.Add(new (assetFile, colors, size)); // caches the loaded texture.
        }
        else if (SoundExtensions.Contains(extension))
        {
            assetIO.LoadSound(assetFile, out SoundEffect sound);
            // TODO : registration of Sound
        }
    }

    private void HandlePackageFileAsset(AssetIO.AssetFile assetFile)
    {
        throw new NotImplementedException(nameof(HandlePackageFileAsset));
    }
}