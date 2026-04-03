using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Verse.Rendering;

public sealed class Camera
{
    private Viewport viewport;
    private Vector2 position = Vector2.Zero;
    private float zoom = 2f;

    public Vector2 Position { get {return position; } set { position = value; } }
    public float X { get { return position.X; } set { position.X = value; } }
    public float Y { get { return position.Y; } set { position.Y = value; } }
    public float Zoom { get { return zoom; } set { zoom = value > 0.01f ? value : 0.01f; } }

    public Viewport Viewport => viewport;

    public Camera(Viewport viewport)
    {
        this.viewport = viewport;
    }

    public Matrix GetViewMatrix()
    {
        // Translation to center the camera on its position
        var translation = Matrix.CreateTranslation(new Vector3(-position.X, position.Y, 0f));

        // Zoom
        var scale = Matrix.CreateScale(Zoom, Zoom, 1f);

        // Move origin to center of screen
        var screenCenter = Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));

        // Final view matrix: translate world -> rotate/scale -> move to screen center
        return translation * scale * screenCenter;
    }
} 
