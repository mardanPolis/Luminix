using System;

namespace Luminix.Threading;

internal sealed class RenderThread : Thread
{
    private Action start;
    private Action stop;

    public RenderThread(MainRender main)
    {
        Priority = System.Threading.ThreadPriority.AboveNormal;
        Name = "RenderThread";

        //TODO create Actions
    }

    public override void Start() => start();
    public override void Stop() => stop();
}