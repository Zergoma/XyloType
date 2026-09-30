using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using XyloType.Application.Interfaces;

namespace XyloType.Services;

/// <summary>
/// Plays short feedback sounds through a single, always-running WASAPI output and a mixer.
/// The output stream never stops (the mixer outputs silence when idle), so the audio device
/// stays awake: sounds start immediately, are never clipped at the start, and can overlap.
/// Must be registered as a singleton.
/// </summary>
public sealed class MauiPlaySoundSample : IPlaySoundSample, IDisposable
{
    private const int MixerSampleRate = 44100;
    private const int MixerChannels = 2;
    private const int OutputLatencyMs = 40;

    private readonly ILogger<MauiPlaySoundSample> _logger;
    private readonly ConcurrentDictionary<string, Lazy<Task<float[]>>> _sounds = new();
    private readonly Lazy<MixingSampleProvider?> _mixer;
    private WasapiOut? _output;

    public MauiPlaySoundSample(ILogger<MauiPlaySoundSample> logger)
    {
        _logger = logger;
        _mixer = new Lazy<MixingSampleProvider?>(StartOutput);
    }

    public async Task PreloadAsync(params string[] sounds)
    {
        _ = _mixer.Value;
        await Task.WhenAll(sounds.Select(GetSoundAsync));
    }

    public void PlaySound(string sound, double volume)
    {
        if (volume <= 0 || _mixer.Value is not MixingSampleProvider mixer)
            return;

        Task<float[]> samples = GetSoundAsync(sound);
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

            _output = new WasapiOut(AudioClientShareMode.Shared, useEventSync: true, OutputLatencyMs);
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

    private Task<float[]> GetSoundAsync(string sound)
    {
        Lazy<Task<float[]>> entry = _sounds.GetOrAdd(sound, s => new Lazy<Task<float[]>>(() => LoadAsync(s)));

        // Allow a later retry if loading failed
        if (entry.Value.IsFaulted)
            _sounds.TryRemove(new KeyValuePair<string, Lazy<Task<float[]>>>(sound, entry));

        return entry.Value;
    }

    private async Task<float[]> LoadAsync(string sound)
    {
        try
        {
            using MemoryStream buffer = new();
            await using (Stream file = await FileSystem.OpenAppPackageFileAsync(sound))
            {
                await file.CopyToAsync(buffer);
            }
            buffer.Position = 0;

            using WaveFileReader reader = new(buffer);
            return ToMixerFormat(reader.ToSampleProvider());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load sound {Sound}", sound);
            throw;
        }
    }

    /// <summary>
    /// Decodes the whole sound once, converted to the mixer sample rate and channel count.
    /// </summary>
    private static float[] ToMixerFormat(ISampleProvider source)
    {
        if (source.WaveFormat.SampleRate != MixerSampleRate)
            source = new WdlResamplingSampleProvider(source, MixerSampleRate);

        if (source.WaveFormat.Channels == 1)
            source = new MonoToStereoSampleProvider(source);

        List<float> samples = [];
        float[] chunk = new float[MixerSampleRate * MixerChannels];
        int read;
        while ((read = source.Read(chunk)) > 0)
            samples.AddRange(chunk.AsSpan(0, read));

        return [.. samples];
    }

    /// <summary>
    /// Reads a decoded sound once, then ends (the mixer removes it automatically).
    /// </summary>
    private sealed class CachedSoundSampleProvider(float[] samples, float volume, WaveFormat format) : ISampleProvider
    {
        private int _position;

        public WaveFormat WaveFormat { get; } = format;

        public int Read(Span<float> buffer)
        {
            int count = Math.Min(buffer.Length, samples.Length - _position);
            ReadOnlySpan<float> source = samples.AsSpan(_position, count);

            for (int i = 0; i < count; i++)
                buffer[i] = source[i] * volume;

            _position += count;
            return count;
        }
    }
}
