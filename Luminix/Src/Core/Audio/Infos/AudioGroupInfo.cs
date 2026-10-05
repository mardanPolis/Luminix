using System.Threading;
using Microsoft.Xna.Framework;

namespace Luminix;

public readonly ref struct AudioGroupInfo
{
    internal AudioGroupInfo(in AudioGroupState state, bool threaded) 
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

                Name = state.Name;
                Volume = state.Volume;

                Thread.MemoryBarrier();

                int v2 = state.Version;

                if (v1 == v2)
                    break;
            }

            return;
        }

        Name = state.Name;
        Volume = state.Volume;
    }
    
    public string Name { get; init; }
    public float Volume { get; init; }

}