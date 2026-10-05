using System;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Luminix;

// <================================== AUDIO ENGINE - LISTENER ==================================>

internal sealed partial class AudioEngine : IAudioEngine, IDisposable
{
    private AudioListenerState[] audioListenerStates;
    
    private void InitializeAudioListener()
    {
        audioListenerStates = new AudioListenerState[1];
        audioBridge.SetStates(audioListenerStates);
    }

    private void FenceState(ref AudioListenerState state)
    {
        state.Version++;
        Thread.MemoryBarrier();
    }

    public void SetListenerPosition(float x, float y, float z)
    {
        ValidateDevice();
            
        audioDevice.ListenerSetPosition(x, y, z);

        ref var state = ref audioListenerStates[0];
        FenceState(ref state);
        state.PosX = x;
        state.PosY = y;
        state.PosZ = z;
        FenceState(ref state);
    }
    public void SetListenerVelocity(float x, float y, float z)
    {
        ValidateDevice();
            
        audioDevice.ListenerSetVelocity(x, y, z);

        ref var state = ref audioListenerStates[0];
        FenceState(ref state);
        state.VelX = x;
        state.VelY = y;
        state.VelZ = z;
        FenceState(ref state);
    }
    public void SetListenerDirection(float x, float y, float z)
    {
        ValidateDevice();
        audioDevice.ListenerSetDirection(x, y, z);
    }    
    public void SetListenerWorldUp(float x, float y, float z)
    {
        ValidateDevice();
        audioDevice.ListenerSetWorldUp(x, y, z);
    }
    
    public Vector3 GetListenerPosition()
    {
        ValidateDevice();
        ref var state = ref audioListenerStates[0];
        return new Vector3(state.PosX, state.PosY, state.PosZ);
    }
    public Vector3 GetListenerVelocity()
    {
        ValidateDevice();
        ref var state = ref audioListenerStates[0];
        return new Vector3(state.VelX, state.VelY, state.VelZ);
    }

    public bool TryGetListenerInfo(out AudioListenerInfo info)
    {
        ValidateDevice();
        info = new AudioListenerInfo(in audioListenerStates[0], false);
        return true;
    }
}