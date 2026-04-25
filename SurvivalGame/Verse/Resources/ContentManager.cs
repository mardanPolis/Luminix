using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering;

namespace Verse.Resources;
public sealed class ContentManager : Microsoft.Xna.Framework.Content.ContentManager
{
    public ContentManager(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        textureRegions = new Dictionary<string, TextureRegion>();
        textures = new Dictionary<string, Texture2D>();
        soundEffects = new Dictionary<string, SoundEffect>();
        spriteFonts = new Dictionary<string, SpriteFont>();
        effects = new Dictionary<string, Effect>();
    }

    private readonly Dictionary<string, TextureRegion> textureRegions;
    private readonly Dictionary<string, Texture2D> textures;
    private readonly Dictionary<string, SoundEffect> soundEffects;
    private readonly Dictionary<string, SpriteFont> spriteFonts;
    private readonly Dictionary<string, Effect> effects;

    private readonly Dictionary<string, object> loadedAssets;
    private readonly List<IDisposable> disposables;

    private Stream openedStream = null;

    public Texture2D CreateTexture(string assetName, Color[] colors, Vector2 scale)
    {
        ArgumentNullException.ThrowIfNull(assetName, nameof(assetName));
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        
        if(textures.ContainsKey(assetName))
            throw new InvalidOperationException($"Unable to create a Texture, because one already exists with name: '{assetName}'");

        var texture = new Texture2D(RenderingSystem.GraphicsDevice, (int)scale.X, (int)scale.Y);
        texture.SetData(colors);
        textures[assetName] = texture;
        loadedAssets[assetName] = texture;
        disposables.Add(texture);
        return texture;
    }

    public Texture2D CreateTexture(string assetName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(assetName, nameof(assetName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if(textures.ContainsKey(assetName))
            throw new InvalidOperationException($"Unable to create a Texture, because one already exists with name: '{assetName}'");

        openedStream = stream;
        var texture =  ReadAsset<Texture2D>(assetName, null);
        textures[assetName] = texture;
        loadedAssets[assetName] = texture;
        disposables.Add(texture);
        openedStream = null;
        return texture;
    }

    public SoundEffect CreateSoundEffect(string assetName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(assetName, nameof(assetName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if(soundEffects.ContainsKey(assetName))
            throw new InvalidOperationException($"Unable to create a SoundEffect, because one already exists with name: '{assetName}'");

        openedStream = stream;
        SoundEffect soundEffect = ReadAsset<SoundEffect>(assetName, null);
        soundEffects[assetName] = soundEffect;
        loadedAssets[assetName] = soundEffect;
        openedStream = null;
        return soundEffect;
    }

    public SpriteFont CreateSpriteFont(string assetName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(assetName, nameof(assetName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if(spriteFonts.ContainsKey(assetName))
            throw new InvalidOperationException($"Unable to create a SpriteFont, because one already exists with name: '{assetName}'");

        openedStream = stream;
        var font = ReadAsset<SpriteFont>(assetName, null);
        spriteFonts[assetName] = font;
        loadedAssets[assetName] = font;
        openedStream = null;
        return font;
    }

    public Effect CreateEffect(string assetName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(assetName, nameof(assetName));
        
        if(soundEffects.ContainsKey(assetName))
            throw new InvalidOperationException($"Unable to create a Effect, because one already exists with name: '{assetName}'");
        
        openedStream = stream;
        var effect = ReadAsset<Effect>(assetName, null);
        effects[assetName] = effect;
        openedStream = null;
        return effect;
    }

    protected override Stream OpenStream(string assetName)
    {
        if(openedStream != null)
            return openedStream;

        return base.OpenStream(assetName);
    }

    public override void Unload()
    {
        base.Unload();

        foreach (IDisposable disposableAsset in disposables)
        {
            disposableAsset?.Dispose();
        }

        disposables.Clear();
        loadedAssets.Clear();
    }

    public override void UnloadAsset(string assetName)
    {
        textureRegions.Remove(assetName);
        textures.Remove(assetName);
        soundEffects.Remove(assetName);
        spriteFonts.Remove(assetName);
        effects.Remove(assetName);

        if (loadedAssets.TryGetValue(assetName, out var value))
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
                disposables.Remove(disposable);
            }

            loadedAssets.Remove(assetName);
        }
        base.UnloadAsset(assetName);

    }
}
        // NullTexture = LoadTexture("null_texture.png");
        // NullTextureRegion = NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

        // GenericFontBold = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Bold");
        // GenericFontMedium = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Medium");
        // GenericFontRegular = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Regular");

        // TileShader = ContentManager.Load<Effect>("Effects/SpriteShader");

