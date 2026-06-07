using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix.Rendering;

internal sealed class BatchRenderer : ImmediateRenderer
{
    private struct Data : IVertexType
    {
        public Vector3 Position;
        public Vector2 Size;
        public float   Rotation;
        
        public Vector2 RectPos;
        public Vector2 RectSize;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0,  VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(20, VertexElementFormat.Single,  VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 4),
            new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }

    public BatchRenderer()
    {
        var vertices = new VertexPositionColorTexture[] {
            new (new Vector3(-0.5f, -0.5f, 0), Color.White, new Vector2(0, 0)),
            new (new Vector3(+0.5f, -0.5f, 0), Color.White, new Vector2(1, 0)),
            new (new Vector3(+0.5f, +0.5f, 0), Color.White, new Vector2(1, 1)),
            new (new Vector3(-0.5f, +0.5f, 0), Color.White, new Vector2(0, 1))
        };

        quadBuffer = new VertexBuffer(renderingSystem.GraphicsDevice, typeof(VertexPositionColorTexture), 4, BufferUsage.WriteOnly);
        quadBuffer.SetData(vertices);

        short[] indices = { 0, 1, 2, 0, 2, 3 };
        indexBuffer = new IndexBuffer(renderingSystem.GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);
        
        dataArray = new Data[8192];
        dataBuffer = new DynamicVertexBuffer(renderingSystem.GraphicsDevice, typeof(Data), 8192, BufferUsage.WriteOnly);

        bindings =
        [
            new VertexBufferBinding(quadBuffer),
            new VertexBufferBinding(dataBuffer, 0, 1)
        ];

        this.device = renderingSystem.GraphicsDevice;
    }

    private DynamicVertexBuffer dataBuffer;
    private IndexBuffer indexBuffer;
    private VertexBuffer quadBuffer;
    private VertexBufferBinding[] bindings;

    private Data[] dataArray;

    private GraphicsDevice device;

    private RenderBuffer renderBuffer;

    private float partialTick;


    private readonly HashSet<int> frustumCulledGroups = new();

    public void SetData(RenderBuffer buffer, float partialTick)
    {
        this.renderBuffer = buffer;
        this.partialTick = partialTick;
    }

