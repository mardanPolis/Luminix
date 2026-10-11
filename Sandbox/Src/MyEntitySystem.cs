using System;
using System.Text.Json.Serialization;
using Luminix;
using Microsoft.Xna.Framework;

namespace Sandbox;

public class MyEntitySystem : EntitySystem
{
    [JsonInclude] private TextureResource texture;
    [JsonInclude] private AudioResource audio;

    private const float Speed = 0.5f;
    private const float Stride = 0.5f;
    private const float Range = 5f;

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

    private static float PathLength(float t)
    {
        float k = MathF.Floor(t / MathF.PI);
        float r = t - k * MathF.PI;
        float partial = r <= MathF.PI / 2f ? MathF.Sin(r) : 2f - MathF.Sin(r);
        return (2f * k + partial) * MathF.Sqrt(2f);
    }

    private void Update(ref TransformInterp transform, ref AudioHolder audioHolder)
    {
        float dt = 1f / Time.TickRate;
        float t = Time.TickCount * dt * Speed;
        float tPrev = t - dt * Speed;

        float s = MathF.Sin(t) * Range;
        transform.Position = new Vector3(s, s, transform.Position.Z);

        if (tPrev >= 0f &&
            MathF.Floor(PathLength(t) / Stride) > MathF.Floor(PathLength(tPrev) / Stride))
        {
            AudioMedia.PlaySource(audioHolder.AudioSource);
        }
    }

    private void Render(in TransformInterp transform)
    {
        Graphics.DrawSprite(texture, transform);
    }


    private void Start(ref AudioHolder audioHolder)
    {
        audioHolder.AudioSource = AudioMedia.CreateSource(audio);
        AudioMedia.PlaySource(audioHolder.AudioSource);
    }

    private void OnDestroy(ref AudioHolder audioHolder)
    {
        AudioMedia.DisposeSource(audioHolder.AudioSource);
    }
}
