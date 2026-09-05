using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using System.Runtime.CompilerServices;

namespace Luminix;

public sealed class Graphics : Workspace.Service
{
    internal readonly struct Controller
    {
        internal Controller(Graphics controlled, RenderBridge renderBridge)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a Graphics.Controller, the provided graphics is already initialized!");
            
            this.bridge = renderBridge;
            this.controlled = controlled;
        }

        private readonly RenderBridge bridge;
        private readonly Graphics controlled;

        internal void SwapBuffers()
        {
            this.bridge.SwapBuffers();
            var renderInfoBuffer = this.bridge.CurrentReadInfoBuffer;
            if(renderInfoBuffer == null || !renderInfoBuffer.Updated)
                return;

            var accumulatedFrameCount = renderInfoBuffer.AccumulatedFrameCount;
            controlled.FrameRate = renderInfoBuffer.FrameRate;
            controlled.FrameCount = renderInfoBuffer.FrameCount;
            controlled.FrameTime = renderInfoBuffer.FrameTime / accumulatedFrameCount;
            controlled.FrameDeltaTime = renderInfoBuffer.FrameDeltaTime / accumulatedFrameCount;
            controlled.FrameAllocated = renderInfoBuffer.Allocated;
            renderInfoBuffer.Reset();
        }

        internal void BeginRender()
        {
            controlled.renderBuffer = bridge.CurrentWriteBuffer ?? bridge.CreateBuffer();
            controlled.renderBuffer.Reset();
        }

        internal void StopRender()
        {
            controlled.renderBuffer.Resolution = controlled.Resolution;
            controlled.renderBuffer.Fullscreen = controlled.Fullscreen;
            controlled.renderBuffer = null;
        }
    }

    internal Graphics(RenderBridge renderBridge, out Controller controller)
    {
        controller = new Controller(this, renderBridge);
        this.controllerIssued = true;
    }
    
    private readonly bool controllerIssued;

    private RenderBuffer renderBuffer;

    public uint FrameRate { get; private set; }
    public ulong FrameCount { get; private set; }
    public double FrameTime { get; private set; }
    public double FrameDeltaTime { get; private set; }
    public long FrameAllocated { get; private set; }

    public int DrawCalls { get; private set; }

    public Viewport Viewport { get; private set; }

    public int RefreshRate { get; private set; }

    public int TargetFrameRate { get; set; }
    public bool VSync { get; set; }
    public bool Fullscreen { get; set; }
    public Point Resolution { get; set; }

    public void DrawSprite(TextureResource texture, in Vector3 position)                                    => DrawSprite(null, texture, Color.White, new Transform(position));
    public void DrawSprite(TextureResource texture, in Vector3 position, float rotation)                    => DrawSprite(null, texture, Color.White, new Transform(position, rotation));
    public void DrawSprite(TextureResource texture, in Vector3 position, in Vector2 scale)                  => DrawSprite(null, texture, Color.White, new Transform(position, scale));
    public void DrawSprite(TextureResource texture, in Transform transform)                                 => DrawSprite(null, texture, Color.White, transform);
    public void DrawSprite(ShaderResource shader, TextureResource texture, Color color, in Transform transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushSprite(shader, texture, color, transform);
    }

    public void DrawTSprite(TextureResource texture, in Vector3 position0, in Vector3 position)                                                                     => DrawTSprite(null, texture, Color.White, new Transform(position0), new Transform(position));
    public void DrawTSprite(TextureResource texture, in Vector3 position0, in Vector3 position, float rotation0, float rotation)                                    => DrawTSprite(null, texture, Color.White, new Transform(position0, rotation0), new Transform(position, rotation));
    public void DrawTSprite(TextureResource texture, in Vector3 position0, in Vector3 position, in Vector2 scale0, Vector2 scale)                                   => DrawTSprite(null, texture, Color.White, new Transform(position0, scale0), new Transform(position, scale));
    public void DrawTSprite(TextureResource texture, in Transform from, in Transform to)                                                                            => DrawTSprite(null, texture, Color.White, from, to);
    public void DrawTSprite(ShaderResource shader, TextureResource texture, Color color, in Transform from, in Transform to)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushInterpolatedSprite(shader, texture, color, from, to);
    }

    public void DrawText(FontResource font, string text, Vector3 position)                                  => DrawText(null, font, Color.White, text, new Transform(position));
    public void DrawText(FontResource font, string text, Vector3 position, float rotation)                  => DrawText(null, font, Color.White, text, new Transform(position, rotation));
    public void DrawText(FontResource font, string text, Vector3 position, Vector2 scale)                   => DrawText(null, font, Color.White, text, new Transform(position, scale));
    public void DrawText(FontResource font, string text, in Transform transform)                            => DrawText(null, font, Color.White, text, transform);
    public void DrawText(ShaderResource shader, FontResource font, Color color, string text, in Transform transform)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushText(shader, font, color, text, transform);
    }

    public void DrawTText(FontResource font, string text, Vector3 position0, Vector3 position)                                                                  => DrawTText(null, font, Color.White, text, new Transform(position0), new Transform(position));
    public void DrawTText(FontResource font, string text, Vector3 position0, Vector3 position, float rotation0, float rotation)                                 => DrawTText(null, font, Color.White, text, new Transform(position0, rotation0), new Transform(position, rotation));
    public void DrawTText(FontResource font, string text, Vector3 position0, Vector3 position, Vector2 scale0, Vector2 scale)                                   => DrawTText(null, font, Color.White, text, new Transform(position0, scale0), new Transform(position, scale));
    public void DrawTText(FontResource font, string text, in Transform from, in Transform to)                                                                   => DrawTText(null, font, Color.White, text, from, to);
    public void DrawTText(ShaderResource shader, FontResource font, Color color, string text, in Transform from, in Transform to)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushInterpolatedText(shader, font, color, text, from, to);
    }

    public void DrawImage(TextureResource image, int x, int y, int width, int height)                   => DrawImage(null, image, Color.White, x, y, width, height, 0);
    public void DrawImage(TextureResource image, int x, int y, int width, int height, float rotation)   => DrawImage(null, image, Color.White, x, y, width, height, rotation);
    public void DrawImage(ShaderResource shader, TextureResource image, Color color, int x, int y, int width, int height, float rotation)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushImage(shader, image, color, x, y, width, height, rotation);
    }

    public void DrawLabel(FontResource font, string text, int x, int y)                                         => DrawLabel(null, font, Color.White, text, x, y, 0, 0, 0);
    public void DrawLabel(FontResource font, string text, int x, int y, int width, int height)                  => DrawLabel(null, font, Color.White, text, x, y, width, height, 0);
    public void DrawLabel(FontResource font, string text, int x, int y, int width, int height, float rotation)  => DrawLabel(null, font, Color.White, text, x, y, width, height, rotation);
    public void DrawLabel(ShaderResource shader, FontResource font, Color color, string text, int x, int y, int width, int height, float rotation)
    {
        if (!EnsureRenderBuffer()) return;
        renderBuffer.PushLabel(shader, font, color, text, x, y, width, height, rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EnsureRenderBuffer([CallerMemberName] string caller = "")
    {
        if (renderBuffer == null)
            throw new InvalidOperationException($"{caller} must be called during rendering.");
        return true;
    }
}