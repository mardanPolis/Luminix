using System.Threading;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework;

namespace Luminix;

internal sealed class AudioBridge
{
//     private readonly CommandBridge<AudioCommand> commands = new();
//     private readonly SoundState[][] soundStates;
 
//     public void Play(Sound handle)
//     {
        
//     }
//     public void Stop(Sound handle)
//     {
        
//     }

//     public void Resume(Sound handle)
//     {
        
//     }
//     public void Pause(Sound handle)
//     {
        
//     }

//     public void SetSpatial(Sound handle, bool spatial)
//     {
        
//     }
//     public void SetLooping(Sound handle, bool loop)
//     {
        
//     }
//     public void SetVolume(Sound handle, float volume)
//     {
        
//     }
//     public void SetPitch(Sound handle, float pitch)
//     {
        
//     }
//     public void SetPan(Sound handle, float pan)
//     {
        
//     }
//     public void SetPosition(Sound handle, Vector2 position)
//     {
        
//     }

//     public bool TryGetInfo(Sound handle, out SoundInfo info)
//     {
//         info = default;
//         if (!handle.IsValid) return false;
 
//         if(soundStates is null)
//             return false;

//         uint index = handle.Index;
//         if ((uint)index >= soundStates.Length) return false;
 
//         SoundState[] arr = Volatile.Read(ref soundStates[index]);
//         if (arr == null) return false;
 
//         for (int i = 0; i < arr.Length; i++)
//         {
//             if (ReadVoice(ref arr[i], out Sound owner, out SoundInfo read) && owner == handle)
//             {
//                 info = read;
//                 return true;
//             }
//         }
//         return false;
//     }
 
//     private static bool ReadVoice(ref SoundState soundState, out Sound owner, out SoundInfo info)
//     {
//         while (true)
//         {
//             int version = Volatile.Read(ref soundState.Version);
//             if ((version & 1) == 0)
//             {
//                 owner = soundState.Owner;
//                 int state = soundState.State;
//                 bool looped = soundState.Looped;
//                 float volume = soundState.Volume, pitch = soundState.Pitch, pan = soundState.Pan;
 
//                 Thread.MemoryBarrier();
//                 if (Volatile.Read(ref soundState.Version) == version)
//                 {
//                     info = new SoundInfo(state, looped, volume, pitch, pan);
//                     return owner.IsValid;
//                 }
//             }
//             Thread.SpinWait(1);
//         }
//     }
 
}