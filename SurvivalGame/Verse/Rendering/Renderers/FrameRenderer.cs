
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering.RenderData;

namespace Verse.Rendering;

internal interface IFrameRenderer
{
    void DrawFrame(RenderStack hotRenderStack, RenderStack renderStack, float partialTick);
}
internal abstract class BaseFrameRenderer : IFrameRenderer
{
    protected BaseFrameRenderer(SpriteBatch spriteBatch)
    {
        this.spriteBatch = spriteBatch;
    }

    protected SpriteBatch spriteBatch;

    public virtual void DrawFrame(RenderStack hotRenderStack, RenderStack renderStack, float partialTick)
    {
        RenderingSystem.GraphicsDevice.Clear(Color.CornflowerBlue);
        spriteBatch.Begin(transformMatrix: renderStack.GenerateCameraViewMatrix(partialTick));
        DrawRenderStack(renderStack, partialTick);
        spriteBatch.End();

        spriteBatch.Begin();
        DrawHotRenderStack(hotRenderStack);
        spriteBatch.End();
    }

    protected abstract void DrawRenderStack(RenderStack renderStack, float t);
    protected abstract void DrawHotRenderStack(RenderStack renderStack);

    // Abstract hooks for per-class behavior
    protected abstract void DrawSprite(ref InstanceInterpolatedSprite sprite, float t);
    protected abstract void DrawSprite(ref InstanceSprite sprite);
    protected abstract void DrawLine(ref InstanceLine line);
}

internal sealed class FrameRenderer : BaseFrameRenderer
{
    public FrameRenderer(SpriteBatch spriteBatch) 
        : base(spriteBatch) { }

    protected sealed override void DrawRenderStack(RenderStack renderStack, float t)
    {
        for (int i = 0; i < renderStack.InterpolatedSpriteCount; i++)
            DrawSprite(ref renderStack.InstanceInterpolatedSprites[i], t);

        for (int i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (int i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
    }


    protected sealed override void DrawHotRenderStack(RenderStack renderStack)
    {
        for (int i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (int i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void DrawSprite(ref InstanceInterpolatedSprite sprite, float t)
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
    protected override void DrawSprite(ref InstanceSprite sprite)
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

    protected override void DrawLine(ref InstanceLine line)
    {
        // implement line drawing
    }
}

internal sealed class DebugFrameRenderer : BaseFrameRenderer
{
    public DebugFrameRenderer(SpriteBatch spriteBatch) 
        : base(spriteBatch) { }

    private Texture2D currTexture;
    private ulong drawCalls;

    public override void DrawFrame(RenderStack hotRenderStack, RenderStack renderStack, float partialTick)
    {
        drawCalls = 0;
        currTexture = null;

        RenderingSystem.GraphicsDevice.Clear(Color.Black);
        if(renderStack.CanRender())
        {
            RenderingSystem.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(transformMatrix: renderStack.GenerateCameraViewMatrix(partialTick));
            DrawRenderStack(renderStack, partialTick);
            spriteBatch.End();
        }

        spriteBatch.Begin();
        DrawHotRenderStack(hotRenderStack);
        spriteBatch.End();
    }

    protected sealed override void DrawRenderStack(RenderStack renderStack, float t)
    {
        for (int i = 0; i < renderStack.InterpolatedSpriteCount; i++)
            DrawSprite(ref renderStack.InstanceInterpolatedSprites[i], t);

        for (int i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (int i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
    }

    protected sealed override void DrawHotRenderStack(RenderStack renderStack)
    {
        for (int i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (int i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void DrawSprite(ref InstanceInterpolatedSprite sprite, float t)
    {
        TrackDrawCall(sprite.Sprite.TextureRegion.Texture);

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
    protected override void DrawSprite(ref InstanceSprite sprite)
    {
        TrackDrawCall(sprite.Sprite.TextureRegion.Texture);

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

    protected override void DrawLine(ref InstanceLine line)
    {
        // implement line drawing
    }

    private void TrackDrawCall(Texture2D texture)
    {
        if (currTexture != texture)
        {
            currTexture = texture;
            drawCalls++;
        }
    }

    public ulong DrawCalls => drawCalls;
}