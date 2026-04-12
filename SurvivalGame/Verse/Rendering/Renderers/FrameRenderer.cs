using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering.RenderInstances;
using Verse.Resources;

namespace Verse.Rendering;

internal sealed class FrameRenderer
{
    public FrameRenderer(SpriteBatch spriteBatch)
    {
        this.spriteBatch = spriteBatch;
    }

    private SpriteBatch spriteBatch;
    public void DrawFrame(RenderStack renderStack, Viewport viewport, float partialTick)
    {
        RenderingSystem.GraphicsDevice.Clear(Color.CornflowerBlue);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.Deferred, depthStencilState: DepthStencilState.Default,
        transformMatrix: renderStack.GenerateCameraViewMatrix(partialTick, viewport.Height, viewport.Width));

        int count = 0;
        int i = 0;
        int length = 0;
        string typeRendered = "";

        try
        {
            var interpSpritesCount = renderStack.InterpolatedSpriteCount;
            var interpSprites = renderStack.InstanceInterpolatedSprites;

            var spritesCount = renderStack.SpriteCount;
            var sprites = renderStack.InstanceSprites;

            var lineCount = renderStack.LineCount;
            var lines = renderStack.InstanceLines;

            var textCount = renderStack.TextCount;
            var texts = renderStack.InstanceText;
            
            count = interpSpritesCount;
            typeRendered = "Interpolated Sprites";
            length = interpSprites.Length;
            for (i = 0; i < interpSpritesCount; i++)
                DrawSprite(ref interpSprites[i], partialTick);

            count = spritesCount;
            typeRendered = "Sprites";
            length = sprites.Length;
            for (i = 0; i < spritesCount; i++)
                DrawSprite(ref sprites[i]);

            count = lineCount;
            typeRendered = "Lines";
            length = lines.Length;
            for (i = 0; i < lineCount; i++)
                DrawLine(ref lines[i]);

            count = textCount;
            typeRendered = "Text";
            length = texts.Length;
            for (i = 0; i < textCount; i++)
                DrawText(ref texts[i]);
        }
        catch
        {
            Logger.Get().Err($"Exception happened during rendering: RenderStack[{renderStack.stackID}] '{typeRendered}' -> (Index/Count/Length : {i}/{count}/{length})");
            throw;
        }

        spriteBatch.End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref InstanceInterpolatedSprite sprite, float t)
    {
        var pos = sprite.Position0 + (sprite.Position - sprite.Position0) * t;
        pos.Y = -pos.Y;

        var rot = sprite.Rotation0 + (sprite.Rotation - sprite.Rotation0) * t;
        var scale = sprite.Scale0 + (sprite.Scale - sprite.Scale0) * t;

        spriteBatch.Draw(
            sprite.Sprite.TextureRegion.Texture,
            pos,
            sprite.Sprite.TextureRegion.SourceRectangle,
            sprite.Sprite.Color,
            rot,
            sprite.Sprite.Origin,
            scale,
            sprite.Sprite.Effects,
            sprite.LayerDepth
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref InstanceSprite sprite)
    {
        var pos = sprite.Position;
        pos.Y = -pos.Y;

        spriteBatch.Draw(
            sprite.Sprite.TextureRegion.Texture,
            pos,
            sprite.Sprite.TextureRegion.SourceRectangle,
            sprite.Sprite.Color,
            sprite.Rotation,
            sprite.Sprite.Origin,
            sprite.Scale,
            sprite.Sprite.Effects,
            sprite.LayerDepth
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawLine(ref InstanceLine line)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawText(ref InstanceText instance)
    {
        spriteBatch.DrawString(
            instance.Bold ? ResourceManager.GenericFontBold : ResourceManager.GenericFontMedium,
            instance.Text,
            instance.Position,
            instance.Color
        );
    }
}