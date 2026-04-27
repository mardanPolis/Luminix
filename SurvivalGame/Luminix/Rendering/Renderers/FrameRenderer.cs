using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Rendering.RenderItems;
using Luminix.Resources;

namespace Luminix.Rendering;

internal sealed class FrameRenderer
{
    private struct InstanceData : IVertexType
    {
        public Vector3 Position;
        public Vector2 SourceRectPos;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
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
    public void RenderFrame(RenderStack renderStack, Viewport viewport, float partialTick)
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
        Render(renderStack, viewMatrix * projection);
    }

    private void Render(RenderStack renderStack, Matrix matrixTransform)
    {
        int i = 0;
        int count = 0;
        int dataSetLength = 0;

        try
        {
            instancingEffect.Parameters["MatrixTransform"].SetValue(matrixTransform);

            // var spritesBatch = renderStack.SpriteBatchRenderItemStack;
            // var spritesBatchDataSet = spritesBatch.DataSet;
            // var batchPositionsDataSet = renderStack.BatchPositions.DataSet;
            // var spritesBatchCount = spritesBatch.Count;

            // int instanceIndex = 0;
            // Texture2D texture = null;

            // i = 0;
            // count = spritesBatchCount;
            // dataSetLength = spritesBatchDataSet.Length;
            // for (i = 0; i < spritesBatchCount; i++)
            // {
            //     ref var renderItem = ref spritesBatchDataSet[i];
            //     var tex = renderItem.Sprite.TextureRegion.Texture;
            //     var origin = renderItem.Sprite.Origin;
            //     var scale = renderItem.Scale;

            //     Console.WriteLine(scale);

            //     if(tex != texture && texture != null)
            //         Flush(ref instanceIndex);

            //     texture = tex;

            //     if(texture == null)
            //         break;

            //     instancingEffect.Parameters["SpriteTexture"].SetValue(texture);

            //     var source = renderItem.Sprite.TextureRegion.SourceRectangle;
            //     Vector4 sourceVec = new Vector4(source.X, source.Y, source.Width, source.Height);
                
            //     int sourceIndex = renderItem.StartIndex;
            //     int destinationIndex = renderItem.Length;

            //     for (int j = 0; j < destinationIndex; j++)
            //     {
            //         if(instanceIndex >= instanceDataArray.Length)
            //             Flush(ref instanceIndex);

            //         instanceDataArray[instanceIndex].Position =  batchPositionsDataSet[j];
            //         instanceIndex++;
            //     }
            // } 

            //Flush(ref instanceIndex);
        }
        catch
        {
            Logger.Get().Err($"Exception happened during batch rendering: RenderStack[{renderStack.stackID}] -> (Index/Count/Length : {i}/{count}/{dataSetLength})");
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