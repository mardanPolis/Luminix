
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
        foreach(var spriteRenderData in renderStack.SpriteRenderDataSet)
            DrawSprite(spriteRenderData);
        
        foreach(var lineRenderData in renderStack.LineRenderDataSet)
            DrawLine(lineRenderData);

        //hot
        foreach(var spriteRenderData in hotRenderStack.SpriteRenderDataSet)
            DrawSprite(spriteRenderData);

        foreach(var lineRenderData in hotRenderStack.LineRenderDataSet)
            DrawLine(lineRenderData);

        spriteBatch.End();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSprite(InstanceSprite spriteRenderData)
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
    private void DrawLine(InstanceLine lineRenderData)
    {
        
    }


    public void SetSpriteBatch(SpriteBatch spriteBatch)
    {
        this.spriteBatch = spriteBatch;
    }

}