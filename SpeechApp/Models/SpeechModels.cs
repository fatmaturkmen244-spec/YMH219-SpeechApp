namespace SpeechApp.Models;

/// <summary>
/// Uygulamada o anda hangi ses işleminin aktif olduğunu belirtir.
/// </summary>
public enum ActiveSpeechMode
{
    Idle,
    TtsSpeaking,
    TtsPaused,
    SttListening,
    Error
}

/// <summary>
/// Mikrofondan tanınan bir konuşma cümlesini zaman damgası ve güven skoruyla temsil eder.
/// </summary>
public sealed class RecognizedSpeechEntry
{
    public DateTime Timestamp { get; }
    public float Confidence { get; }
    public string Text { get; }

    public RecognizedSpeechEntry(DateTime timestamp, float confidence, string text)
    {
        Timestamp = timestamp;
        Confidence = Math.Clamp(confidence, 0f, 1f);
        Text = text;
    }

    public string ToFormattedLog()
    {
        int pct = (int)Math.Round(Confidence * 100);
        return $"[{Timestamp:HH:mm:ss}] (Güven: %{pct,2})  {Text}";
    }

    public override string ToString() => ToFormattedLog();
}
