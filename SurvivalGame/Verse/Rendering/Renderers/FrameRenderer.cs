using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering.RenderItems;
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
        transformMatrix: renderStack.Camera.GetViewMatrix(partialTick, viewport.Height, viewport.Width));

        int count = 0;
        int i = 0;
        int length = 0;
        string typeRendered = "";

        try
        {
            var batchPositions = renderStack.BatchPositions;
            var batchPositionsDataSet = batchPositions.DataSet;

            var interpSprites = renderStack.InterpolatedSpriteRenderItemStack;
            var interpSpritesDataSet = interpSprites.DataSet;
            var interpSpritesCount = interpSprites.Count;

            var sprites = renderStack.SpriteRenderItemStack;
            var spritesDataSet = sprites.DataSet;
            var spritesCount = sprites.Count;

            var spritesBatch = renderStack.SpriteBatchRenderItemStack;
            var spritesBatchDataSet = spritesBatch.DataSet;
            var spritesBatchCount = spritesBatch.Count;

            var lines = renderStack.LineRenderItemStack;
            var linesDataSet = lines.DataSet;
            var lineCount = lines.Count;

            var texts = renderStack.TextRenderItemStack;
            var textDataSet = texts.DataSet;
            var textCount = texts.Count;
            
            count = interpSpritesCount;
            typeRendered = "Interpolated Sprites";
            length = interpSpritesDataSet.Length;
            for (i = 0; i < interpSpritesCount; i++)
                DrawSprite(ref interpSpritesDataSet[i], partialTick);

            count = spritesCount;
            typeRendered = "Sprites";
            length = spritesDataSet.Length;
            for (i = 0; i < spritesCount; i++)
                DrawSprite(ref spritesDataSet[i]);

            count = spritesBatchCount;
            typeRendered = "SpriteBatches";
            length = spritesBatchDataSet.Length;
            for (i = 0; i < spritesBatchCount; i++)
                DrawSpriteBatch(ref spritesBatchDataSet[i], batchPositionsDataSet);

            count = lineCount;
            typeRendered = "Lines";
            length = spritesBatchDataSet.Length;
            for (i = 0; i < lineCount; i++)
                DrawLine(ref linesDataSet[i]);

            count = textCount;
            typeRendered = "Text";
            length = textDataSet.Length;
            for (i = 0; i < textCount; i++)
                DrawText(ref textDataSet[i]);
        }
        catch
        {
            Logger.Get().Err($"Exception happened during rendering: RenderStack[{renderStack.stackID}] '{typeRendered}' -> (Index/Count/Length : {i}/{count}/{length})");
            throw;
        }

        spriteBatch.End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref RenderItemInterpSprite renderItem, float t)
    {
        var pos = renderItem.Position0 + (renderItem.Position - renderItem.Position0) * t;
        pos.Y = -pos.Y;

        var rot = renderItem.Rotation0 + (renderItem.Rotation - renderItem.Rotation0) * t;
        var scale = renderItem.Scale0 + (renderItem.Scale - renderItem.Scale0) * t;

        spriteBatch.Draw(
            renderItem.Sprite.TextureRegion.Texture,
            pos,
            renderItem.Sprite.TextureRegion.SourceRectangle,
            renderItem.Sprite.Color,
            rot,
            renderItem.Sprite.Origin,
            scale,
            renderItem.Sprite.Effects,
            renderItem.LayerDepth
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref RenderItemSprite renderItem)
    {
        var pos = renderItem.Position;
        pos.Y = -pos.Y;

        spriteBatch.Draw(
            renderItem.Sprite.TextureRegion.Texture,
            pos,
            renderItem.Sprite.TextureRegion.SourceRectangle,
            renderItem.Sprite.Color,
            renderItem.Rotation,
            renderItem.Sprite.Origin,
            renderItem.Scale,
            renderItem.Sprite.Effects,
            renderItem.LayerDepth
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSpriteBatch(ref RenderItemSpriteBatch renderItem, Vector2[] positions)
    {
        var texture = renderItem.Sprite.TextureRegion.Texture;
        var source = renderItem.Sprite.TextureRegion.SourceRectangle;
        var color = renderItem.Sprite.Color;
        var origin = renderItem.Sprite.Origin;
        var scale = renderItem.Scale;
        var effects = renderItem.Sprite.Effects;
        var layer = renderItem.LayerDepth;

        var startIndex = renderItem.StartIndex;
        var length = startIndex + renderItem.Length;

        for(int i = startIndex; i < length; i++)
        {
            var pos = positions[i];
            pos.Y = -pos.Y;

            spriteBatch.Draw(
                texture,
                pos,
                source,
                color,
                0,
                origin,
                scale,
                effects,
                layer
            );
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawLine(ref RenderItemLine renderItem)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawText(ref RenderItemText renderItem)
    {
        spriteBatch.DrawString(
            renderItem.Bold ? ResourceManager.GenericFontBold : ResourceManager.GenericFontMedium,
            renderItem.Text,
            renderItem.Position,
            renderItem.Color
        );
    }
}