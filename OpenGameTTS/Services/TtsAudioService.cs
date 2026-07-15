using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using NAudio.Wave;

namespace OpenGameTTS.Services;

public sealed class TtsAudioService : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer = new() { Rate = 0, Volume = 100 };
    private WaveOutEvent? _waveOut;
    private WaveOutEvent? _waveOutVBCable;

    public int Volume
    {
        get => _synthesizer.Volume;
        set => _synthesizer.Volume = Math.Clamp(value, 0, 100);
    }

    public List<string> GetInstalledVoiceNames() =>
        _synthesizer.GetInstalledVoices()
            .Where(v => v.Enabled)
            .Select(v => v.VoiceInfo.Name)
            .ToList();

    public void SelectVoice(string voiceName)
    {
        try
        {
            _synthesizer.SelectVoice(voiceName);
        }
        catch (ArgumentException)
        {
            // Voice not installed; keep current selection.
        }
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        Stop();

        using var memoryStream = new MemoryStream();
        _synthesizer.SetOutputToWaveStream(memoryStream);
        _synthesizer.Speak(text);

        var audioData = memoryStream.ToArray();

        using var audioFileReader = new WaveFileReader(new MemoryStream(audioData));

        using var pcmStream = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int bytesRead;
        while ((bytesRead = audioFileReader.Read(buffer, 0, buffer.Length)) > 0)
        {
            pcmStream.Write(buffer, 0, bytesRead);
        }

        var pcm = pcmStream.ToArray();
        if (pcm.Length == 0) return;

        var provider = new BufferedWaveProvider(audioFileReader.WaveFormat);
        provider.AddSamples(pcm, 0, pcm.Length);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(provider);
        _waveOut.Play();

        var vbCableDeviceId = FindVBCableDevice();
        if (vbCableDeviceId != -1)
        {
            var vbProvider = new BufferedWaveProvider(audioFileReader.WaveFormat);
            vbProvider.AddSamples(pcm, 0, pcm.Length);
            _waveOutVBCable = new WaveOutEvent { DeviceNumber = vbCableDeviceId };
            _waveOutVBCable.Init(vbProvider);
            _waveOutVBCable.Play();
        }
    }

    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;

        _waveOutVBCable?.Stop();
        _waveOutVBCable?.Dispose();
        _waveOutVBCable = null;
    }

    private static int FindVBCableDevice()
    {
        for (var deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
        {
            var capabilities = WaveOut.GetCapabilities(deviceId);
            if (capabilities.ProductName.Contains("CABLE Input") ||
                capabilities.ProductName.Contains("VB-Audio Virtual Cable"))
            {
                return deviceId;
            }
        }
        return -1;
    }

    public void Dispose()
    {
        Stop();
        _synthesizer.Dispose();
    }
}
