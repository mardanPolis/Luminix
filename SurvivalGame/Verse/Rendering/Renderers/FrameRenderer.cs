
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

internal sealed class FrameRenderer : IFrameRenderer
{
    private Camera camera;
    private SpriteBatch spriteBatch;

    public FrameRenderer(Camera camera, SpriteBatch spriteBatch)
    {
        this.camera = camera;
        this.spriteBatch = spriteBatch;
    }

    public void DrawFrame(RenderStack hotRenderStack, RenderStack renderStack, float partialTick)
    {
        RenderingSystem.GraphicsDevice.Clear(Color.CornflowerBlue);

        spriteBatch.Begin(transformMatrix: camera.GetViewMatrix());

        DrawRenderStack(renderStack, partialTick);
        DrawHotRenderStack(hotRenderStack);

        spriteBatch.End();
    }

    private void DrawRenderStack(RenderStack renderStack, float partialTick)
    {
        int i = 0;

        for (i = 0; i < renderStack.InterpolatedSpriteCount; i++)
            DrawSprite(ref renderStack.InstanceInterpolatedSprites[i], partialTick);

        for (i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
        

    }
    private void DrawHotRenderStack(RenderStack renderStack)
    {
        int i = 0;
        for (i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref InstanceInterpolatedSprite spriteRenderData, float t)
    {
        var pos = spriteRenderData.Position0 + (spriteRenderData.Position - spriteRenderData.Position0) * t;
        pos.Y = -pos.Y;
        var rot = spriteRenderData.Rotation0 + (spriteRenderData.Rotation - spriteRenderData.Rotation0) * t;
        var scale = spriteRenderData.Scale0 + (spriteRenderData.Scale - spriteRenderData.Scale0) * t;

        spriteBatch.Draw(
            spriteRenderData.Sprite.TextureRegion.Texture,
            pos,
            spriteRenderData.Sprite.TextureRegion.SourceRectangle,
            spriteRenderData.Sprite.Color,
            rot,
            spriteRenderData.Sprite.Origin,
            scale,
            spriteRenderData.Sprite.Effects,
            spriteRenderData.LayerDepth
        );
    }

    private void DrawSprite(ref InstanceSprite spriteRenderData)
    {
        var pos = spriteRenderData.Position;
        pos.Y = -pos.Y;

        spriteBatch.Draw(
            spriteRenderData.Sprite.TextureRegion.Texture,
            pos,
            spriteRenderData.Sprite.TextureRegion.SourceRectangle,
            spriteRenderData.Sprite.Color,
            spriteRenderData.Rotation,
            spriteRenderData.Sprite.Origin,
            spriteRenderData.Scale,
            spriteRenderData.Sprite.Effects,
            spriteRenderData.LayerDepth
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawLine(ref InstanceLine lineRenderData)
    {
        
    }
}

internal sealed class DebugFrameRenderer : IFrameRenderer
{
    private Camera camera;
    private SpriteBatch spriteBatch;
    private Texture2D currTexture;
    private ulong drawCalls = 0;

    public DebugFrameRenderer(Camera camera, SpriteBatch spriteBatch)
    {
        this.camera = camera;
        this.spriteBatch = spriteBatch;
    }

    public void DrawFrame(RenderStack hotRenderStack, RenderStack renderStack, float partialTick)
    {
        drawCalls = 0;
        currTexture = null;
        RenderingSystem.GraphicsDevice.Clear(Color.CornflowerBlue);

        spriteBatch.Begin(transformMatrix: camera.GetViewMatrix());

        DrawRenderStack(renderStack, partialTick);
        DrawHotRenderStack(hotRenderStack);

        spriteBatch.End();
    }


    private void DrawRenderStack(RenderStack renderStack, float partialTick)
    {
        int i = 0;

        for (i = 0; i < renderStack.InterpolatedSpriteCount; i++)
            DrawSprite(ref renderStack.InstanceInterpolatedSprites[i], partialTick);

        for (i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
        

    }
    private void DrawHotRenderStack(RenderStack renderStack)
    {
        int i = 0;
        for (i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.InstanceSprites[i]);

        for (i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.InstanceLines[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref InstanceInterpolatedSprite spriteRenderData, float t)
    {
        DrawCall(spriteRenderData.Sprite.TextureRegion.Texture);
        
        var pos = spriteRenderData.Position0 + (spriteRenderData.Position - spriteRenderData.Position0) * t;
        pos.Y = -pos.Y;
        var rot = spriteRenderData.Rotation0 + (spriteRenderData.Rotation - spriteRenderData.Rotation0) * t;
        var scale = spriteRenderData.Scale0 + (spriteRenderData.Scale - spriteRenderData.Scale0) * t;

        spriteBatch.Draw(
            spriteRenderData.Sprite.TextureRegion.Texture,
            pos,
            spriteRenderData.Sprite.TextureRegion.SourceRectangle,
            spriteRenderData.Sprite.Color,
            rot,
            spriteRenderData.Sprite.Origin,
            scale,
            spriteRenderData.Sprite.Effects,
            spriteRenderData.LayerDepth
        );
    }

    private void DrawSprite(ref InstanceSprite spriteRenderData)
    {
        DrawCall(spriteRenderData.Sprite.TextureRegion.Texture);

        var pos = spriteRenderData.Position;
        pos.Y = -pos.Y;

        spriteBatch.Draw(
            spriteRenderData.Sprite.TextureRegion.Texture,
            pos,
            spriteRenderData.Sprite.TextureRegion.SourceRectangle,
            spriteRenderData.Sprite.Color,
            spriteRenderData.Rotation,
            spriteRenderData.Sprite.Origin,
            spriteRenderData.Scale,
            spriteRenderData.Sprite.Effects,
            spriteRenderData.LayerDepth
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawLine(ref InstanceLine lineRenderData)
    {
    }

    private void DrawCall(Texture2D texture)
    {
        if(currTexture != texture)
        {
            currTexture = texture;
            drawCalls++;
        }
    }

    public ulong DrawCalls => drawCalls;
}