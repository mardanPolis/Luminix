using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StbImageSharp;
using StbImageWriteSharp;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace Luminix.IO;

public sealed class AssetIO : Workspace.Service
{
    public const string ASSET_DIRECTORY = "Assets";
    public const string ASSET_CACHE_DIRECTORY = "Cache";

    private static Dictionary<(string, string), Type> dataAssetTypes = null;

    private static readonly HashSet<string> packageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".package" };
    private static readonly HashSet<string> textureExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
    private static readonly HashSet<string> audioExtensions = new(StringComparer.OrdinalIgnoreCase) { ".wav", ".mp3", ".ogg" };
    private static readonly HashSet<string> dataExtensions = new(StringComparer.OrdinalIgnoreCase) { ".json" };
    private static readonly HashSet<string> monoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xnb" };

    private static readonly HashSet<string> allExtensions = new HashSet<string>(packageExtensions.Union(textureExtensions).Union(audioExtensions).Union(monoExtensions).Union(dataExtensions));
    private static readonly List<string> extensions = allExtensions.ToList();

    public struct AssetFile
    {
        public static AssetFile Generated => new AssetFile("Generated");

        internal AssetFile(string assetRoot, string relativePath)
        {
            AssetRoot = assetRoot;
            RelativePath = relativePath;
            Name = Path.GetFileNameWithoutExtension(Path.Combine(assetRoot, relativePath)).ToLower();
        }
        private AssetFile(string gen)
        {
            AssetRoot = null;
            RelativePath = null;
            Name = null;

            IsGenerated = true;
        }

        public bool IsGenerated { get; }

        public string AssetRoot { get; }
        public string RelativePath { get; }
        public string Name { get; }

        public string FullPath => Path.Combine(AssetRoot, RelativePath);
        public long FileSize => IsGenerated ? 0 : new FileInfo(FullPath).Length;

        public override string ToString() => FullPath;
    }

    public IEnumerable<AssetFile> EnumerateAssets(string rootDirectory = "", bool? boot = null, string directory = "", bool includeDataAssets = false)
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

            string extension = Path.GetExtension(relativePath);
            
            if(!allExtensions.Contains(extension))
            {
                Logger.Get()?.Log($"Unsupported extension '{extension}' at location: '{path}'!");
                continue;
            }

            if(includeDataAssets && dataExtensions.Contains(extension))
                continue;

            yield return new AssetFile(root, relativePath);
        }
    } 

    public bool Access(string rootDirectory, string directory, out AssetFile _assetFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory, nameof(rootDirectory));
        ArgumentException.ThrowIfNullOrWhiteSpace(directory, nameof(directory));

        _assetFile = new AssetFile();

        if (Path.IsPathFullyQualified(directory))
        {
            Logger.Get()?.Log("Not supported!");
            return false;
        }

        string root = Path.Combine(AppContext.BaseDirectory, rootDirectory, ASSET_DIRECTORY);
        if (!Path.Exists(root))
            throw new DirectoryNotFoundException($"The specified asset path '{root}' does not exist.");

        string fullPath = Path.Combine(root, directory);
        if (!fullPath.StartsWith(root))
            throw new ArgumentException("Invalid directory traversal.");

        if (Path.HasExtension(fullPath))
        {
            string extension = Path.GetExtension(fullPath);
            if (!allExtensions.Contains(extension))
                throw new NotSupportedException($"Unsupported extension '{extension}' at location: '{fullPath}'!");

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"The specified asset path '{fullPath}' does not exist.");

            _assetFile = new AssetFile(root, directory);
        }
        else
        {
            string extension = null;
            for (int i = 0; i < extensions.Count; i++)
            {
                if (File.Exists(fullPath + extensions[i]))
                {
                    extension = extensions[i];
                    break;
                }
            }

            if (extension == null)
                throw new FileNotFoundException($"The specified asset path '{directory}' does not exist.");

            _assetFile = new AssetFile(root, directory + extension);
        }

        return true;
    }

    public void LoadAsset(AssetFile assetFile, out Asset _asset)
    {
        _asset = null;
        string extension = Path.GetExtension(assetFile.FullPath);
        if(packageExtensions.Contains(extension))
        {
            LoadPackageAsset(assetFile, out PackageAsset asset);
            _asset = asset; 
            return;
        }
        if(textureExtensions.Contains(extension))
        {
            LoadTexture(assetFile, out TextureAsset asset);
            _asset = asset; 
            return;
        }
        if(audioExtensions.Contains(extension))
        {
            LoadAudio(assetFile, out AudioAsset asset);
            _asset = asset; 
            return;
        }
        if(monoExtensions.Contains(extension))
        {
            LoadStreamAsset(assetFile, out StreamAsset asset);
            _asset = asset; 
            return;
        }
        if (dataExtensions.Contains(extension))
        {
            if(LoadDataAsset(assetFile, out DataAsset asset))
                _asset = asset; 

            return;
        }

        throw new ArgumentException($"Unsupported file extension: '{extension}'");
    }
    public void LoadTexture(AssetFile assetFile, out TextureAsset _textureAsset)
    {
        using Stream stream = OpenStream(assetFile);
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

        _textureAsset = new TextureAsset(assetFile.Name, colors, size);
    }
    public void LoadAudio(AssetFile assetFile, out AudioAsset _audioAsset)
    {
        using Stream stream = OpenStream(assetFile);
        var soundEffect = SoundEffect.FromStream(stream);
        _audioAsset = new AudioAsset(assetFile.Name, soundEffect);
    }
    public void LoadStreamAsset(AssetFile assetFile, out StreamAsset _streamAsset)
    {
        _streamAsset = null;

        using Stream fileStream = OpenStream(assetFile);
        MemoryStream memStream = new MemoryStream();
        fileStream.CopyTo(memStream);
        memStream.Position = 0;

        if(InputOutputUtils.DetectXMBAssetType(memStream, out var assetType))
            _streamAsset = new StreamAsset(assetFile.Name, memStream, assetType);
        else
            Logger.Get()?.Err($"File '{assetFile.FullPath}' is not a valid XNB file or has an unrecognized format.");
    }
    public void LoadPackageAsset(AssetFile assetFile, out PackageAsset _assets)
    {
        throw new NotImplementedException(nameof(LoadPackageAsset));
    }
    public bool LoadDataAsset(AssetFile assetFile, out DataAsset _dataAsset)
    {
        _dataAsset = null;
        if (dataAssetTypes == null || dataAssetTypes.Count == 0)
        {
            Logger.Get().Log("No DataAsset types found!");
            return false;
        }

        var json = File.ReadAllText(assetFile.FullPath);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            Logger.Get().Log($"Failed to parse JSON at line {ex.LineNumber}: {ex.Message}");
            return false;
        }

        var property = doc.RootElement.GetProperty("dataAsset");
        string[] typeName = property.GetString().Split(".");
        if (typeName.Length != 2)
        {
            Logger.Get().Log($"Incorrect formatting! Expected 'ModID.ClassName' but got '{property.GetString()}'");
            return false;
        }

        if (!dataAssetTypes.TryGetValue((typeName[0], typeName[1]), out Type type))
        {
            Logger.Get().Log($"DataAsset (modID '{typeName[0]}', type '{typeName[1]}') not found in registry!");
            return false;
        }

        var dataElement = doc.RootElement.GetProperty("data").GetRawText();
        object result = JsonSerializer.Deserialize(dataElement, type);
        _dataAsset = (DataAsset)result;
        return true;
    }

    public async Task<Asset> LoadAssetAsync(AssetFile assetFile)
    {
        string extension = Path.GetExtension(assetFile.FullPath);

        if (packageExtensions.Contains(extension))
            return await LoadPackageAssetAsync(assetFile);

        if (textureExtensions.Contains(extension))
            return await LoadTextureAsync(assetFile);

        if (audioExtensions.Contains(extension))
            return await LoadAudioAsync(assetFile);

        if (monoExtensions.Contains(extension))
            return await LoadStreamAssetAsync(assetFile);

        return null;
    }
    public async Task<TextureAsset> LoadTextureAsync(AssetFile assetFile)
    {
        await using Stream stream = OpenStream(assetFile);
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

        return new TextureAsset(assetFile.Name, colors, size);
    }
    public async Task<AudioAsset> LoadAudioAsync(AssetFile assetFile)
    {
        await using Stream stream = OpenStream(assetFile);
        var soundEffect = SoundEffect.FromStream(stream);
        return new AudioAsset(assetFile.Name, soundEffect);
    }
    public async Task<StreamAsset> LoadStreamAssetAsync(AssetFile assetFile)
    {
        await using Stream fileStream = OpenStream(assetFile);
        var memStream = new MemoryStream();
        await fileStream.CopyToAsync(memStream);
        memStream.Position = 0;

        if(InputOutputUtils.DetectXMBAssetType(memStream, out var assetType))
            return new StreamAsset(assetFile.Name, memStream, assetType);
        else
            Logger.Get()?.Err($"File '{assetFile.FullPath}' is not a valid XNB file or has an unrecognized format.");
        
        return null;
    }
    public async Task<PackageAsset> LoadPackageAssetAsync(AssetFile assetFile)
    {
        throw new NotImplementedException(nameof(LoadPackageAssetAsync));
    }

    public Stream OpenStream(AssetFile assetFile)
    {
        return new FileStream(assetFile.FullPath, 
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true);
    }

    internal void SaveTexture(string name, Color[] colors, Vector2 size)
    {
        string directoryPath = Path.Combine(AppContext.BaseDirectory, ASSET_CACHE_DIRECTORY);
        string path = Path.Combine(directoryPath, name + ".png");

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

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

        if(Settings.DebugAssets)
            Logger.Get().Log($"Texture '{name}', saved to: '{path}'");
    }

}