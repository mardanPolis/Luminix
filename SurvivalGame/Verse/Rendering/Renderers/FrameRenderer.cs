
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering.RenderData;

namespace Verse.Rendering;
internal class FrameRenderer
{
    private SpriteBatch spriteBatch;
    public void DrawFrame(RenderStack hotRenderStack, RenderStack renderStack)
    {
        spriteBatch.Begin();

        DrawRenderStack(renderStack);
        DrawRenderStack(hotRenderStack);

        spriteBatch.End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawRenderStack(RenderStack renderStack)
    {
        int i = 0;
        for (i = 0; i < renderStack.SpriteCount; i++)
            DrawSprite(ref renderStack.SpriteRenderDataSet[i]);

        for (i = 0; i < renderStack.LineCount; i++)
            DrawLine(ref renderStack.LineRenderDataSet[i]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(ref InstanceSprite spriteRenderData)
    {
        spriteBatch.Draw(
            spriteRenderData.Sprite.TextureRegion.Texture,
            spriteRenderData.Position,
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


    public void SetSpriteBatch(SpriteBatch spriteBatch)
    {
        this.spriteBatch = spriteBatch;
    }

}