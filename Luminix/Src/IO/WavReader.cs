using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Luminix.IO;

internal static class WavReader
{
    public static AudioData Read(Stream stream)
    {
        using var reader = new BinaryReader(
            stream,
            Encoding.UTF8,
            leaveOpen: true);

        if (ReadFourCC(reader) != "RIFF")
            throw new InvalidDataException("Invalid WAV: missing RIFF header.");

        reader.ReadUInt32(); // RIFF chunk size

        if (ReadFourCC(reader) != "WAVE")
            throw new InvalidDataException("Invalid WAV: missing WAVE identifier.");

        ushort audioFormat = 0;
        ushort channels = 0;
        uint sampleRate = 0;
        ushort bitsPerSample = 0;

        byte[]? pcmData = null;

        while (stream.Position < stream.Length)
        {
            string chunkId = ReadFourCC(reader);
            uint chunkSize = reader.ReadUInt32();

            long chunkEnd = stream.Position + chunkSize;

            switch (chunkId)
            {
                case "fmt ":
                {
                    audioFormat = reader.ReadUInt16();
                    channels = reader.ReadUInt16();
                    sampleRate = reader.ReadUInt32();

                    reader.ReadUInt32(); // byte rate
                    reader.ReadUInt16(); // block align
                    bitsPerSample = reader.ReadUInt16();

                    stream.Position = chunkEnd;

                    break;
                }

                case "data":
                {
                    if (chunkSize > int.MaxValue)
                        throw new InvalidDataException(
                            "WAV data chunk is too large.");

                    pcmData = reader.ReadBytes((int)chunkSize);

                    if (pcmData.Length != chunkSize)
                        throw new EndOfStreamException(
                            "Unexpected end of WAV data.");

                    break;
                }

                default:
                    stream.Position = chunkEnd;
                    break;
            }

            if ((chunkSize & 1) != 0)
                stream.Position++;
        }

        if (pcmData == null)
            throw new InvalidDataException(
                "WAV file does not contain a data chunk.");

        if (audioFormat == 0 || channels == 0 || sampleRate == 0)
            throw new InvalidDataException(
                "WAV file does not contain a valid fmt chunk.");

        AudioClip.Format format = GetBufferFormat(audioFormat, channels, bitsPerSample);

        if (format == AudioClip.Format.Unknown)
            throw new NotSupportedException(
                $"Unsupported WAV format: " +
                $"AudioFormat={audioFormat}, " +
                $"Channels={channels}, " +
                $"Bits={bitsPerSample}");

        return new AudioData
        {
            PcmData = pcmData,
            Format = format,
            SampleRate = (AudioClip.SampleRate)sampleRate
        };
    }
    public static async Task<AudioData> ReadAsync(Stream stream)
    {
        byte[] buffer = new byte[8];

        // RIFF
        await ReadExactlyAsync(stream, buffer, 0, 4);
        if (Encoding.ASCII.GetString(buffer, 0, 4) != "RIFF")
            throw new InvalidDataException("Invalid WAV: missing RIFF header.");

        await ReadExactlyAsync(stream, buffer, 0, 4);
        uint riffSize = BitConverter.ToUInt32(buffer, 0);

        // WAVE
        await ReadExactlyAsync(stream, buffer, 0, 4);
        if (Encoding.ASCII.GetString(buffer, 0, 4) != "WAVE")
            throw new InvalidDataException("Invalid WAV: missing WAVE identifier.");

        ushort audioFormat = 0;
        ushort channels = 0;
        uint sampleRate = 0;
        ushort bitsPerSample = 0;

        byte[]? pcmData = null;

        while (stream.Position < stream.Length)
        {
            // Chunk ID
            await ReadExactlyAsync(stream, buffer, 0, 4);
            string chunkId = Encoding.ASCII.GetString(buffer, 0, 4);

            // Chunk size
            await ReadExactlyAsync(stream, buffer, 0, 4);
            uint chunkSize = BitConverter.ToUInt32(buffer, 0);

            long chunkEnd = stream.Position + chunkSize;

            switch (chunkId)
            {
                case "fmt ":
                {
                    if (chunkSize < 16)
                        throw new InvalidDataException(
                            "Invalid WAV fmt chunk.");

                    byte[] fmt = new byte[16];

                    await ReadExactlyAsync(
                        stream,
                        fmt,
                        0,
                        fmt.Length);

                    audioFormat = BitConverter.ToUInt16(fmt, 0);
                    channels = BitConverter.ToUInt16(fmt, 2);
                    sampleRate = BitConverter.ToUInt32(fmt, 4);
                    bitsPerSample = BitConverter.ToUInt16(fmt, 14);

                    stream.Position = chunkEnd;

                    break;
                }

                case "data":
                {
                    if (chunkSize > int.MaxValue)
                        throw new InvalidDataException(
                            "WAV data chunk is too large.");

                    pcmData = new byte[chunkSize];

                    await ReadExactlyAsync(
                        stream,
                        pcmData,
                        0,
                        pcmData.Length);

                    break;
                }

                default:
                {
                    stream.Position = chunkEnd;
                    break;
                }
            }

            if ((chunkSize & 1) != 0)
                stream.Position++;
        }

        if (pcmData == null)
            throw new InvalidDataException(
                "WAV file does not contain a data chunk.");

        if (audioFormat == 0 ||
            channels == 0 ||
            sampleRate == 0)
        {
            throw new InvalidDataException(
                "WAV file does not contain a valid fmt chunk.");
        }

        AudioClip.Format format =
            GetBufferFormat(
                audioFormat,
                channels,
                bitsPerSample);

        if (format == AudioClip.Format.Unknown)
        {
            throw new NotSupportedException(
                $"Unsupported WAV format: " +
                $"AudioFormat={audioFormat}, " +
                $"Channels={channels}, " +
                $"BitsPerSample={bitsPerSample}");
        }

        return new AudioData
        {
            PcmData = pcmData,
            Format = format,
            SampleRate = (AudioClip.SampleRate)sampleRate
        };
    }

    private static string ReadFourCC(BinaryReader reader)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(4));
    }
    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);

            if (read == 0)
                throw new EndOfStreamException(
                    "Unexpected end of WAV file.");

            totalRead += read;
        }
    }

    private static AudioClip.Format GetBufferFormat(ushort audioFormat, ushort channels, ushort bitsPerSample)
    {
        return (audioFormat, channels, bitsPerSample) switch
        {
            // PCM integer
            (1, 1, 8)  => AudioClip.Format.Mono8,
            (1, 2, 8)  => AudioClip.Format.Stereo8,

            (1, 1, 16) => AudioClip.Format.Mono16,
            (1, 2, 16) => AudioClip.Format.Stereo16,

            (1, 1, 24) => AudioClip.Format.Mono24,
            (1, 2, 24) => AudioClip.Format.Stereo24,

            (1, 1, 32) => AudioClip.Format.MonoS32,
            (1, 2, 32) => AudioClip.Format.StereoS32,

            // IEEE float
            (3, 1, 32) => AudioClip.Format.MonoF32,
            (3, 2, 32) => AudioClip.Format.StereoF32,

            _ => AudioClip.Format.Unknown
        };
    }
    private static AudioClip.SampleRate GetSampleRate(uint sampleRate)
    {
        return Enum.IsDefined(typeof(AudioClip.SampleRate), sampleRate)
            ? (AudioClip.SampleRate)sampleRate
            : AudioClip.SampleRate.Invalid;
    }
}