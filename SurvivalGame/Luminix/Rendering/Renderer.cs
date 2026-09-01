using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Luminix.Threading.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix.Rendering;

internal sealed class Renderer
{
    private const string MATRIX_PARAMETER = "MatrixTransform";
    private const string TEXTURE_PARAMETER = "Texture";
    private const string TEXTURE_SIZE_PARAMETER = "TextureSize";

    private struct Data : IVertexType
    {
        public Vector3 Position;
        public Vector2 Scale;
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
    public Renderer()
    {
        renderingSystem = Workspace.Current.GetService<RenderingSystem>();
        resources = renderingSystem.ResourceManager.Resources;
        spriteBatch = renderingSystem.SpriteBatch;

        defaultShader = resources.GetResource<ShaderResource>("Assets.Built-In.Shaders.GenericShader.xnb");
        defaultTexture = resources.GetResource<TextureResource>("NullTexture");
        defaultFontBold = resources.GetResource<FontResource>("Assets.Built-In.Fonts.IBMPlexMono-Bold.xnb");
        defaultFontMedium = resources.GetResource<FontResource>("Assets.Built-In.Fonts.IBMPlexMono-Medium.xnb");
        defaultFontRegular = resources.GetResource<FontResource>("Assets.Built-In.Fonts.IBMPlexMono-Regular.xnb");

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

    private readonly RenderingSystem  renderingSystem;
    private readonly Resources resources;
    private readonly SpriteBatch      spriteBatch;

    private readonly ShaderResource  defaultShader;
    private readonly TextureResource defaultTexture;
    private readonly FontResource    defaultFontBold;
    private readonly FontResource    defaultFontMedium;
    private readonly FontResource    defaultFontRegular;

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

    public void Render()
    {
        Matrix? transformMatrix = renderingSystem.TransformMatrix;

        if(!transformMatrix.HasValue) return;
        if(renderBuffer == null) return;
        
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
            var commands = renderBuffer.GetWorldRenderCommands();
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
                var commandDataType = command.Type;
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
                            Scale     = instance.Scale,
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
                        Vector2 size     = Vector2.Lerp(instance.Scale0, instance.Scale, partialTick);
                        float   rotation = MathHelper.Lerp(instance.Rotation0, instance.Rotation, partialTick);
 
                        float texW = textureSource.Width;
                        float texH = textureSource.Height;
 
                        flushed = false;
                        dataArray[currentInstanceCount++] = new Data
                        {
                            Position = position,
                            Scale     = size,
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
                        //FIXME : Shader ignored

                        var instance = nonInterpolatedTexts[commandDataIndex];
 
                        if (frustumCulledGroups.Contains(instance.Group))
                            continue;

                        spriteBatch.DrawString(instance.Font.Source, instance.Text, new Vector2(instance.Position.X, instance.Position.Y), Color.White,
                        instance.Rotation, new Vector2(0, 0), instance.Scale, SpriteEffects.None, instance.Position.Z);
                        break;
                    }
 
                    case RenderBuffer.RenderObjectType.TextInterpolated:
                    {
                        //FIXME : Shader ignored

                        var instance = interpolatedTexts[commandDataIndex];

                        var position = Vector3.Lerp(instance.Position0, instance.Position, partialTick);
                        var size = Vector2.Lerp(instance.Scale0, instance.Scale, partialTick);
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
        catch(Exception ex)
        {
            Logger.Get().NewLog(Logger.Log.Level.Error, $"Exception happened during rendering (CommandIndex/CommandCount : {commandIndex}/{commandCount}), reason: {ex}");
            throw;
        }

        spriteBatch.End();
    }

    public void UnscaledRender()
    {
        if(renderBuffer == null)
            return;

        int commandIndex = 0;
        int commandCount = 0;

        int imageCount = 0;
        int labelCount = 0;

        RenderBuffer.UiRenderObjectType commandDataType = RenderBuffer.UiRenderObjectType.Image;

        try
        {
            var commands = renderBuffer.GetUiRenderCommands();
            commandCount = commands.Length;
 
            var images = renderBuffer.GetImages();
            var labels = renderBuffer.GetLabels();
            imageCount = images.Length;
            labelCount = labels.Length;
            
            ShaderResource currentShader = null;

            spriteBatch.Begin(sortMode:SpriteSortMode.Deferred);
            for (commandIndex = 0; commandIndex < commandCount; commandIndex++)
            {
                var command         = commands[commandIndex];
                var commandDataIndex = command.DataIndex;
                commandDataType = command.Type;

                switch(commandDataType)
                {
                    case RenderBuffer.UiRenderObjectType.Image:
                        var image = images[commandDataIndex];

                        if(image.Shader != currentShader)
                        {
                            currentShader = image.Shader;
                            spriteBatch.End();
                            spriteBatch.Begin(effect:currentShader?.Source);
                        }

                        spriteBatch.Draw(image.Texture.Source, new Vector2(image.X, image.Y), image.Texture.Rectangle, image.Color, image.Rotation, Vector2.Zero, new Vector2(image.Width, image.Height), SpriteEffects.None, 0f);
                    break;
                    case RenderBuffer.UiRenderObjectType.Label:

                        var label = labels[commandDataIndex];

                        if(label.Shader != currentShader)
                        {
                            currentShader = label.Shader;
                            spriteBatch.End();
                            spriteBatch.Begin(effect:currentShader?.Source);
                        }

                        spriteBatch.DrawString(label.Font.Source, label.Text, new Vector2(label.X, label.Y), label.Color);
                    break;
                }

            }
            spriteBatch.End();

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
        catch
        {
            switch(commandDataType)
            {
                case RenderBuffer.UiRenderObjectType.Image:
                    Logger.Get().NewLog(Logger.Log.Level.Error, $"Exception happened during rendering (CommandIndex/CommandCount/ImageCount : {commandIndex}/{commandCount}/{imageCount})");
                break;
                case RenderBuffer.UiRenderObjectType.Label:
                    Logger.Get().NewLog(Logger.Log.Level.Error, $"Exception happened during rendering (CommandIndex/CommandCount/LabelCount : {commandIndex}/{commandCount}/{labelCount})");
                break;
            }
            throw;
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
    
    //TODO : Complete Frustum Culling

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