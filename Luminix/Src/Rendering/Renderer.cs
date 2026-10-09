using System;
using System.Runtime.CompilerServices;
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
    private record struct GPUInstanceData(ShaderResource Shader, Resource Resource, Color Color, Transform Transform, string Text);
    
    public Renderer()
    {
        renderingSystem = Workspace.Current.GetService<RenderingSystem>();
        resources = renderingSystem.ResourceManager;
        spriteBatch = renderingSystem.SpriteBatch;

        defaultTexture = resources.NullTexture;
        defaultShader = resources.GenericShader;
        defaultFontBold = resources.GenericFontBold;
        defaultFontMedium = resources.GenericFontMedium;
        defaultFontRegular = resources.GenericFontRegular;

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
        
        dataArray = new Data[4096];
        dataBuffer = new DynamicVertexBuffer(renderingSystem.GraphicsDevice, typeof(Data), 4096, BufferUsage.WriteOnly);

        bindings =
        [
            new VertexBufferBinding(quadBuffer),
            new VertexBufferBinding(dataBuffer, 0, 1)
        ];

        this.device = renderingSystem.GraphicsDevice;
    }

    private readonly RenderingSystem  renderingSystem;
    private readonly GraphicResourceManager resources;
    private readonly SpriteBatch      spriteBatch;

    private readonly Effect     defaultShader;
    private readonly Texture2D  defaultTexture;
    private readonly SpriteFont defaultFontBold;
    private readonly SpriteFont defaultFontMedium;
    private readonly SpriteFont defaultFontRegular;

    private DynamicVertexBuffer dataBuffer;
    private IndexBuffer indexBuffer;
    private VertexBuffer quadBuffer;
    private VertexBufferBinding[] bindings;

    private Data[] dataArray;

    private Microsoft.Xna.Framework.Graphics.GraphicsDevice device;

    private RenderCommand[] commands = new RenderCommand[4096];
    private GPUInstanceData[] gpuInstanceBuffer = new GPUInstanceData[4096];
    private int activeCommandCount = 0;

    private void SetData(ReadOnlySpan<RenderInstance> instances, float partialTick)
    {
        activeCommandCount = instances.Length;
        if (activeCommandCount == 0) return;

        EnsureCapacity(activeCommandCount);

        for (int i = 0; i < instances.Length; i++)
        {
            ref readonly var instance = ref instances[i];

            var transform = Interpolate(instance.Transform, partialTick);

            ushort layerBits = (ushort)Math.Clamp((int)(transform.Position.X * 100.0f), 0, 65535);

            var key = BuildKey(layerBits, instance.Shader, instance.Resource);

            commands[i] = new RenderCommand(key, i);
            gpuInstanceBuffer[i] = new GPUInstanceData(instance.Shader, instance.Resource, instance.Color, transform, instance.Text);
        }

        Array.Sort(commands, 0, activeCommandCount);
    }

    private void SetData(ReadOnlySpan<RenderInstanceUI> instances)
    {
        activeCommandCount = instances.Length;
        if (activeCommandCount == 0) return;

        EnsureCapacity(activeCommandCount);

        for (int i = 0; i < instances.Length; i++)
        {
            ref readonly var instance = ref instances[i];

            var transform = new Transform(new Vector3(instance.X, instance.Y, 0), instance.Rotation);

            ushort layerBits = (ushort)Math.Clamp((int)(transform.Position.X * 100.0f), 0, 65535);

            var key = BuildKey(layerBits, instance.Shader, instance.Resource);

            commands[i] = new RenderCommand(key, i);
            gpuInstanceBuffer[i] = new GPUInstanceData(instance.Shader, instance.Resource, instance.Color, transform, instance.Text);
        }

    }

    public void Render()
    {
        Matrix? transformMatrix = renderingSystem.TransformMatrix;

        if(!transformMatrix.HasValue) return;
        if(activeCommandCount == 0) return;
        
        Matrix transformMatrixValue = transformMatrix.Value;

        spriteBatch.Begin(transformMatrix:transformMatrixValue, sortMode:SpriteSortMode.Immediate);

        int commandIndex = 0;
        int commandCount = 0;
        int instanceIndex = 0;
        int currentInstanceCount = 0;
 
        try
        {
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
                        var transform = instance.Transform;
 
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
                            Position = transform.Position,
                            Scale    = transform.Scale,
                            Rotation = transform.Rotation,
                            RectPos  = new Vector2(textureRegion.X, textureRegion.Y),
                            RectSize = new Vector2(textureRegion.Width, textureRegion.Height)
                        };

                        if (currentInstanceCount >= dataArray.Length)
                        {
                            Flush(prevEffect, ref flushed, ref currentInstanceCount);
                        }
 
                        break;
                    }
 
                    case RenderBuffer.RenderObjectType.TextNonInterpolated:
                    {
                        //FIXME : Shader ignored

                        var instance = nonInterpolatedTexts[commandDataIndex];
                        var transform = instance.Transform;

                        if (frustumCulledGroups.Contains(instance.Group))
                            continue;

                        spriteBatch.DrawString(instance.Font?.Source ?? defaultFontRegular.Source, instance.Text, new Vector2(transform.Position.X, transform.Position.Y), Color.White,
                        transform.Rotation, new Vector2(0, 0), transform.Scale, SpriteEffects.None, transform.Position.Z);
                        break;
                    }
                }
            }
 
            if (currentInstanceCount > 0 && prevEffect != null)
                Flush(prevEffect, ref flushed, ref currentInstanceCount);
        }
        catch(Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Exception happened during rendering (CommandIndex/CommandCount : {commandIndex}/{commandCount}), reason: {ex}");
            throw;
        }

        spriteBatch.End();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)] private void EnsureCapacity(int requiredCapacity)
    {
        if (commands.Length < requiredCapacity)
        {
            int newCapacity = Math.Max(commands.Length * 2, requiredCapacity);
            Array.Resize(ref commands, newCapacity);
            Array.Resize(ref gpuInstanceBuffer, newCapacity);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static ulong BuildKey(uint layer, ShaderResource shader, Resource resource)
    {
        uint resourceIndex = resource is TextureResource texture ? texture.Index : ((FontResource)resource).Index;

        ulong layerBits = ((ulong)layer & 0xFFFF) << 48;
        ulong shaderBits = ((ulong)(shader.Index & 0xFF_FFFF)) << 24;
        ulong textureBits = (ulong)(resourceIndex & 0xFF_FFFF);

        return layerBits | shaderBits | textureBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    private static Transform Interpolate(in TransformInterp transform, float partialTick)
    {
        return new Transform(
            Vector3.Lerp(transform.Position0, transform.Position, partialTick),
            Vector2.Lerp(transform.Scale0, transform.Scale, partialTick),
            MathHelper.Lerp(transform.Rotation0, transform.Rotation, partialTick)
        );
    }
}