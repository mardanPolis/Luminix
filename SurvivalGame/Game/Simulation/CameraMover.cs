using System;
using Microsoft.Xna.Framework.Input;
using Luminix;

namespace Game.Simulation;

public class CameraMover : CameraComponent
{
    private Input input;

    public CameraMover(Camera camera) : base(camera)
    {
        Workspace.Current.TryGetService(out input);
    }

    public override void Tick()
    {
        float speed = (float)(5f * Universe.TickDeltaTime);

        var cameraPos = camera.Position;

        if(input.GetKey(Keys.W))
            cameraPos.Y += speed * (camera.OrthographicSize * 0.1f);
        if(input.GetKey(Keys.S))
            cameraPos.Y -= speed * (camera.OrthographicSize * 0.1f);

        if(input.GetKey(Keys.A))
            cameraPos.X -= speed * (camera.OrthographicSize * 0.1f);
        if(input.GetKey(Keys.D))
            cameraPos.X += speed * (camera.OrthographicSize * 0.1f);
            
        float direction = 0;
        if(input.GetKey(Keys.E)) direction += 1f;
        if(input.GetKey(Keys.Q)) direction -= 1f;

        if (direction != 0)
        {
            float targetSize = camera.OrthographicSize + (direction * speed * 10f);
            targetSize = Math.Clamp(targetSize, 5, 150);
            camera.ScaleTo(targetSize);
        }

        camera.MoveTo(cameraPos);
    }
}
