using System;
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
            new VertexElement(24, VertexElementFormat.Color,   VertexElementUsage.TextureCoordinate, 4),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 5),
            new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 6)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
    public Renderer()
    {
        renderingSystem = Workspace.Current.GetService<RenderingSystem>();

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

    private DynamicVertexBuffer dataBuffer;
    private IndexBuffer indexBuffer;
    private VertexBuffer quadBuffer;
    private VertexBufferBinding[] bindings;

    private Effect currShader;
    private object currSource;

    private Data[] dataArray;
    private int currentInstanceCount;

    private bool flushed;

    private Microsoft.Xna.Framework.Graphics.GraphicsDevice device;

    public void Render(in Matrix transformMatrixValue, Effect shader, Texture2D source, in Rectangle sourceRect, in Color color, in Transform transform)
    {
        if (shader != currShader || source != currSource)
        {
            Flush();

            currShader = shader;
            currSource = source;

            shader.Parameters[MATRIX_PARAMETER].SetValue(transformMatrixValue);
            shader.Parameters[TEXTURE_PARAMETER].SetValue(source);
            shader.Parameters[TEXTURE_SIZE_PARAMETER].SetValue(new Vector2(source.Width, source.Height));
        }
        
        flushed = false;
        dataArray[currentInstanceCount++] = new Data
        {
            Position = transform.Position,
            Scale    = transform.Scale,
            Rotation = transform.Rotation,
            Color    = color,
            RectPos  = new Vector2(sourceRect.X, sourceRect.Y),
            RectSize = new Vector2(sourceRect.Width, sourceRect.Height)
        };

        if (currentInstanceCount >= dataArray.Length)
            Flush();
    }
    public void Render(Matrix transformMatrixValue, Effect shader, SpriteFont font, string text, Vector3 position, Vector2 scale, float rotation, Color color)
    {
        var glyphs = font.GetGlyphs();
        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);
        Vector2 pen = Vector2.Zero;

        foreach (char c in text)
        {
            if (c == '\r') continue;

            if (c == '\n')
            {
                pen.X = 0;
                pen.Y += font.LineSpacing;
                continue;
            }

            if (!glyphs.TryGetValue(c, out var g))
            {
                if (!font.DefaultCharacter.HasValue ||
                    !glyphs.TryGetValue(font.DefaultCharacter.Value, out g))
                    continue;
            }

            Rectangle src = g.BoundsInTexture;
            if (src.Width > 0 && src.Height > 0)
            {
                Vector2 local = (pen + new Vector2(g.LeftSideBearing + g.Cropping.X, g.Cropping.Y)) * scale;

                Vector2 rotated = new Vector2(
                    local.X * cos - local.Y * sin,
                    local.X * sin + local.Y * cos);

                Render(transformMatrixValue, shader, font.Texture, src, color,
                    new Transform
                    {
                        Position = new Vector3(position.X + rotated.X, position.Y + rotated.Y, position.Z),
                        Scale    = scale,
                        Rotation = rotation
                    });
            }

            pen.X += g.WidthIncludingBearings + font.Spacing;
        }
    }

    private void Flush()
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