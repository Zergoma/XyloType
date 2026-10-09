using System.Collections.Concurrent;
using System.Runtime.Versioning;

using Microsoft.Extensions.Logging;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using XyloType.Application.Interfaces;

namespace XyloType.Infrastructure.Audio;

/// <summary>
/// Plays short feedback sounds through a single, always-running WASAPI output and a mixer.
/// The output stream never stops (the mixer outputs silence when idle), so the audio device
/// stays awake: sounds start immediately, are never clipped at the start, and can overlap.
/// The sound files are read from the assets of the app (<see cref="IAssetReader"/>).
/// Must be registered as a singleton.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NAudioSoundPlayer : IPlaySoundSample, IDisposable
{
    private const int MixerSampleRate = 44100;
    private const int MixerChannels = 2;
    private const int OutputLatencyMs = 40;

    private readonly ILogger<NAudioSoundPlayer> _logger;
    private readonly IAssetReader _assets;
    private readonly ConcurrentDictionary<string, Lazy<Task<short[]>>> _sounds = new();
    private readonly Lazy<MixingSampleProvider?> _mixer;
    private WasapiPlayer? _output;

    public NAudioSoundPlayer(IAssetReader assets, ILogger<NAudioSoundPlayer> logger)
    {
        _assets = assets;
        _logger = logger;
        _mixer = new Lazy<MixingSampleProvider?>(StartOutput);
    }

    public async Task PreloadAsync(params string[] sounds)
    {
        // opening the audio device takes about half a second: never on the UI thread
        await Task.Run(() => _mixer.Value);
        await Task.WhenAll(sounds.Select(GetSoundAsync));
    }

    public void PlaySound(string sound, double volume)
    {
        if (volume <= 0 || _mixer.Value is not MixingSampleProvider mixer)
            return;

        Task<short[]> samples = GetSoundAsync(sound);
        if (!samples.IsCompletedSuccessfully)
            return;

        mixer.AddMixerInput(new CachedSoundSampleProvider(samples.Result, (float)volume, mixer.WaveFormat));
    }

    public void Dispose()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
    }

    private MixingSampleProvider? StartOutput()
    {
        try
        {
            MixingSampleProvider mixer = new(WaveFormat.CreateIeeeFloatWaveFormat(MixerSampleRate, MixerChannels))
            {
                // Keep producing silence when no sound plays, so the stream never stops
                ReadFully = true
            };

            _output = new WasapiPlayerBuilder()
                .WithSharedMode()
                .WithEventSync()
                .WithLatency(OutputLatencyMs)
                .Build();
            _output.Init(mixer);
            _output.Play();
            return mixer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to start the audio output, typing sounds are disabled");
            _output?.Dispose();
            _output = null;
            return null;
        }
    }

    private Task<short[]> GetSoundAsync(string sound)
    {
        Lazy<Task<short[]>> entry = _sounds.GetOrAdd(sound, s => new Lazy<Task<short[]>>(() => LoadAsync(s)));

        // Allow a later retry if loading failed
        if (entry.Value.IsFaulted)
            _sounds.TryRemove(new KeyValuePair<string, Lazy<Task<short[]>>>(sound, entry));

        return entry.Value;
    }

    private async Task<short[]> LoadAsync(string sound)
    {
        try
        {
            using MemoryStream buffer = new();
            await using (Stream file = await _assets.OpenAsync(sound))
            {
                await file.CopyToAsync(buffer);
            }
            buffer.Position = 0;

            // decoding and resampling take a while: not on the UI thread
            return await Task.Run(() =>
            {
                using WaveFileReader reader = new(buffer);
                return ToMixerFormat(reader.ToSampleProvider());
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load sound {Sound}", sound);
            throw;
        }
    }

    /// <summary>
    /// Decodes the whole sound once, at the mixer sample rate, kept in mono 16 bits:
    /// 4 times less memory than stereo floats (every note of every instrument may end up cached).
    /// </summary>
    private static short[] ToMixerFormat(ISampleProvider source)
    {
        if (source.WaveFormat.SampleRate != MixerSampleRate)
            source = new WdlResamplingSampleProvider(source, MixerSampleRate);

        if (source.WaveFormat.Channels == 2)
            source = new StereoToMonoSampleProvider(source);

        List<short> samples = [];
        float[] chunk = new float[MixerSampleRate];
        int read;
        while ((read = source.Read(chunk)) > 0)
        {
            foreach (float sample in chunk.AsSpan(0, read))
                samples.Add((short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue));
        }

        return [.. samples];
    }

    /// <summary>
    /// Reads a decoded sound once, then ends (the mixer removes it automatically):
    /// the mono 16 bits samples are played on both channels, at the volume asked.
    /// </summary>
    private sealed class CachedSoundSampleProvider(short[] samples, float volume, WaveFormat format) : ISampleProvider
    {
        private const float Scale = 1f / short.MaxValue;

        private int _position;

        public WaveFormat WaveFormat { get; } = format;

        public int Read(Span<float> buffer)
        {
            int frames = Math.Min(buffer.Length / MixerChannels, samples.Length - _position);
            float gain = volume * Scale;

            for (int i = 0; i < frames; i++)
            {
                float value = samples[_position + i] * gain;
                buffer[2 * i] = value;
                buffer[2 * i + 1] = value;
            }

            _position += frames;
            return frames * MixerChannels;
        }
    }
}
