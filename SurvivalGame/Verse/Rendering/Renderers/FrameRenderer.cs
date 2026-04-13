using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering.RenderItems;
using Verse.Resources;

namespace Verse.Rendering;

internal sealed class FrameRenderer
{
    private struct InstanceData : IVertexType
    {
        public Vector3 Position;
        public Vector2 Origin;
        public Vector2 Scale;
        public Vector4 SourceRect;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(28, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }

    public FrameRenderer(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        this.spriteBatch = spriteBatch;

        var vertices = new VertexPositionColorTexture[] {
            new (new Vector3(0, 0, 0), Color.White, new Vector2(0, 0)),
            new (new Vector3(1, 0, 0), Color.White, new Vector2(1, 0)),
            new (new Vector3(1, 1, 0), Color.White, new Vector2(1, 1)),
            new (new Vector3(0, 1, 0), Color.White, new Vector2(0, 1))
        };
        quadBuffer = new VertexBuffer(device, typeof(VertexPositionColorTexture), 4, BufferUsage.WriteOnly);
        quadBuffer.SetData(vertices);

        short[] indices = { 0, 1, 2, 0, 2, 3 };
        indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);
        
        instanceDataArray = new InstanceData[50000];
        instanceBuffer = new DynamicVertexBuffer(device, typeof(InstanceData), 50000, BufferUsage.WriteOnly);

        instancingEffect = ResourceManager.GenericShader;

        bindings =
        [
            new VertexBufferBinding(quadBuffer),
            new VertexBufferBinding(instanceBuffer, 0, 1)
        ];

        this.device = device;
    }

    private DynamicVertexBuffer instanceBuffer;
    private VertexBuffer quadBuffer;
    private IndexBuffer indexBuffer;
    private Effect instancingEffect;
    private VertexBufferBinding[] bindings;
    private InstanceData[] instanceDataArray;

    private GraphicsDevice device;

    private Viewport lastViewport;

    private Matrix projection;

    private SpriteBatch spriteBatch;
    public void DrawFrame(RenderStack renderStack, Viewport viewport, float partialTick)
    {
        if (viewport.Width != lastViewport.Width || viewport.Height != lastViewport.Height)
        {
            Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, 4f, -4f, out projection);
            if (device.UseHalfPixelOffset)
            {
                projection.M41 += -0.5f * projection.M11;
                projection.M42 += -0.5f * projection.M22;
            }

            lastViewport = viewport;
        }

        device.Clear(Color.CornflowerBlue);
        var viewMatrix = renderStack.Camera.GetViewMatrix(partialTick, viewport.Height, viewport.Width);
        device.RasterizerState = RasterizerState.CullNone;
        BatchRender(renderStack, viewMatrix * projection);
        Render(renderStack, viewMatrix, partialTick);
    }

    private void Render(RenderStack renderStack, Matrix viewMatrix, float partialTick)
    {
        int i = 0;
        int count = 0;
        int length = 0;
        string typeRendered = "0";

        spriteBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.Deferred, depthStencilState: DepthStencilState.Default,
        transformMatrix: viewMatrix);

        try
        {
            //spriteBatch.Draw(ResourceManager.NullTexture, new Vector2(0, 0), null, Color.White, 0, new Vector2(ResourceManager.NullTexture.Width / 2, ResourceManager.NullTexture.Height / 2), Vector2.One, SpriteEffects.None, 0);

            var interpSprites = renderStack.InterpolatedSpriteRenderItemStack;
            var interpSpritesDataSet = interpSprites.DataSet;
            var interpSpritesCount = interpSprites.Count;

            var sprites = renderStack.SpriteRenderItemStack;
            var spritesDataSet = sprites.DataSet;
            var spritesCount = sprites.Count;

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

            count = lineCount;
            typeRendered = "Lines";
            length = linesDataSet.Length;
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


    private void BatchRender(RenderStack renderStack, Matrix matrixTransform)
    {
        int i = 0;
        int count = 0;
        int dataSetLength = 0;
        string typeRendered = "0";

        try
        {
            instancingEffect.Parameters["MatrixTransform"].SetValue(matrixTransform);

            var spritesBatch = renderStack.SpriteBatchRenderItemStack;
            var spritesBatchDataSet = spritesBatch.DataSet;
            var batchPositionsDataSet = renderStack.BatchPositions.DataSet;
            var spritesBatchCount = spritesBatch.Count;

            int instanceIndex = 0;
            Texture2D texture = null;

            i = 0;
            count = spritesBatchCount;
            dataSetLength = spritesBatchDataSet.Length;
            typeRendered = "SpriteBatches";
            for (i = 0; i < spritesBatchCount; i++)
            {
                ref var renderItem = ref spritesBatchDataSet[i];
                var tex = renderItem.Sprite.TextureRegion.Texture;
                var origin = renderItem.Sprite.Origin;
                var scale = renderItem.Scale;

                Console.WriteLine(scale);

                if(tex != texture && texture != null)
                    Flush(ref instanceIndex);

                texture = tex;

                if(texture == null)
                    break;

                instancingEffect.Parameters["SpriteTexture"].SetValue(texture);

                var source = renderItem.Sprite.TextureRegion.SourceRectangle;
                Vector4 sourceVec = new Vector4(source.X, source.Y, source.Width, source.Height);
                
                int sourceIndex = renderItem.StartIndex;
                int destinationIndex = renderItem.Length;

                for (int j = 0; j < destinationIndex; j++)
                {
                    if(instanceIndex >= instanceDataArray.Length)
                        Flush(ref instanceIndex);

                    instanceDataArray[instanceIndex].Position =  batchPositionsDataSet[j];
                    instanceDataArray[instanceIndex].Origin = origin;
                    instanceDataArray[instanceIndex].Scale = scale;
                    instanceDataArray[instanceIndex].SourceRect = sourceVec;
                    instanceIndex++;
                }
            } 

            Flush(ref instanceIndex);
        }
        catch
        {
            Logger.Get().Err($"Exception happened during batch rendering: RenderStack[{renderStack.stackID}] '{typeRendered}' -> (Index/Count/Length : {i}/{count}/{dataSetLength})");
            throw;
        }
    }
    private void Flush(ref int currentInstanceCount)
    {
        if (currentInstanceCount == 0) return;

        instanceBuffer.SetData(instanceDataArray, 0, currentInstanceCount, SetDataOptions.Discard);

        device.Indices = indexBuffer;
        device.SetVertexBuffers(bindings);

        foreach (var pass in instancingEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, currentInstanceCount);
        }
    
        currentInstanceCount = 0;
    }
}