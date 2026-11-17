using System.Globalization;
using System.Speech.Recognition;
using System.Text;
using SpeechApp.Models;

namespace SpeechApp.Services;

/// <summary>
/// System.Speech.Recognition.SpeechRecognitionEngine kullanarak mikrofondan alınan
/// konuşmayı metne dönüştürür (STT) ve mikrofon/dil paketi hatalarını yakalar.
/// </summary>
public sealed class SpeechToTextService : IDisposable
{
    private SpeechRecognitionEngine? _engine;
    public bool IsListening { get; private set; }

    public event Action<RecognizedSpeechEntry>? SpeechRecognized;
    public event Action<string>? RecognitionError;

    public IReadOnlyList<string> GetAvailableRecognizers()
    {
        try
        {
            return SpeechRecognitionEngine.InstalledRecognizers()
                .Select(r => $"{r.Culture.Name} — {r.Description}")
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public void StartListening(string? preferredCultureName = null)
    {
        if (IsListening)
        {
            return;
        }

        StopListening();

        IReadOnlyCollection<RecognizerInfo> installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0)
        {
            throw new InvalidOperationException(
                "Sistemde yüklü bir Windows Konuşma Tanıma (Speech Recognition) dil paketi bulunamadı.\n" +
                "Windows Ayarları > Zaman ve Dil > Konuşma bölümünden konuşma tanıma paketini etkinleştirebilirsiniz.");
        }

        RecognizerInfo? selectedInfo = null;
        if (!string.IsNullOrWhiteSpace(preferredCultureName))
        {
            string cultureCode = preferredCultureName.Split(' ')[0].Trim();
            selectedInfo = installed.FirstOrDefault(r =>
                r.Culture.Name.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
        }

        selectedInfo ??= installed.First();

        _engine = new SpeechRecognitionEngine(selectedInfo.Culture);

        try
        {
            _engine.SetInputToDefaultAudioDevice();
        }
        catch (Exception ex)
        {
            _engine.Dispose();
            _engine = null;
            throw new InvalidOperationException(
                $"Varsayılan mikrofon aygıtına erişilemedi!\nLütfen mikrofonunuzun takılı ve Windows ses ayarlarında etkin olduğundan emin olun.\n(Teknik Detay: {ex.Message})");
        }

        // Hem serbest dikte hem de Türkçe/İngilizce temel komut grameri yükle
        _engine.LoadGrammar(new DictationGrammar());

        Choices samplePhrases = new(
            "merhaba dünya",
            "nesne tabanlı programlama",
            "uygulama testi başarılı",
            "ses tanıma sistemi aktif",
            "hello world",
            "object oriented programming");
        GrammarBuilder gb = new(samplePhrases) { Culture = selectedInfo.Culture };
        _engine.LoadGrammar(new Grammar(gb));

        _engine.SpeechRecognized += (_, e) =>
        {
            if (e.Result != null && !string.IsNullOrWhiteSpace(e.Result.Text))
            {
                RecognizedSpeechEntry entry = new(DateTime.Now, e.Result.Confidence, e.Result.Text);
                SpeechRecognized?.Invoke(entry);
            }
        };

        _engine.SpeechRecognitionRejected += (_, _) =>
        {
            RecognitionError?.Invoke("Konuşma algılandı ancak net olarak çözümlenemedi. Lütfen mikrofona daha net konuşun.");
        };

        _engine.RecognizeAsync(RecognizeMode.Multiple);
        IsListening = true;
    }

    public void StopListening()
    {
        if (_engine == null)
        {
            IsListening = false;
            return;
        }

        try
        {
            _engine.RecognizeAsyncCancel();
            _engine.Dispose();
        }
        catch
        {
            // Yoksay
        }
        finally
        {
            _engine = null;
            IsListening = false;
        }
    }

    public void Dispose()
    {
        StopListening();
    }
}

/// <summary>
/// Dönüştürülen metni ve zaman damgalı konuşma tanıma kayıtlarını .txt dosyasına kaydeder.
/// </summary>
public sealed class SpeechFileExporter
{
    public void ExportToFile(string filePath, string ttsText, string sttTranscript, IReadOnlyList<RecognizedSpeechEntry> entries)
    {
        using StreamWriter writer = new(filePath, append: false, Encoding.UTF8);
        writer.WriteLine("==========================================================================");
        writer.WriteLine(" YMH 219 — SPEECH-TO-TEXT & TEXT-TO-SPEECH DÖNÜŞÜM RAPORU");
        writer.WriteLine($" Kayıt Tarihi: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine("==========================================================================");
        writer.WriteLine();
        writer.WriteLine("--- 1. TEXT-TO-SPEECH (SESLİ OKUNAN METİN) ---");
        writer.WriteLine(string.IsNullOrWhiteSpace(ttsText) ? "(Boş)" : ttsText.Trim());
        writer.WriteLine();
        writer.WriteLine("--- 2. SPEECH-TO-TEXT (KONUŞMADAN METNE AKTARILAN PARAGRAF) ---");
        writer.WriteLine(string.IsNullOrWhiteSpace(sttTranscript) ? "(Boş)" : sttTranscript.Trim());
        writer.WriteLine();
        writer.WriteLine("--- 3. ZAMAN DAMGALI KONUŞMA TANIMA GÜNLÜĞÜ ---");
        foreach (RecognizedSpeechEntry entry in entries)
        {
            writer.WriteLine(entry.ToFormattedLog());
        }
    }
}
