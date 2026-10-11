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
        
        public Color   Color;

        public Vector2 RectPos;
        public Vector2 RectSize;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0,  VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(20, VertexElementFormat.Single,  VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(24, VertexElementFormat.Color,   VertexElementUsage.Color, 1),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 4),
            new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
    public Renderer()
    {
        this.device = Workspace.Current.GetService<RenderingSystem>().GraphicsDevice;

        var vertices = new VertexPositionColorTexture[] {
            new (new Vector3(-0.5f, -0.5f, 0), Color.White, new Vector2(0, 0)),
            new (new Vector3(+0.5f, -0.5f, 0), Color.White, new Vector2(1, 0)),
            new (new Vector3(+0.5f, +0.5f, 0), Color.White, new Vector2(1, 1)),
            new (new Vector3(-0.5f, +0.5f, 0), Color.White, new Vector2(0, 1))
        };

        quadBuffer = new VertexBuffer(device, typeof(VertexPositionColorTexture), 4, BufferUsage.WriteOnly);
        quadBuffer.SetData(vertices);

        short[] indices = { 0, 1, 2, 0, 2, 3 };
        indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);
        
        dataArray = new Data[4096];
        dataBuffer = new DynamicVertexBuffer(device, typeof(Data), 4096, BufferUsage.WriteOnly);

        bindings =
        [
            new VertexBufferBinding(quadBuffer),
            new VertexBufferBinding(dataBuffer, 0, 1)
        ];

    }

    private DynamicVertexBuffer dataBuffer;
    private IndexBuffer indexBuffer;
    private VertexBuffer quadBuffer;
    private VertexBufferBinding[] bindings;

    private Matrix transformMatrix;
    private Effect currShader;
    private object currTexture;

    private Data[] dataArray;
    private int currentInstanceCount;

    private bool flushed;

    private Microsoft.Xna.Framework.Graphics.GraphicsDevice device;

    public void SetTransformMatrix(in Matrix matrix)
    {
        transformMatrix = matrix;
    }

    public void Reset()
    {
        currShader = null;
        currTexture = null;
    }

    public void Render(Effect shader, Texture2D source, in Color color, in Rectangle destinationRect)
    {
        Render(shader, source, in color, in destinationRect, new Rectangle(0, 0, source.Width, source.Height));
    }
    public void Render(Effect shader, Texture2D source, in Color color, in Rectangle destinationRect, in Rectangle sourceRect)
    {
        Render(shader, source, in color, 
        new Vector3(destinationRect.X, destinationRect.Y, 0), new Vector2(destinationRect.Width, destinationRect.Height), 0,
        in sourceRect);
    }
    public void Render(Effect shader, Texture2D source, in Color color, in Vector3 position, in Vector2 scale, float rotation, in Rectangle sourceRect)
    {
        if (shader != currShader || source != currTexture)
        {
            Flush();

            currShader = shader;
            currTexture = source;

            shader.Parameters[MATRIX_PARAMETER].SetValue(transformMatrix);
            shader.Parameters[TEXTURE_PARAMETER].SetValue(source);
            shader.Parameters[TEXTURE_SIZE_PARAMETER].SetValue(new Vector2(source.Width, source.Height));
        }
        
        flushed = false;
        dataArray[currentInstanceCount++] = new Data
        {
            Position = position,
            Scale    = scale,
            Rotation = rotation,
            Color    = color,
            RectPos  = new Vector2(sourceRect.X, sourceRect.Y),
            RectSize = new Vector2(sourceRect.Width, sourceRect.Height)
        };

        if (currentInstanceCount >= dataArray.Length)
            Flush();
    }
    
    public void Flush()
    {
        if (flushed) return;
        if (currShader == null) return;
        if (currentInstanceCount == 0) return;

        dataBuffer.SetData(dataArray, 0, currentInstanceCount, SetDataOptions.Discard);

        device.Indices = indexBuffer;
        device.SetVertexBuffers(bindings);

        foreach (var pass in currShader.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, currentInstanceCount);
        }
    
        currentInstanceCount = 0;
        flushed = true;
    }
}