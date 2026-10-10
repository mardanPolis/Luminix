using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Luminix.Rendering;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Buffers;

internal sealed class RenderBuffer
{
    internal enum RenderObjectType : byte
    {
        SpriteNonInterpolated,
        SpriteInterpolated,
        TextNonInterpolated,
        TextInterpolated,
    }
    internal enum UiRenderObjectType : byte
    {
        Image,
        Label,
    }
    
    public readonly record struct RenderCommand(uint Identifier);
    internal readonly record struct UiRenderCommand(uint DataIndex);
   
    public RenderBuffer()
    {
        renderInstances = new(5120);
        renderInstancesUI = new(256);
    }

    private Camera? cameraData;

    //WorldSpace
    private readonly List<RenderInstance>  renderInstances;
    
    //ScreenSpace
    private readonly List<RenderInstanceUI> renderInstancesUI;

    public RenderSettings RenderSettings;

    internal void SetCam(in Camera? camera)
    {
        cameraData = camera;
    }

    internal void PushSprite(ShaderResource shader, TextureResource texture, Color color, in Transform transform)
    {
        renderInstances.Add(new RenderInstance(shader, texture, color, new(transform), null));
    }
    internal void PushInterpolatedSprite(ShaderResource shader, TextureResource texture, Color color, in TransformInterp transform)
    {
        renderInstances.Add(new RenderInstance(shader, texture, color, transform, null));
    }
    internal void PushText(ShaderResource shader, FontResource font, Color color, string text, in Transform transform)
    {
        renderInstances.Add(new RenderInstance(shader, font, color, new(transform), text ?? string.Empty));
    }
    internal void PushInterpolatedText(ShaderResource shader, FontResource font, Color color, string text, in TransformInterp transform)
    {
        renderInstances.Add(new RenderInstance(shader, font, color, transform, text ?? string.Empty));
    }

    internal void PushImage(ShaderResource shader, TextureResource image, Color color, int x, int y, int width, int height, float rotation)
    {
        renderInstancesUI.Add(new RenderInstanceUI(shader, image, color, x, y, width, height, rotation, null));
    }
    internal void PushLabel(ShaderResource shader, FontResource font, Color color, string text, int x, int y, int width, int height, float rotation)
    {
        renderInstancesUI.Add(new RenderInstanceUI(shader, font, color, x, y, width, height, rotation, text ?? string.Empty));
    }

    internal Camera? GetCameraData()
        => cameraData;

    internal ReadOnlySpan<RenderInstance> GetRenderInstances() 
        => CollectionsMarshal.AsSpan(renderInstances);

    internal ReadOnlySpan<RenderInstanceUI> GetUIRenderInstances()
        => CollectionsMarshal.AsSpan(renderInstancesUI);

    internal void Reset()
    {
        renderInstances.Clear();
        renderInstancesUI.Clear();
    }
}

