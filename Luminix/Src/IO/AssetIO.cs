using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using StbImageSharp;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Luminix.IO;

public sealed class AssetIO
{
    public const string ASSET_DIRECTORY = "Assets";
    public const string ASSET_CACHE_DIRECTORY = "Cache";


    // private static readonly HashSet<string> packageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".package" };
    private static readonly HashSet<string> textureExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
    private static readonly HashSet<string> audioExtensions = new(StringComparer.OrdinalIgnoreCase) { ".wav" };
    private static readonly HashSet<string> dataExtensions = new(StringComparer.OrdinalIgnoreCase) { ".json" };
    private static readonly HashSet<string> xnbExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xnb" };

    public readonly struct AssetInfo
    {
        internal AssetInfo(string rootPath, string relativePath)
        {
            Name = Path.GetFileNameWithoutExtension(relativePath).ToLower();
            IsGenerated = false;

            RootPath = rootPath;
            RelativePath = Path.ChangeExtension(relativePath, null);
            Extension = Path.GetExtension(relativePath);

            Size = new FileInfo(FullPath).Length;
        }

        public string Name { get; }
        public bool IsGenerated { get; }

        public long Size { get; }
        public string RootPath { get; }
        public string RelativePath { get; }
        public string Extension { get; }
        
        public readonly string FullPath => Path.Combine(RootPath, RelativePath) + Extension ?? null;

        public override string ToString() => FullPath;
    }

    public IEnumerable<AssetInfo> EnumerateAssetInfos(string rootPath, bool? boot = null)
    {
        if (!Path.IsPathRooted(rootPath))
            throw new ArgumentException($"'{rootPath}' must not be a relative path.", nameof(rootPath));

        string[] segments = rootPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(s => s == ".."))
            throw new ArgumentException($"'{rootPath}' must not contain parent directory references ('..').", nameof(rootPath));

