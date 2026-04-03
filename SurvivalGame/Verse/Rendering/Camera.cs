using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Rendering;

public sealed class Camera
{
    private Viewport viewport;
    private Vector2 position = Vector2.Zero;
    private float orthoSize = 15f;

    public Vector2 Position { get {return position; } set { position = value; } }
    public float PosX { get { return position.X; } set { position.X = value; } }
    public float PosY { get { return position.Y; } set { position.Y = value; } }

    public float ScaleX { get { return ScaleY; }}
    public float ScaleY { get { return viewport.Height / orthoSize;; } }

    public float OrthographicSize { get { return orthoSize; } set { orthoSize = value > 0.01f ? value : 0.01f; } }

    public Viewport Viewport => viewport;

    public Camera(Viewport viewport)
    {
        this.viewport = viewport;
    }

    public Matrix GetViewMatrix()
    {
        float scaleY = viewport.Height / orthoSize;
        float scaleX = scaleY;
        // Translation to center the camera on its position
        var translation = Matrix.CreateTranslation(new Vector3(-position.X, position.Y, 0f));

        // Zoom
        var scale = Matrix.CreateScale(scaleX, scaleY, 1f);

        // Move origin to center of screen
        var screenCenter = Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));

        // Final view matrix: translate world -> rotate/scale -> move to screen center
        return translation * scale * screenCenter;
    }
} 
