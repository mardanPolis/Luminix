namespace Luminix.Threading.Commands;

internal struct AudioCommand
{
    public enum Type : byte { Play, Stop, Pause, Resume, SetSpatial, SetLoop, SetVolume, SetPitch, SetPan, SetPosition }
    public Type Kind;
    public float Value1, Value2;
}