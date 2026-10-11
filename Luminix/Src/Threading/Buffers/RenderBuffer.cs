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

    internal void PushSprite(ShaderID shader, TextureID texture, Rectangle sourceRect, Color color, in Transform transform)
    {
        renderInstances.Add(new RenderInstance(shader, texture, sourceRect, color, new(transform)));
    }
    internal void PushInterpolatedSprite(ShaderID shader, TextureID texture, Rectangle sourceRect, Color color, in TransformInterp transform)
    {
        renderInstances.Add(new RenderInstance(shader, texture, sourceRect, color, transform));
    }
    internal void PushImage(ShaderID shader, TextureID image, Rectangle sourceRect, Color color, Point position, Point size, float rotation)
    {
        renderInstancesUI.Add(new RenderInstanceUI(shader, image, sourceRect, color, position, size, rotation));
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

