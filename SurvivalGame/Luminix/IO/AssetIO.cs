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

    private static readonly HashSet<string> PackageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".package" };
    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase) { ".wav", ".mp3", ".ogg" };
    private static readonly HashSet<string> MonoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xnb" };

    public struct AssetFile
    {
        public AssetFile(string assetRoot, string relativePath)
        {
            AssetRoot = assetRoot;
            RelativePath = relativePath;
            Name = Path.GetFileNameWithoutExtension(Path.Combine(assetRoot, relativePath));
        }
        public string AssetRoot { get; }
        public string RelativePath { get; }
        public string Name { get; }

        public string FullPath => Path.Combine(AssetRoot, RelativePath);

        public override string ToString() => FullPath;
    }

    public IEnumerable<AssetFile> EnumerateAssets(string rootDirectory = "", bool? boot = null, string directory = "")
    {
        string root = Path.Combine(AppContext.BaseDirectory, rootDirectory, ASSET_DIRECTORY);

        if (!Path.Exists(root))
            throw new DirectoryNotFoundException($"The specified asset path '{root}' does not exist.");

        var targetPath = root;

        if(boot.HasValue && boot.Value) targetPath = Path.Combine(targetPath, "Boot");

        targetPath = Path.Combine(targetPath, directory);

        if (!targetPath.StartsWith(root))
            throw new ArgumentException("Invalid directory traversal.");

        if (!Directory.Exists(targetPath)) yield break;

        foreach (var path in Directory.EnumerateFiles(targetPath, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(targetPath, path);
            bool isBoot = relativePath.StartsWith("Boot");

            if (boot.HasValue && !boot.Value && isBoot) continue;

            yield return new AssetFile(targetPath, relativePath);
        }
    } 

    public void LoadAsset(AssetFile assetFile, out Asset _asset)
    {
        _asset = null;
        string extension = Path.GetExtension(assetFile.FullPath);
        if(PackageExtensions.Contains(extension))
        {
            LoadPackageAsset(assetFile, out PackageAsset asset);
            _asset = asset; 
            return;
        }

        if(TextureExtensions.Contains(extension))
        {
            LoadTexture(assetFile, out TextureAsset asset);
            _asset = asset; 
            return;
        }
        // TODO : create song asset handling
        if(AudioExtensions.Contains(extension))
        {
            LoadAudio(assetFile, out AudioAsset asset);
            _asset = asset; 
            return;
        }

        if(MonoExtensions.Contains(extension))
        {
            LoadStreamAsset(assetFile, out StreamAsset asset);
            _asset = asset; 
            return;
        }
    }

    public void LoadTexture(AssetFile assetFile, out TextureAsset _textureAsset)
    {
        using Stream stream = File.OpenRead(assetFile.FullPath);
        var result = ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

        int pixelCount = result.Width * result.Height;
        var colors = new Color[pixelCount];
        var raw = result.Data;

        for (int i = 0; i < pixelCount; i++)
        {
            int b = i * 4;
            colors[i] = new Color(raw[b], raw[b + 1], raw[b + 2], raw[b + 3]);
        }

        var size = new Vector2(result.Width, result.Height);

        _textureAsset = new TextureAsset(assetFile, colors, size);
    }

    public void LoadAudio(AssetFile assetFile, out AudioAsset _audioAsset)
    {
        using Stream stream = File.OpenRead(assetFile.FullPath);
        var soundEffect = SoundEffect.FromStream(stream);
        _audioAsset = new AudioAsset(assetFile, soundEffect);
    }


    public void LoadStreamAsset(AssetFile assetFile, out StreamAsset _streamAsset)
    {
        using Stream fileStream = OpenStream(assetFile);
        MemoryStream memStream = new MemoryStream();
        fileStream.CopyTo(memStream);
        memStream.Position = 0;

        StreamAsset.AssetType assetType = DetectAssetType(memStream);
        _streamAsset = new StreamAsset(assetFile, memStream, assetType);
    }

    public void LoadPackageAsset(AssetFile assetFile, out PackageAsset _assets)
    {
        throw new NotImplementedException(nameof(LoadPackageAsset));
    }

    public FileStream OpenStream(AssetFile assetFile)
    {
        return File.OpenRead(assetFile.FullPath);
    }


    private StreamAsset.AssetType DetectAssetType(Stream stream)
    {
        try
        {
            byte[] header = new byte[512];
            int bytesRead = stream.Read(header, 0, header.Length);
            stream.Position = 0;

            int i = 10;

            while (i < bytesRead && (header[i] & 0x80) != 0) i++;
            i++;

            int strLen = 0, shift = 0;
            while (i < bytesRead)
            {
                byte b = header[i++];
                strLen |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }

            string typeName = System.Text.Encoding.UTF8.GetString(header, i, Math.Min(strLen, bytesRead - i));

            return typeName switch
            {
                var t when t.Contains("Texture2DReader")   => StreamAsset.AssetType.Texture,
                var t when t.Contains("SpriteFontReader")  => StreamAsset.AssetType.Font,
                var t when t.Contains("SoundEffectReader") => StreamAsset.AssetType.Audio,
                var t when t.Contains("SongReader")        => StreamAsset.AssetType.Audio,
                var t when t.Contains("EffectReader")      => StreamAsset.AssetType.Shader,
                _ => StreamAsset.AssetType.Texture
            };
        }
        catch
        {
            return StreamAsset.AssetType.Texture;
        }
    }

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
