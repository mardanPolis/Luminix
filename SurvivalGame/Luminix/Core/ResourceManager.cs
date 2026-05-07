using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Collections;
using Luminix.Identifiers;
using System.Linq;

namespace Luminix;
internal sealed class ResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    public struct TextureRegion
    {
        public TextureRegion(Texture2D texture, int x, int y, int width, int height)
        {
            if(texture == null) throw new ArgumentNullException(nameof(texture));

            Texture = texture;
            Rectangle = new Rectangle(x, y, width, height);
            WidthCoefficient = 1f / width;
            HeightCoefficient = 1f / height;
        }

        public readonly Texture2D Texture;
        public readonly Rectangle Rectangle;

        public readonly float WidthCoefficient;
        public readonly float HeightCoefficient;
    }

    private struct Textures // aka freeList for textures
    {   
        private struct SourceTexture
        {
            public SourceTexture(string name, Texture2D texture)
            {
                ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
                ArgumentNullException.ThrowIfNull(texture, nameof(texture));

                Name = name;
                Texture = texture;
                Regions = new List<ID>();
            }

            public readonly string Name;
            public readonly Texture2D Texture;
            public readonly List<ID> Regions;
        }

        public Textures(int initialCapacity = 32)
        {
            sourcesStrings = new Dictionary<string, SourceTexture>();
            sourceTextures = new Dictionary<Texture2D, SourceTexture>();

            data = new TextureRegion[initialCapacity];
            slotStatus = new bool[initialCapacity];
            freeSlots = new Stack<uint>();
        }

        private readonly Dictionary<string, SourceTexture> sourcesStrings;
        private readonly Dictionary<Texture2D, SourceTexture> sourceTextures;

        private TextureRegion[] data;
        private bool[] slotStatus;
        private readonly Stack<uint> freeSlots;

        private int count;
        private int nextIndex;

        public int Count => count;
        public int Cursor => nextIndex;

        public TextureRegion this[ID id]
        {
            get
            {
                int index = id;
                if (index < 0 || index >= nextIndex)
                    throw new IndexOutOfRangeException($"Index {index} is out of range for EngineTextures with Cursor {Cursor}.");

                return data[index];
            }
        }
        public Texture2D this[string name]
        {
            get
            {
                if (!sourcesStrings.TryGetValue(name, out SourceTexture sourceTexture))
                    return null;

                return sourceTexture.Texture;
            }
        }
        
        public void NewSource(string name, Texture2D texture)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            ArgumentNullException.ThrowIfNull(texture, nameof(texture));

            if(sourcesStrings.ContainsKey(name))
                throw new ArgumentException($"A texture source with the name '{name}' already exists.");

            sourcesStrings[name] = new SourceTexture(name, texture);
        }
        public void RemSource(string name)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            if(!sourcesStrings.ContainsKey(name))
                return;

            var source = sourcesStrings[name];
            for (int i = 0; i < source.Regions.Count; i++)
                Remove(source.Regions[i]);

            sourcesStrings.Remove(name);
            sourceTextures.Remove(source.Texture);
        }

        public ID Append(Texture2D texture, int x, int y, int width, int height)
        {
            if(texture == null) throw new ArgumentNullException(nameof(texture));
            if(!sourceTextures.TryGetValue(texture, out SourceTexture sourceTexture)) throw new ArgumentException("The provided texture is not registered as a source texture.", nameof(texture));

            var next = nextIndex;
            if(next >= data.Length)
            {
                if(freeSlots.Count > 0)
                {
                    next = (int)freeSlots.Pop();
                }
                else
                {
                    int newSize = data.Length * 2;
                    Array.Resize(ref data, newSize);
                    Array.Resize(ref slotStatus, newSize);
                    nextIndex++;
                    count++;
                }
            }
            else
            {
                nextIndex++;
                count++;
            }

            ID id = (ID)next;
            data[next] = new TextureRegion(texture, x, y, width, height);
            slotStatus[next] = true;
            sourceTexture.Regions.Add(id);
            return id;
        }

        public void Remove(ID id)
        {
            int idx = id;
            if (idx < 0 || idx >= data.Length || !slotStatus[idx])
                return;

            data[idx] = default;
            slotStatus[idx] = false;
            freeSlots.Push((uint)idx);
            count--;
        }
    
        public void Clear()
        {
            Array.Clear(data, 0, data.Length);
            Array.Clear(slotStatus, 0, slotStatus.Length);
            freeSlots.Clear();
            count = 0;
            nextIndex = 0;
        }
    }

    public static bool HeaderIsXNB(byte[] header)
    {
        return header[0] == 'X' && header[1] == 'N' && header[2] == 'B';
    }
    public static bool HeaderIsTexture(byte[] header)
    {
        return (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) ||
            (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF);
    }
    public static bool HeaderIsAudio(byte[] header)
    {
        return header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F';
    }
    public static bool HeaderIsSong(byte[] header)
    {
        return (header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33) ||
            (header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53);
    }

    public ResourceManager(Main main) : base(main.Services)
    {
        resources = new Dictionary<string, Resource>();

        RootDirectory = "Assets";

        MethodInfo method = typeof(Microsoft.Xna.Framework.Content.ContentManager).GetMethod("RecordDisposable", BindingFlags.Instance | BindingFlags.NonPublic) ??
         throw new InvalidOperationException("Could not find method RecordDisposable on ContentManager.");
        RecordDisposable = (Action<IDisposable>)Delegate.CreateDelegate(typeof(Action<IDisposable>), this, method);

        this.main = main;
        this.graphicsDevice = main.GraphicsDevice;
    }

    Action<IDisposable> RecordDisposable;

    private Main main;
    private GraphicsDevice graphicsDevice;

    private readonly Dictionary<string, Resource> resources;
    private readonly Textures engineTextures;
    private readonly FreeList<Effect> effects;
    private readonly FreeList<SpriteFont> fonts;

    private Stream openedStream = null; 

    private bool disposed = false;

    public void CreateTexture(string name, Color[] colors, Vector2 scale)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        
        Texture2D texture = new Texture2D(graphicsDevice, (int)scale.X, (int)scale.Y);
        texture.SetData(colors);

        engineTextures.NewSource(name, texture);
        RecordDisposable(texture);
    }

    public void CreateTextureRegion(string textureName, string name, Vector2 position, Vector2 scale)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(textureName, nameof(textureName));

        throw new NotImplementedException("Creating texture regions is not yet implemented.");
    }

    protected sealed override Stream OpenStream(string name)
    {
        if(openedStream != null)
            return openedStream;

        return base.OpenStream(name);
    }
    public sealed override void UnloadAsset(string name)
    {
        if(!resources.ContainsKey(name))
            throw new KeyNotFoundException($"Cannot unload asset '{name}' because it is not loaded.");
        
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException("assetName");
        }

        if (disposed)
        {
            throw new ObjectDisposedException("ContentManager");
        }

        resources.Remove(name);
        engineTextures.RemSource(name);

        base.UnloadAsset(name);

    }
    public sealed override void Unload() 
    {
        base.Unload();
        resources.Clear();
        engineTextures.Clear();
        effects.Clear();
        fonts.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        disposed = true;
    }
}
        // NullTexture = LoadTexture("null_texture.png");
        // NullTextureRegion = NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

        // GenericFontBold = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Bold");
        // GenericFontMedium = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Medium");
        // GenericFontRegular = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Regular");

        // TileShader = ContentManager.Load<Effect>("Effects/SpriteShader");
