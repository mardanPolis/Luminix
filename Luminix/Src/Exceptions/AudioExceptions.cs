using System;
using Luminix.Native.Audio;


namespace Luminix.Exceptions;

public class AudioException : Exception
{
    public AudioException(string message) : base(message) { }
    public AudioException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class UnavailableAudioEngine : AudioException
{
    public string Operation { get; }
 
    internal UnavailableAudioEngine(string operation, Exception innerException = null)
        : base($"Cannot perform '{operation}', reason: AudioEngine is not available", innerException)
    {
        Operation = operation;
    }
}

public sealed class AudioDeviceException : AudioException
{
    internal AudioDeviceResult Result { get; }
    public string Operation { get; }
 
    internal AudioDeviceException(string operation, AudioDeviceResult result, Exception innerException = null)
        : base($"Audio device failed during '{operation}', result: {result}.", innerException)
    {
        Operation = operation;
        Result = result;
    }
}


public sealed class InvalidAudioSourceException : ArgumentException
{
    public AudioSource AudioSource { get; }
    public string Operation { get; }
 
    internal InvalidAudioSourceException(AudioSource source, string operation, string paramName)
        : base($"Cannot perform '{operation}', reason: audio source with index {source.Index} is invalid or has been disposed.", paramName)
    {
        AudioSource = source;
        Operation = operation;
    }
}

public sealed class InvalidAudioGroupException : ArgumentException
{
    public AudioGroup AudioGroup { get; }
    public string Operation { get; }
 
    internal InvalidAudioGroupException(AudioGroup group, string operation, string paramName)
        : base($"Cannot perform '{operation}', reason: audio group with index {group.Index} is invalid or has been disposed.", paramName)
    {
        AudioGroup = group;
        Operation = operation;
    }
}
