using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering;

namespace Verse.Resources;
public sealed class ContentManager : Microsoft.Xna.Framework.Content.ContentManager
{
    private readonly Dictionary<string, TextureRegion> textureRegions = new Dictionary<string, TextureRegion>();
    private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, SoundEffect> soundEffects = new Dictionary<string, SoundEffect>();
    private readonly Dictionary<string, SpriteFont> spriteFonts = new Dictionary<string, SpriteFont>();
    private readonly Dictionary<string, Effect> effects = new Dictionary<string, Effect>();

    public ContentManager(IServiceProvider serviceProvider) : base(serviceProvider) { }

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
        return texture;
    }

    public Texture2D CreateTexture(string assetName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(assetName, nameof(assetName));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if(textures.ContainsKey(assetName))
            throw new InvalidOperationException($"Unable to create a Texture, because one already exists with name: '{assetName}'");

        using (stream)
        {
            var texture = Texture2D.FromStream(RenderingSystem.GraphicsDevice, stream);
            textures[assetName] = texture;
            return texture;
        }
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
}
        // NullTexture = LoadTexture("null_texture.png");
        // NullTextureRegion = NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

        // GenericFontBold = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Bold");
        // GenericFontMedium = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Medium");
        // GenericFontRegular = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Regular");

        // TileShader = ContentManager.Load<Effect>("Effects/SpriteShader");