    public sealed override void Render()
    {
        Matrix? transformMatrix = renderingSystem.TransformMatrix;

        if(!transformMatrix.HasValue) return;
        
        Matrix transformMatrixValue = transformMatrix.Value;

        spriteBatch.Begin(transformMatrix:transformMatrixValue, sortMode:SpriteSortMode.Immediate);

        Vector2 frustumTopLeft, frustumBtmRight;
        ComputeFrustumBounds(renderingSystem.ViewMatrix.Value, out frustumTopLeft, out frustumBtmRight);
 
        frustumCulledGroups.Clear();
        var groups = renderBuffer.GetGroups();
        for (int g = 0; g < groups.Length; g++)
        {
            var grp = groups[g];
            if (FrustumCulled(grp.topLeft, grp.btmRight, frustumTopLeft, frustumBtmRight))
                frustumCulledGroups.Add(g);
        }

        int commandIndex = 0;
        int commandCount = 0;
        int instanceIndex = 0;
        int currentInstanceCount = 0;
 
        try
        {
            var commands = renderBuffer.GetRenderCommands();
            commandCount = commands.Length;
 
            var nonInterpolatedSprites = renderBuffer.GetSprites();
            var nonInterpolatedTexts   = renderBuffer.GetTexts();
            var interpolatedSprites    = renderBuffer.GetInterpolatedSprites();
            var interpolatedTexts      = renderBuffer.GetInterpolatedTexts();
 
            Effect    prevEffect  = null;
            Texture2D prevTexture = null;
 
            bool flushed = false;

            for (commandIndex = 0; commandIndex < commandCount; commandIndex++, instanceIndex++)
            {
                var command         = commands[commandIndex];
                var commandDataType = command.type;
                var commandDataIndex = command.DataIndex;

                switch (commandDataType)
                {
                    case RenderBuffer.RenderObjectType.SpriteNonInterpolated:
                    {
                        var instance = nonInterpolatedSprites[commandDataIndex];
 
                        if (frustumCulledGroups.Contains(instance.Group))
                            continue;

                        var textureResource = instance.Texture ?? defaultTexture;
                        var shaderResource  = instance.Shader ?? defaultShader;

                        var textureSource = textureResource.Source;
                        var textureRegion = textureResource.Rectangle;
                        var effect        = shaderResource.Source;
 
                        if ((prevEffect != null && prevEffect != effect) ||
                            (prevTexture != null && prevTexture != textureSource))
                        {
                            Flush(prevEffect, ref flushed, ref currentInstanceCount);
                        }
 
                        if (prevEffect != effect || prevTexture != textureSource)
                        {
                            effect.Parameters[MATRIX_PARAMETER].SetValue(transformMatrixValue);
                            effect.Parameters[TEXTURE_PARAMETER].SetValue(textureSource);
                            effect.Parameters[TEXTURE_SIZE_PARAMETER].SetValue(new Vector2(textureSource.Width, textureSource.Height));
                        }
 
                        prevEffect  = effect;
                        prevTexture = textureSource;
 
                        float texW = textureSource.Width;
                        float texH = textureSource.Height;
 
                        flushed = false;
                        dataArray[currentInstanceCount++] = new Data
                        {
                            Position = instance.Position,
                            Size     = instance.Size,
                            Rotation = instance.Rotation,
                            RectPos  = new Vector2(textureRegion.X, textureRegion.Y),
                            RectSize = new Vector2(textureRegion.Width, textureRegion.Height)
                        };

                        if (currentInstanceCount >= dataArray.Length)
                        {
                            Flush(prevEffect, ref flushed, ref currentInstanceCount);
                        }
 
                        break;
                    }
 
                    case RenderBuffer.RenderObjectType.SpriteInterpolated:
                    {
                        var instance = interpolatedSprites[commandDataIndex];
 
                        var textureResource = instance.Texture ?? defaultTexture;
                        var shaderResource  = instance.Shader ?? defaultShader;

                        var textureSource = textureResource.Source;
                        var textureRegion = textureResource.Rectangle;
                        var effect        = shaderResource.Source;
 
                        if ((prevEffect != null && prevEffect != effect) ||
                            (prevTexture != null && prevTexture != textureSource))
                        {
                            Flush(prevEffect, ref flushed, ref currentInstanceCount);
                        }
 
                        if (prevEffect != effect || prevTexture != textureSource)
                        {
                            effect.Parameters[MATRIX_PARAMETER].SetValue(transformMatrixValue);
                            effect.Parameters[TEXTURE_PARAMETER].SetValue(textureSource);
                            effect.Parameters[TEXTURE_SIZE_PARAMETER].SetValue(new Vector2(textureSource.Width, textureSource.Height));
                        }
 
                        prevEffect  = effect;
                        prevTexture = textureSource;
 
                        Vector3 position = Vector3.Lerp(instance.Position0, instance.Position, partialTick);
                        Vector2 size =     Vector2.Lerp(instance.Size0, instance.Size, partialTick);
                        float   rotation = MathHelper.Lerp(instance.Rotation0, instance.Rotation, partialTick);
 
                        float texW = textureSource.Width;
                        float texH = textureSource.Height;
 
                        flushed = false;
                        dataArray[currentInstanceCount++] = new Data
                        {
                            Position = position,
                            Size     = size,
                            Rotation = rotation,
                            RectPos  = new Vector2(textureRegion.X / texW, textureRegion.Y / texH),
                            RectSize = new Vector2(textureRegion.Width / texW, textureRegion.Height / texH)
                        };
 
                        if (currentInstanceCount >= dataArray.Length)
                            Flush(prevEffect, ref flushed, ref currentInstanceCount);
 
                        break;
                    }
 
                    case RenderBuffer.RenderObjectType.TextNonInterpolated:
                    {
                        var instance = nonInterpolatedTexts[commandDataIndex];
 
                        if (frustumCulledGroups.Contains(instance.Group))
                            continue;

                        spriteBatch.DrawString(instance.Font.Source, instance.Text, new Vector2(instance.Position.X, instance.Position.Y), Color.White,
                        instance.Rotation, new Vector2(0, 0), instance.Size, SpriteEffects.None, instance.Position.Z);
                        break;
                    }
 
                    case RenderBuffer.RenderObjectType.TextInterpolated:
                    {
                        var instance = interpolatedTexts[commandDataIndex];

                        var position = Vector3.Lerp(instance.Position0, instance.Position, partialTick);
                        var size = Vector2.Lerp(instance.Size0, instance.Size, partialTick);
                        var rotation = MathHelper.Lerp(instance.Rotation0, instance.Rotation, partialTick);

                        spriteBatch.DrawString(instance.Font.Source, instance.Text, new Vector2(position.X, position.Y), Color.White,
                        instance.Rotation, new Vector2(0, 0), size, SpriteEffects.None, position.Z);
                        break;
                    }
                }
            }
 
            if (currentInstanceCount > 0 && prevEffect != null)
                Flush(prevEffect, ref flushed, ref currentInstanceCount);
        }
        catch
        {
            Logger.Get().Err(
                $"Exception happened during batch rendering: " +
                $"RenderStack[{renderBuffer.BufferID}] -> " +
                $"(CommandIndex/CommandCount : {commandIndex}/{commandCount})");
            throw;
        }

        spriteBatch.End();
    }

