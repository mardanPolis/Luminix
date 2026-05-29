using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StbImageSharp;
using StbImageWriteSharp;

using Luminix.Workspaces;

namespace Luminix.IO;

public sealed class AssetIO : Workspace.Service
{
    public const string ASSET_DIRECTORY = "Assets";
    public const string ASSET_CACHE_DIRECTORY = "Cache";
    
    public const string TEXTURES_DIRECTORY  = "Textures";
    public const string AUDIO_DIRECTORY     = "Audios";
    public const string SHADER_DIRECTORY    = "Shaders";
    public const string FONTS_DIRECTORY     = "Fonts";
    public const string DATA_DIRECTORY      = "Data";

    public struct AssetFile(string assetRoot, string relativePath)
    {
        public readonly string AssetRoot = assetRoot;
        public readonly string RelativePath = relativePath;

        public string FullPath => Path.Combine(AssetRoot, RelativePath);

        public override string ToString() => FullPath;
    }

    public IEnumerable<AssetFile> EnumerateAssets(string rootDirectory = "", bool? boot = null, string directory = "")
    {
        string root = Path.Combine(AppContext.BaseDirectory, rootDirectory, ASSET_DIRECTORY);

        if (!Path.Exists(root))
            throw new DirectoryNotFoundException($"The specified asset path '{root}' does not exist.");

        var targetPath = Path.Combine(root, directory);

        if (!targetPath.StartsWith(root))
            throw new ArgumentException("Invalid directory traversal.");

        if (!Directory.Exists(targetPath)) yield break;

        foreach (var path in Directory.EnumerateFiles(targetPath, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(root, path);
            bool isBoot = relativePath.StartsWith("Boot");

            if (boot.HasValue && boot.Value != isBoot) continue;

            yield return new AssetFile(root, relativePath);
        }
    } 

    public void LoadTexture(AssetFile assetFile, out Color[] colors, out Vector2 size)
    {
        using Stream stream = File.OpenRead(assetFile.FullPath);
        var result = ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

        int pixelCount = result.Width * result.Height;
        colors = new Color[pixelCount];
        var raw = result.Data;

        for (int i = 0; i < pixelCount; i++)
        {
            int b = i * 4;
            colors[i] = new Color(raw[b], raw[b + 1], raw[b + 2], raw[b + 3]);
        }

        size = new Vector2(result.Width, result.Height);
    }

    public void LoadSound(AssetFile assetFile, out SoundEffect sound)
    {
        using Stream stream = File.OpenRead(assetFile.FullPath);
        sound = SoundEffect.FromStream(stream);
    }

    // TODO : add a way to load Shaders, SpriteFonts.

    internal void SaveTexture(string name, Color[] colors, Vector2 size)
    {
        string path = Path.Combine(AppContext.BaseDirectory, ASSET_CACHE_DIRECTORY, name + ".png");

        byte[] bytes = new byte[colors.Length * 4];
        for (int i = 0; i < colors.Length; i++)
        {
            bytes[i * 4 + 0] = colors[i].R;
            bytes[i * 4 + 1] = colors[i].G;
            bytes[i * 4 + 2] = colors[i].B;
            bytes[i * 4 + 3] = colors[i].A;
        }

        using FileStream stream = File.OpenWrite(path);
        ImageWriter writer = new ImageWriter();
        writer.WritePng(bytes, (int)size.X, (int)size.Y, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
    }
}
