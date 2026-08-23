using System;
using Microsoft.Xna.Framework;
using Luminix.Collections;

namespace Luminix;

public class Camera
{
    public record struct Data(Vector2 Position0, Vector2 Position, float OrthographicSize0, float OrthographicSize);
    public Camera()
    {
        universe = Workspace.Current.GetService<Universe>();
        graphics = Workspace.Current.GetService<Graphics>();
        //TODO : complete Constructor Method
    }

    protected readonly Universe universe;
    protected readonly Graphics graphics;

    private Vector2 position0 = Vector2.Zero;
    private Vector2 position = Vector2.Zero;
    private float orthoSize0 = 15f;
    private float orthoSize = 15f;

    private bool isDestroyed;

    public Vector2 Position { get {return position0; } set { position0 = value; position = value; } }
    internal Vector2 TPosition => position;
    public float PosX { get { return position0.X; } set { position0.X = value; position.X = value; } }
    public float PosY { get { return position0.Y; } set { position0.Y = value; position.Y = value; } }
    public float ScaleX { get { return ScaleY; }}
    public float ScaleY { get { return graphics.Viewport.Height / orthoSize0; } }
    public float Zoom
    {
        get => graphics.Viewport.Height / orthoSize0;
        set => orthoSize0 = graphics.Viewport.Height / MathF.Max(value, 0.01f);
    }
    public float OrthographicSize { get { return orthoSize0; } set { orthoSize0 = value > 0.01f ? value : 0.01f; orthoSize = orthoSize0;  } }
    internal float TOrthographicSize => orthoSize;

    public bool IsDestroyed => isDestroyed;

    public Data GetCameraSnapshot()
    {
        return new Data(position0, position, orthoSize0, orthoSize);
    }

    public virtual Matrix GetViewMatrix()
    {
        var viewport = graphics.Viewport;

        float scaleY = viewport.Height / orthoSize0;
        float scaleX = scaleY;

        var translation = Matrix.CreateTranslation(new Vector3(-position0.X, position0.Y, 0f));
        var scale = Matrix.CreateScale(scaleX, -scaleY, 1f);
        var screenCenter = Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));

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

    public void Translate(Vector2 delta)
    {
        position += delta;
    }

    public void MoveTo(Vector2 to)
    {
        position = to;
    }
    public void ScaleTo(float orthoSize)
    {
        this.orthoSize = Math.Max(orthoSize, 0.01f);
    }

    public void Destroy()
    {
        isDestroyed = true;

        //TODO : complete Destroy Method
    }
} 
