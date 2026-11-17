using System.Speech.Synthesis;

namespace SpeechApp.Services;

/// <summary>
/// System.Speech.Synthesis.SpeechSynthesizer kullanarak metni sesli okuma (TTS),
/// duraklatma, devam ettirme, ses seçimi ve hız ayarı işlemlerini yönetir.
/// </summary>
public sealed class TextToSpeechService : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer;
    public bool IsPaused { get; private set; }
    public bool IsSpeaking
    {
        get
        {
            try
            {
                return _synthesizer.State == SynthesizerState.Speaking;
            }
            catch
            {
                return false;
            }
        }
    }

    public event Action<SynthesizerState>? StateChanged;
    public event Action<string>? SpeakCompleted;

    public TextToSpeechService()
    {
        _synthesizer = new SpeechSynthesizer();
        try
        {
            _synthesizer.SetOutputToDefaultAudioDevice();
        }
        catch
        {
            // Ses çıkış aygıtı yoksa veya erişilemiyorsa SpeakTextAsync sırasında bildirilir
        }

        try
        {
            _synthesizer.StateChanged += (_, e) => StateChanged?.Invoke(e.State);
            _synthesizer.SpeakCompleted += (_, e) =>
            {
                IsPaused = false;
                if (e.Error != null)
                {
                    SpeakCompleted?.Invoke($"Okuma hatası: {e.Error.Message}");
                }
                else if (e.Cancelled)
                {
                    SpeakCompleted?.Invoke("Sesli okuma durduruldu.");
                }
                else
                {
                    SpeakCompleted?.Invoke("Sesli okuma tamamlandı.");
                }
            };
        }
        catch
        {
            // SAPI başlatma kısıtlaması durumunda güvenli devam et
        }
    }

    public IReadOnlyList<string> GetInstalledVoiceNames()
    {
        try
        {
            return _synthesizer.GetInstalledVoices()
                .Where(v => v.Enabled)
                .Select(v => v.VoiceInfo.Name)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public void SelectVoiceIfAvailable(string voiceName)
    {
        if (string.IsNullOrWhiteSpace(voiceName))
        {
            return;
        }

        try
        {
            _synthesizer.SelectVoice(voiceName);
        }
        catch
        {
            // Seçilen ses motoru desteklenmiyorsa varsayılan ses kullanılır
        }
    }

    public void ConfigureAudio(int rate, int volume)
    {
        _synthesizer.Rate = Math.Clamp(rate, -10, 10);
        _synthesizer.Volume = Math.Clamp(volume, 0, 100);
    }

    public void SpeakTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Okutulacak metin boş olamaz.");
        }

        Stop();
        IsPaused = false;
        _synthesizer.SpeakAsync(text);
    }

    public void TogglePauseResume()
    {
        if (_synthesizer.State == SynthesizerState.Speaking)
        {
            _synthesizer.Pause();
            IsPaused = true;
        }
        else if (_synthesizer.State == SynthesizerState.Paused)
        {
            _synthesizer.Resume();
            IsPaused = false;
        }
    }

    public void Stop()
    {
        try
        {
            if (_synthesizer.State == SynthesizerState.Paused)
            {
                _synthesizer.Resume();
            }

            _synthesizer.SpeakAsyncCancelAll();
            IsPaused = false;
        }
        catch
        {
            // Yoksay
        }
    }

    public void Dispose()
    {
        Stop();
        _synthesizer.Dispose();
    }
}