        if (rootPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException($"'{rootPath}' contains invalid path characters.", nameof(rootPath));

        if (!string.IsNullOrEmpty(Path.GetExtension(rootPath)))
            throw new ArgumentException($"'{rootPath}' must be a directory, not a file.", nameof(rootPath));

        string targetPath = Path.GetFullPath(Path.Combine(rootPath, ASSET_DIRECTORY));
        if(!Directory.Exists(targetPath))
        {
            targetPath = Path.GetFullPath(Path.Combine(rootPath, ASSET_DIRECTORY.ToLower()));
            if(!Directory.Exists(targetPath))
                yield break;
        }

        foreach (var path in Directory.EnumerateFiles(targetPath, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(targetPath, path);

            bool isBoot = relativePath.StartsWith("boot", StringComparison.OrdinalIgnoreCase);
            if (boot.HasValue && boot.Value != isBoot) continue;

            string extension = Path.GetExtension(relativePath).ToLower();
            
            if(textureExtensions.Contains(extension) || dataExtensions.Contains(extension) || audioExtensions.Contains(extension) || xnbExtensions.Contains(extension))
            {
                yield return new AssetInfo(targetPath, relativePath);
                continue;
            }

            Logger.NewLog(Logger.Log.Level.Warning, $"Unsupported extension '{extension}' at location: '{path}'!");
            continue;
        }
    }

    public bool IsTextureAsset(AssetInfo assetInfo)
    {
        string extension = assetInfo.Extension.ToLower();
        return textureExtensions.Contains(extension);
    }
    public bool IsAudioAsset(AssetInfo assetInfo)
    {
        string extension = assetInfo.Extension.ToLower();
        return audioExtensions.Contains(extension);
    }
    public bool IsXnbAsset(AssetInfo assetInfo)
    {
        string extension = assetInfo.Extension.ToLower();
        return xnbExtensions.Contains(extension);
    }
    public bool IsDataAsset(AssetInfo assetInfo)
    {
        string extension = assetInfo.Extension.ToLower();
        return dataExtensions.Contains(extension);
    }


    //Assembly

    public Asset        LoadAsset(Assembly assembly, string identifier)
    {
        string extension = Path.GetExtension(identifier);

        if(textureExtensions.Contains(extension))
            return LoadTextureAsset(assembly, identifier);

        if(audioExtensions.Contains(extension))
            return LoadAudioAsset(assembly, identifier);

        if(xnbExtensions.Contains(extension))
            return LoadXnbAsset(assembly, identifier);

        throw new ArgumentException($"Unsupported extension at '{identifier}'");
    }
    public TextureAsset LoadTextureAsset(Assembly assembly, string identifier)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(identifier);
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

            return new TextureAsset(identifier, colors, size);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load texture '{identifier}' from '{assembly.FullName}', reason: {ex.Message}");
        }
    }
    public AudioAsset   LoadAudioAsset(Assembly assembly, string identifier)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(identifier) ?? throw new FileNotFoundException($"Embedded resource '{identifier}' was not found.");
            AudioData wav = WavReader.Read(stream);
            return new AudioAsset(identifier, wav.PcmData, wav.Format, wav.SampleRate);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load audio '{identifier}' from '{assembly.FullName}'.", ex);
        }
    }
    public XnbAsset     LoadXnbAsset(Assembly assembly, string identifier)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(identifier);
            MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Position = 0;
            return new XnbAsset(identifier, memStream);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load xnb '{identifier}' from '{assembly.FullName}', reason: {ex.Message}");
        }
    }
    public DataAsset    LoadDataAsset(Assembly assembly, string identifier)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(identifier);
            JsonDocument json = JsonDocument.Parse(stream);

            return new DataAsset(identifier, json);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load data '{identifier}' from '{assembly.FullName}', reason: {ex.Message}");
        }
    }

    //Sync

    public Asset        LoadAsset(AssetInfo assetInfo)
    {
        string extension = assetInfo.Extension;
        
        if(textureExtensions.Contains(extension))
            return LoadTextureAsset(assetInfo);

        if(audioExtensions.Contains(extension))
            return LoadAudioAsset(assetInfo);

        if (dataExtensions.Contains(extension))
            return LoadDataAsset(assetInfo);

        if(xnbExtensions.Contains(extension))
            return LoadXnbAsset(assetInfo);

        throw new ArgumentException($"Unsupported file extension at path '{assetInfo}'");
    }
    public TextureAsset LoadTextureAsset(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, false);
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

            return new TextureAsset(assetInfo.RelativePath, colors, size);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load texture at '{assetInfo.FullPath}', reason: {ex.Message}");
        }
    }
    public AudioAsset   LoadAudioAsset(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, false);
            AudioData wav = WavReader.Read(stream);
            return new AudioAsset(assetInfo.RelativePath, wav.PcmData, wav.Format, wav.SampleRate);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load audio at '{assetInfo.FullPath}'.", ex);
        }
    }
    public XnbAsset     LoadXnbAsset(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, false);
            MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Position = 0;

            return new XnbAsset(assetInfo.RelativePath, memStream);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load xnb at '{assetInfo.FullPath}', reason: {ex.Message}");
        }
    }
    public DataAsset    LoadDataAsset(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, false);
            JsonDocument json = JsonDocument.Parse(stream);

            return new DataAsset(assetInfo.FullPath, json);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load data at '{assetInfo.FullPath}', reason: {ex.Message}");
        }
    }

    //Async

    public async Task<Asset>        LoadAssetAsync(AssetInfo assetInfo)
    {
        string extension = assetInfo.Extension;
        if(textureExtensions.Contains(extension))
            return await LoadTextureAssetAsync(assetInfo);

        if(audioExtensions.Contains(extension))
            return await LoadAudioAssetAsync(assetInfo);

        if (dataExtensions.Contains(extension))
            return await LoadDataAssetAsync(assetInfo);

        if(xnbExtensions.Contains(extension))
            return await LoadXnbAssetAsync(assetInfo);

        throw new ArgumentException($"Unsupported file extension at path '{assetInfo}'");
    }
    public async Task<TextureAsset> LoadTextureAssetAsync(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, true);
            MemoryStream memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            memStream.Position = 0;

            var result = ImageResult.FromStream(memStream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

            int pixelCount = result.Width * result.Height;
            var colors = new Color[pixelCount];
            var raw = result.Data;

            for (int i = 0; i < pixelCount; i++)
            {
                int b = i * 4;
                colors[i] = new Color(raw[b], raw[b + 1], raw[b + 2], raw[b + 3]);
            }

            var size = new Vector2(result.Width, result.Height);

            return new TextureAsset(assetInfo.RelativePath, colors, size);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load texture at '{assetInfo.FullPath}', reason: {ex.Message}");
        }
    }
    public async Task<AudioAsset>   LoadAudioAssetAsync(AssetInfo assetInfo)
    {
        try
        {
            await using Stream stream = OpenStream(assetInfo, true);

            AudioData wav = await WavReader.ReadAsync(stream);

            return new AudioAsset(assetInfo.RelativePath, wav.PcmData, wav.Format, wav.SampleRate);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to load audio at '{assetInfo.FullPath}'.", ex);
        }
    }
    public async Task<XnbAsset>     LoadXnbAssetAsync(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, true);
            MemoryStream memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            memStream.Position = 0;
            return new XnbAsset(assetInfo.RelativePath, memStream);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load xnb at '{assetInfo.FullPath}', reason: {ex.Message}");
        }
    }
    public async Task<DataAsset>    LoadDataAssetAsync(AssetInfo assetInfo)
    {
        try
        {
            using Stream stream = OpenStream(assetInfo, false);
            JsonDocument json = JsonDocument.Parse(stream);

            return new DataAsset(assetInfo.FullPath, json);
        }
        catch(Exception ex)
        {
            throw new InvalidDataException($"Failed to load data at '{assetInfo.FullPath}', reason: {ex.Message}");
        }
    }

    private Stream OpenStream(AssetInfo assetInfo, bool useAsync)
    {
        return new FileStream(assetInfo.FullPath, 
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true);
    }


    // public void LoadPackageAsset(AssetInfo assetInfo, out PackageAsset _asset)
    // {
    //     throw new NotImplementedException(nameof(LoadPackageAsset));
    // }

    // public async Task<PackageAsset> LoadPackageAssetAsync(AssetInfo assetInfo)
    // {
    //     throw new NotImplementedException(nameof(LoadPackageAssetAsync));
    // }

    // internal void SaveTexture(string name, Color[] colors, Vector2 size)
    // {
    //     string directoryPath = Path.Combine(AppContext.BaseDirectory, ASSET_CACHE_DIRECTORY);
    //     string path = Path.Combine(directoryPath, name + ".png");

    //     if (!Directory.Exists(directoryPath))
    //         Directory.CreateDirectory(directoryPath);

    //     byte[] bytes = new byte[colors.Length * 4];
    //     for (int i = 0; i < colors.Length; i++)
    //     {
    //         bytes[i * 4 + 0] = colors[i].R;
    //         bytes[i * 4 + 1] = colors[i].G;
    //         bytes[i * 4 + 2] = colors[i].B;
    //         bytes[i * 4 + 3] = colors[i].A;
    //     }

    //     using FileStream stream = File.OpenWrite(path);
    //     ImageWriter writer = new ImageWriter();
    //     writer.WritePng(bytes, (int)size.X, (int)size.Y, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);

    // }

}