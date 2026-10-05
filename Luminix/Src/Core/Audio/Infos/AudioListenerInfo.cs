using System.Threading;

namespace Luminix;

public readonly ref struct AudioListenerInfo
{
    internal AudioListenerInfo(in AudioListenerState state, bool threaded) 
    { 
        if(threaded)
        {
            while (true)
            {
                int v1 = state.Version;

                if ((v1 & 1) != 0) 
                {
                    Thread.SpinWait(1);
                    continue;
                }

                PosX = state.PosX;
                PosY = state.PosY;
                PosZ = state.PosZ;
                
                VelX = state.VelX;
                VelY = state.VelY;
                VelZ = state.VelZ;

                Thread.MemoryBarrier();

                int v2 = state.Version;

                if (v1 == v2)
                    break;
            }

            return;
        }

        PosX = state.PosX;
        PosY = state.PosY;
        PosZ = state.PosZ;
        
        VelX = state.VelX;
        VelY = state.VelY;
        VelZ = state.VelZ;
    }
    
    public float PosX { get; }
    public float PosY { get; }
    public float PosZ { get; }
    
    public float VelX { get; }
    public float VelY { get; }
    public float VelZ { get; }
}