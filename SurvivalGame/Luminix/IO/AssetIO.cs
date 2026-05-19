using System;
using System.Collections.Generic;
using System.IO;
using Luminix.Collections;
using Luminix.Workspaces;

namespace Luminix.IO;

public sealed class AssetIO : Workspace.Service
{
    public const string ASSET_DIRECTORY = "Assets";

    private readonly string root;

    private DenseList<string> loadedAssets;

    private IReadOnlyDenseList<string> LoadedAssets => loadedAssets;
    public AssetIO(string root = "")
    {
        if(root != "" && !Directory.Exists(root))
        {
            Logger.Get().Wrn($"AssetIO root directory '{root}' does not exist. Creating it.");
            Directory.CreateDirectory(root);
        }
        else if(root == "")
        {
            root = AppContext.BaseDirectory;
        }
        this.root = root;

        loadedAssets = new DenseList<string>();
    }

    public IEnumerable<AssetEntry> LoadAssets()
    {
        return LoadAssetsIn("");
    }

    public IEnumerable<AssetEntry> LoadAssetsIn(string directory)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(directory, nameof(directory));

        if (!Workspace.IsState(Workspace.State.Initialize))
            throw new InvalidOperationException("Assets can only be loaded during the Initialize phase of the workspace.");

        string assetDir = Path.Combine(root, directory, ASSET_DIRECTORY);

        if (!Directory.Exists(assetDir)) yield break;

        foreach (var path in Directory.EnumerateFiles(assetDir, "*.*", SearchOption.AllDirectories))
        {
            Stream stream;
            try
            {
                stream = File.OpenRead(path);
            }
            catch (Exception ex)
            {
                Logger.Get().Err($"Failed to load asset at path: {path}, reason: {ex}");
                continue;
            }

            string relativePath = Path.GetRelativePath(root, path);
            loadedAssets.Add(relativePath);
            yield return new AssetEntry(relativePath, stream);
        }
    }

    public IEnumerable<string> GetAssetPathsIn(string directory)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(directory, nameof(directory));

        if (!Workspace.IsState(Workspace.State.Initialize))
            throw new InvalidOperationException("Assets can only be loaded during the Initialize phase of the workspace.");

        string assetDir = Path.Combine(root, directory, ASSET_DIRECTORY);

        if (!Directory.Exists(assetDir)) yield break;

        foreach (var path in Directory.EnumerateFiles(assetDir, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(assetDir, path);
            yield return relativePath;
        }
    }
}

public readonly struct AssetEntry(string path, Stream stream) : IDisposable
{
    public string Path { get; } = path;
    public Stream Stream { get; } = stream;
    public void Dispose() => Stream.Dispose();
}