    public override void UnscaledRender()
    {
        if(!renderingSystem.TransformMatrix.HasValue)
        {
            spriteBatch.Begin();
            Vector2 textSize = defaultFontBold.Source.MeasureString("No camera detected!");
            Vector2 Center = new Vector2(
                renderingSystem.Viewport.Width / 2 - textSize.X / 2,
                renderingSystem.Viewport.Height / 2 - textSize.Y / 2
            );

            spriteBatch.DrawString(defaultFontBold.Source, "No camera detected!", Center, Color.White);
            spriteBatch.End();
        }
    }

    private void ComputeFrustumBounds(Matrix viewMatrix, out Vector2 topLeft, out Vector2 btmRight)
    {
        Matrix invView = Matrix.Invert(viewMatrix);
 
        float halfW = renderingSystem.Viewport.Width  * 0.5f;
        float halfH = renderingSystem.Viewport.Height * 0.5f;
 
        Vector2 camPos = new Vector2(invView.Translation.X, invView.Translation.Y);
 
        topLeft  = camPos - new Vector2(halfW, halfH);
        btmRight = camPos + new Vector2(halfW, halfH);
    }
 
    private bool FrustumCulled(Vector2 groupTopLeft, Vector2 groupBtmRight,
                                Vector2 frustumTopLeft, Vector2 frustumBtmRight)
    {
        return groupBtmRight.X < frustumTopLeft.X  ||
               groupTopLeft.X  > frustumBtmRight.X ||
               groupTopLeft.Y  < frustumBtmRight.Y ||
               groupBtmRight.Y > frustumTopLeft.Y;
    }


    private void Flush(Effect effect, ref bool flushed, ref int currentInstanceCount)
    {
        if (flushed) return;
        if (currentInstanceCount == 0) return;

        dataBuffer.SetData(dataArray, 0, currentInstanceCount, SetDataOptions.Discard);

        device.Indices = indexBuffer;
        device.SetVertexBuffers(bindings);

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, currentInstanceCount);
        }
    
        currentInstanceCount = 0;
        flushed = true;
    }
}