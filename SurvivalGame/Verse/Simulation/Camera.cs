using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering;

namespace Verse.Simulation;

public class Camera
{
    public Camera()
    {
        MainCamera ??= this;
        viewport = RenderingSystem.GraphicsDevice.Viewport;
    }

    public static Camera MainCamera { get; set; } = null;

    private Viewport viewport;
    private Vector2 position0 = Vector2.Zero;
    private Vector2 position = Vector2.Zero;
    private float orthoSize0 = 15f;
    private float orthoSize = 15f;

    public Vector2 Position { get {return position0; } set { position0 = value; position = value; } }
    internal Vector2 TPosition => position;
    public float PosX { get { return position0.X; } set { position0.X = value; position.X = value; } }
    public float PosY { get { return position0.Y; } set { position0.Y = value; position.Y = value; } }
    public float ScaleX { get { return ScaleY; }}
    public float ScaleY { get { return viewport.Height / orthoSize0; } }
    public float Zoom
    {
        get => viewport.Height / orthoSize0;
        set => orthoSize0 = viewport.Height / MathF.Max(value, 0.01f);
    }
    public float OrthographicSize { get { return orthoSize0; } set { orthoSize0 = value > 0.01f ? value : 0.01f; orthoSize = orthoSize0;  } }
    internal float TOrthographicSize => orthoSize;

    public virtual Matrix GetViewMatrix()
    {
        viewport = RenderingSystem.GraphicsDevice.Viewport;

        float scaleY = viewport.Height / orthoSize0;
        float scaleX = scaleY;
        // Translation to center the camera on its position
        var translation = Matrix.CreateTranslation(new Vector3(-position0.X, position0.Y, 0f));

        // Zoom
        var scale = Matrix.CreateScale(scaleX, scaleY, 1f);

        // Move origin to center of screen
        var screenCenter = Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));

        // Final view matrix: translate world -> rotate/scale -> move to screen center
        return translation * scale * screenCenter;
    }
    
    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return Vector2.Transform(screenPos, Matrix.Invert(GetViewMatrix()));
    }
    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return Vector2.Transform(worldPos, GetViewMatrix());
    }

    public void LookAt(Vector2 target)
    {
        position0 = target;
        position = target;
    }

    public void Translate(Vector2 delta)
    {
        position0 += delta;
    }

    public void MoveTo(Vector2 to)
    {
        position = to;
    }
    public void ScaleTo(float orthoSize)
    {
        this.orthoSize = Math.Max(orthoSize, 0.01f);
    }

    internal void TickStart(Viewport viewport)
    {
        this.viewport = viewport;
    }
    internal void Tick()
    {
        position0 = position;
        orthoSize0 = orthoSize;
    }
} 
