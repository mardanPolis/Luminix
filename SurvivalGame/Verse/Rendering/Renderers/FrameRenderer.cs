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
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.FrontToBack, transformMatrix: renderStack.GenerateCameraViewMatrix(partialTick, viewport.Height, viewport.Width));

        for (int i = 0; i < renderStack.InterpolatedSpriteCount; i++)
            DrawSprite(ref renderStack.InstanceInterpolatedSprites[i], partialTick);

        for (int i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (int i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);

        for (int i = 0; i < renderStack.TextCount; i++)
            DrawText(ref renderStack.InstanceText[i]);

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