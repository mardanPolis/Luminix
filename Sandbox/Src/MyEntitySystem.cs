using System;
using System.Text.Json.Serialization;
using Luminix;
using Microsoft.Xna.Framework;

namespace Sandbox;

public class MyEntitySystem : EntitySystem
{
    [JsonInclude] private TextureResource texture;
    [JsonInclude] private AudioResource audio;

    [JsonInclude] private float areaSize = 12f;
    [JsonInclude] private float speed = 1.5f;

    private readonly Random random = new();

    private Vector3 RandomPointInArea(Vector3 center)
    {
        float half = areaSize / 2f;
        float x = center.X + (float)(random.NextDouble() * (areaSize * 2) - half * 2);
        float y = center.Y + (float)(random.NextDouble() * areaSize - half);

        return new Vector3(x, y, -y); // Z = Y
    }

    private void Start(ref TransformInterp transform)
    {
        transform.Scale0 = new Vector2(1, 1);
        transform.Scale = new Vector2(1, 1);
    }

    private void Prepare(ref TransformInterp transform)
    {
        transform.Position0 = transform.Position;
        transform.Scale0 = transform.Scale;
        transform.Rotation0 = transform.Rotation;
    }

    private void Update(ref TargetPosition targetPosition, ref TransformInterp transform)
    {
        float step = speed * (float)Time.TickDeltaTime;

        Vector3 toTarget = targetPosition.position - transform.Position;
        float distance = toTarget.Length();

        if (distance <= step)
        {
            transform.Position = targetPosition.position;
            targetPosition.position = RandomPointInArea(Vector3.Zero);
        }
        else
        {
            transform.Position += toTarget / distance * step;
        }
        
        transform.Position.Z = -transform.Position.Y;
    }

    private void Render(in TransformInterp transform)
    {
        Graphics.DrawSprite(texture, transform);
    }
}
