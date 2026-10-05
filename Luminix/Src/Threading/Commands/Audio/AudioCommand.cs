namespace Luminix.Threading.Commands;

internal enum AudioCommandType { 
    SetListenerPosition, SetListenerVelocity, SetGroupVolume, 
    PlaySource, StopSource, ResumeSource, PauseSource,
    SetSourcePosition, SetSourceVelocity, SetSourceSpatial, SetSourceLooping,
    SetSourceVolume, SetSourcePitch, SetSourcePan, SetSourceRolloff,
    SetSourceMinDistance, SetSourceMaxDistance, SetSourceCursor, SetSourceAttenuationModel,
}
internal readonly record struct AudioCommand(AudioCommandType Type, int Index, float X, float Y = 0, float Z = 0, int Generation = 0);