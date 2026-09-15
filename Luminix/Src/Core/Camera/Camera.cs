using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;

public struct Camera()
{
    private Vector2 position0 { get; set; } = Vector2.Zero;
    private Vector2 position { get; set; } = Vector2.Zero;
    private float orthoSize0 { get; set; } = 15f;
    private float orthoSize { get; set; } = 15f;

    public static void SetPosition(ref Camera camera, Vector2 position)
    {
        camera.position0 = position;
        camera.position = position;
    }
    public static Vector2 GetPosition(in Camera camera)
    {
        return camera.position0;
    }
    public static Vector2 GetTargetPosition(in Camera camera)
    {
        return camera.position;
    }

    public static void SetOrthographicSize(ref Camera camera, float size)
    {
        camera.orthoSize0 = size > 0.01f ? size : 0.01f;
        camera.orthoSize = camera.orthoSize0;
    }
    public static float GetOrthographicSize(in Camera camera)
    {
        return camera.orthoSize0;
    } 
    public static float GetTargetOrthographicSize(in Camera camera)
    {
        return camera.orthoSize;
    }

    public static void Translate(ref Camera camera, Vector2 delta)
    {
        camera.position += delta;
    }
    public static void MoveTo(ref Camera camera, Vector2 to)
    {
        camera.position = to;
    }
    public static void ScaleTo(ref Camera camera, float orthoSize)
    {
        camera.orthoSize = Math.Max(orthoSize, 0.01f);
    }

    public static Matrix GetViewMatrix(in Camera camera, Viewport viewport)
    {
        float scaleY = viewport.Height / camera.orthoSize0;
        float scaleX = scaleY;

        var translation = Matrix.CreateTranslation(new Vector3(-camera.position0.X, camera.position0.Y, 0f));
        var scale = Matrix.CreateScale(scaleX, -scaleY, 1f);
        var screenCenter = Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0f));

        return translation * scale * screenCenter;
    }
}