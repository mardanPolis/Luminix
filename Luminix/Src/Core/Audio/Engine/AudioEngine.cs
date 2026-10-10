using System;
using System.Runtime.CompilerServices;
using Luminix.Exceptions;
using Luminix.Native.Audio;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework;

namespace Luminix;

// <================================== AUDIO ENGINE - CORE ==================================>

internal sealed partial class AudioEngine : IAudioEngine, IDisposable
{

    // GetMethods dont check the fence since they are called on the same thread, the fencing is only used on AudioBridge            
    private static (AudioClip.SampleFormat, AudioClip.Channels, int) ExtractData(AudioClip.Format audioFormat)
    {
        return audioFormat switch
        {
            AudioClip.Format.Mono8     => (AudioClip.SampleFormat.U8, AudioClip.Channels.Mono, 1),
            AudioClip.Format.Stereo8   => (AudioClip.SampleFormat.U8, AudioClip.Channels.Stereo, 1),
            AudioClip.Format.Mono16    => (AudioClip.SampleFormat.S16, AudioClip.Channels.Mono, 2),
            AudioClip.Format.Stereo16  => (AudioClip.SampleFormat.S16, AudioClip.Channels.Stereo, 2),
            AudioClip.Format.Mono24    => (AudioClip.SampleFormat.S24, AudioClip.Channels.Mono, 3),
            AudioClip.Format.Stereo24  => (AudioClip.SampleFormat.S24, AudioClip.Channels.Stereo, 3),
            AudioClip.Format.MonoS32   => (AudioClip.SampleFormat.S32, AudioClip.Channels.Mono, 4),
            AudioClip.Format.StereoS32 => (AudioClip.SampleFormat.S32, AudioClip.Channels.Stereo, 4),
            AudioClip.Format.MonoF32   => (AudioClip.SampleFormat.F32, AudioClip.Channels.Mono, 4),
            AudioClip.Format.StereoF32 => (AudioClip.SampleFormat.F32, AudioClip.Channels.Stereo, 4),

            _ => throw new NotSupportedException($"Unsupported format: {audioFormat}")
        };
    }
    private static void ThrowIfFailed(AudioDeviceResult result, [CallerMemberName] string operation = "")
    {
        if (result != AudioDeviceResult.Success)
            throw new AudioDeviceException(operation, result);
    }

    public AudioEngine()
    {
        audioDevice = AudioDevice.Create();
        audioDevice.Start();

        audioBridge = ThreadBridges.GetGlobalBridge<AudioBridge>();

        InitializeAudioListener();
        InitializeAudioSources();
        InitializeAudioClips();
        InitializeAudioGroups();

        handleCommands = (command) =>
        {
            try
            {
                switch(command.Type)
                {

                    // <================================== LISTENER ==================================>

                    case AudioCommandType.SetListenerPosition:
                        SetListenerPosition(command.X, command.Y, command.Z);
                    break;
                    case AudioCommandType.SetListenerVelocity:
                        SetListenerVelocity(command.X, command.Y, command.Z);
                    break;

                    // <================================== AUDIO GROUP ==================================>

                    case AudioCommandType.SetGroupVolume:
                        SetGroupVolume(new AudioGroup(command.Index), command.X);
                    break;

                    // <================================== AUDIO GROUP ==================================>

                    case AudioCommandType.PlaySource:
                        PlaySource(new AudioSource(command.Index, command.Generation));
                    break;
                    case AudioCommandType.StopSource:
                        StopSource(new AudioSource(command.Index, command.Generation));
                    break;
                    case AudioCommandType.ResumeSource:
                        ResumeSource(new AudioSource(command.Index, command.Generation));
                    break;
                    case AudioCommandType.PauseSource:
                        PauseSource(new AudioSource(command.Index, command.Generation));
                    break;
                    case AudioCommandType.SetSourcePosition:
                        SetSourcePosition(new AudioSource(command.Index, command.Generation), new Vector3(command.X, command.Y, command.Z));
                    break;
                    case AudioCommandType.SetSourceVelocity:
                        SetSourceVelocity(new AudioSource(command.Index, command.Generation), new Vector3(command.X, command.Y, command.Z));
                    break;
                    case AudioCommandType.SetSourceSpatial:
                        SetSourceSpatial(new AudioSource(command.Index, command.Generation), command.X == 1);
                    break;
                    case AudioCommandType.SetSourceLooping:
                        SetSourceLooping(new AudioSource(command.Index, command.Generation), command.X == 1);
                    break;
                    case AudioCommandType.SetSourceVolume:
                        SetSourceVolume(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourcePitch:
                        SetSourcePitch(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourcePan:
                        SetSourcePan(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourceRolloff:
                        SetSourceRolloff(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourceMinDistance:
                        SetSourceMinDistance(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourceMaxDistance:
                        SetSourceMaxDistance(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourceCursor:
                        SetSourceCursor(new AudioSource(command.Index, command.Generation), command.X);
                    break;
                    case AudioCommandType.SetSourceAttenuationModel:
                        SetSourceAttenuationModel(new AudioSource(command.Index, command.Generation), (int)command.X);
                    break;
                    default:
                        Logger.NewLog(Logger.Log.Level.Warning, $"Unknown audio command '{command.Type}'.");
                        break;
                }
            }
            catch (InvalidAudioSourceException ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to handle '{command.Type}' audio command, because {ex}");
            }
            catch (InvalidAudioGroupException ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to handle '{command.Type}' audio command, because {ex}");
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to handle '{command.Type}' audio command, reason: {ex}");
            }
        };
    }
    ~AudioEngine()
    {
        audioDevice.Stop();
        audioDevice.Dispose();
    }

    private AudioDevice audioDevice;
    private AudioBridge audioBridge;

    private Action<AudioCommand> handleCommands;

    private const int InitialCapacity = 8;

    public void HandleCommands()
    {
        audioBridge.Commands.ConsumeCommands(handleCommands);
    }

    private void ValidateDevice()
    {
        if (audioDevice.IsInvalid)
            throw new InvalidOperationException("The audio device is invalid, has not been initialized or is disposed.");
    }
    public void Dispose()
    {
        DisposeAllSources();
        DisposeAllGroups();
        DisposeAllAudioClips();
        audioDevice.Dispose();
    }
}