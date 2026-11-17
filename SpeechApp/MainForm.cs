using System.Speech.Synthesis;
using SpeechApp.Models;
using SpeechApp.Services;

namespace SpeechApp;

public sealed class MainForm : Form
{
    private readonly TextToSpeechService _ttsService = new();
    private readonly SpeechToTextService _sttService = new();
    private readonly SpeechFileExporter _fileExporter = new();
    private readonly List<RecognizedSpeechEntry> _recognizedEntries = [];

    private readonly Label _lblActiveOperationBadge;
    private readonly Label _lblFooterStatus;

    // Text-to-Speech (Sol Panel) Kontrolleri
    private readonly RichTextBox _rtbTtsInput;
    private readonly ComboBox _cmbVoices;
    private readonly TrackBar _trkRate;
    private readonly Label _lblRateVal;
    private readonly TrackBar _trkVolume;
    private readonly Label _lblVolumeVal;
    private readonly Button _btnTtsSpeak;
    private readonly Button _btnTtsPauseResume;
    private readonly Button _btnTtsStop;
    private readonly Button _btnTtsClear;

    // Speech-to-Text (Sağ Panel) Kontrolleri
    private readonly ComboBox _cmbRecognizers;
    private readonly Button _btnSttStart;
    private readonly Button _btnSttStop;
    private readonly Button _btnSttClear;
    private readonly Button _btnSaveTxt;
    private readonly RichTextBox _rtbSttOutput;
    private readonly ListBox _lstSttTimestampLog;

    public MainForm()
    {
        Text = "YMH219 — Speech-to-Text & Text-to-Speech Ses Stüdyosu";
        ClientSize = new Size(1000, 665);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(255, 241, 242);
        ForeColor = Color.FromArgb(30, 41, 59);

        // 1. Üst Başlık ve Aktif İşlem Göstergesi (Coral Audio Studio Header)
        Panel headerPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.FromArgb(136, 19, 55)
        };
        Controls.Add(headerPanel);

        Panel coralTopStripe = new()
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(251, 113, 133)
        };
        headerPanel.Controls.Add(coralTopStripe);

        Label lblTitle = new()
        {
            Text = "🎙️  SPEECH-TO-TEXT & TEXT-TO-SPEECH AUDIO STUDIO",
            Font = new Font("Segoe UI", 14.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 13),
            AutoSize = true
        };
        headerPanel.Controls.Add(lblTitle);

        Label lblSubtitle = new()
        {
            Text = ".NET System.Speech Sentezleyici & Konuşma Tanıma Motoru · Ses Hızı/Ton Seçimi · Zaman Damgalı Kayıt",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(254, 205, 211),
            Location = new Point(22, 43),
            AutoSize = true
        };
        headerPanel.Controls.Add(lblSubtitle);

        _lblActiveOperationBadge = new Label
        {
            Text = "⚪ AKTİF İŞLEM: BOŞTA (HAZIR)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(136, 19, 55),
            BackColor = Color.FromArgb(254, 226, 226),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(692, 20),
            Size = new Size(290, 36)
        };
        headerPanel.Controls.Add(_lblActiveOperationBadge);

        // 2. Sol Kart: Text-to-Speech (Metinden Sese Okuma)
        Panel ttsCard = new()
        {
            Location = new Point(16, 90),
            Size = new Size(476, 528),
            BackColor = Color.White
        };
        Controls.Add(ttsCard);

        Panel ttsHeader = new()
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(255, 228, 230)
        };
        ttsCard.Controls.Add(ttsHeader);

        Label lblTtsTitle = new()
        {
            Text = "🔊  TEXT-TO-SPEECH (METİNDEN SESE OKUMA)",
            Font = new Font("Segoe UI", 10.2f, FontStyle.Bold),
            ForeColor = Color.FromArgb(159, 18, 57),
            Location = new Point(14, 11),
            AutoSize = true
        };
        ttsHeader.Controls.Add(lblTtsTitle);

        Label lblTtsPrompt = new()
        {
            Text = "Sesli Okunacak Metin (RichTextBox):",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(14, 54),
            AutoSize = true
        };
        ttsCard.Controls.Add(lblTtsPrompt);

        _rtbTtsInput = new RichTextBox
        {
            Location = new Point(14, 76),
            Size = new Size(448, 178),
            Font = new Font("Segoe UI", 10.5f),
            BackColor = Color.FromArgb(250, 250, 250),
            ForeColor = Color.FromArgb(15, 23, 42),
            BorderStyle = BorderStyle.FixedSingle,
            Text = "Merhaba! Bu uygulama Nesne Tabanlı Programlama dersi kapsamında geliştirilmiştir.\n\nText-to-Speech modülü ile yazdığınız metinleri seçtiğiniz ses profili ve konuşma hızında sesli olarak dinleyebilirsiniz."
        };
        ttsCard.Controls.Add(_rtbTtsInput);

        // TTS Butonları (Başlat, Duraklat/Devam, Durdur, Temizle)
        _btnTtsSpeak = CreateStudioButton("▶ Başlat (Oku)", 14, 266, 118, 36, Color.FromArgb(225, 29, 72), Color.White);
        _btnTtsSpeak.Click += (_, _) => HandleTtsStart();
        ttsCard.Controls.Add(_btnTtsSpeak);

        _btnTtsPauseResume = CreateStudioButton("⏸ Duraklat", 138, 266, 112, 36, Color.FromArgb(217, 119, 6), Color.White);
        _btnTtsPauseResume.Enabled = false;
        _btnTtsPauseResume.Click += (_, _) => HandleTtsPauseResume();
        ttsCard.Controls.Add(_btnTtsPauseResume);

        _btnTtsStop = CreateStudioButton("⏹ Durdur", 256, 266, 100, 36, Color.FromArgb(71, 85, 105), Color.White);
        _btnTtsStop.Enabled = false;
        _btnTtsStop.Click += (_, _) => HandleTtsStop();
        ttsCard.Controls.Add(_btnTtsStop);

        _btnTtsClear = CreateStudioButton("🗑 Temizle", 362, 266, 100, 36, Color.FromArgb(244, 63, 94), Color.White);
        _btnTtsClear.Click += (_, _) =>
        {
            _ttsService.Stop();
            _rtbTtsInput.Clear();
            SetActiveMode(ActiveSpeechMode.Idle, "TTS metin alanı temizlendi.");
        };
        ttsCard.Controls.Add(_btnTtsClear);

        // Ses Seçimi ve Hız/Seviye Ayarları (İsteğe Bağlı Geliştirme)
        Panel ttsSettingsPanel = new()
        {
            Location = new Point(14, 316),
            Size = new Size(448, 196),
            BackColor = Color.FromArgb(255, 245, 245)
        };
        ttsCard.Controls.Add(ttsSettingsPanel);

        Label lblSettingsHeader = new()
        {
            Text = "🎛️  SES SENTEZLEYİCİ AYARLARI (İsteğe Bağlı Geliştirmeler)",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(159, 18, 57),
            Location = new Point(12, 10),
            AutoSize = true
        };
        ttsSettingsPanel.Controls.Add(lblSettingsHeader);

        Label lblVoice = new()
        {
            Text = "Kurulu Ses Profili:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(12, 42),
            AutoSize = true
        };
        ttsSettingsPanel.Controls.Add(lblVoice);

        _cmbVoices = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.2f),
            Location = new Point(136, 38),
            Size = new Size(298, 26)
        };
        PopulateInstalledVoices();
        ttsSettingsPanel.Controls.Add(_cmbVoices);

        Label lblRate = new()
        {
            Text = "Konuşma Hızı:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(12, 86),
            AutoSize = true
        };
        ttsSettingsPanel.Controls.Add(lblRate);

        _lblRateVal = new Label
        {
            Text = "0 (Normal)",
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(159, 18, 57),
            Location = new Point(366, 86),
            AutoSize = true
        };
        ttsSettingsPanel.Controls.Add(_lblRateVal);

        _trkRate = new TrackBar
        {
            Minimum = -10,
            Maximum = 10,
            Value = 0,
            TickFrequency = 2,
            Location = new Point(130, 78),
            Size = new Size(235, 42)
        };
        _trkRate.ValueChanged += (_, _) => _lblRateVal.Text = $"{_trkRate.Value:+#;-#;0} (Normal)";
        ttsSettingsPanel.Controls.Add(_trkRate);

        Label lblVol = new()
        {
            Text = "Ses Seviyesi:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(12, 140),
            AutoSize = true
        };
        ttsSettingsPanel.Controls.Add(lblVol);

        _lblVolumeVal = new Label
        {
            Text = "%95",
            Font = new Font("Consolas", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(159, 18, 57),
            Location = new Point(368, 140),
            AutoSize = true
        };
        ttsSettingsPanel.Controls.Add(_lblVolumeVal);

        _trkVolume = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 95,
            TickFrequency = 10,
            Location = new Point(130, 132),
            Size = new Size(235, 42)
        };
        _trkVolume.ValueChanged += (_, _) => _lblVolumeVal.Text = $"%{_trkVolume.Value}";
        ttsSettingsPanel.Controls.Add(_trkVolume);

        // 3. Sağ Kart: Speech-to-Text (Konuşmadan Metne Dönüşüm)
        Panel sttCard = new()
        {
            Location = new Point(506, 90),
            Size = new Size(478, 528),
            BackColor = Color.White
        };
        Controls.Add(sttCard);

        Panel sttHeader = new()
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(254, 243, 199)
        };
        sttCard.Controls.Add(sttHeader);

        Label lblSttTitle = new()
        {
            Text = "🎙️  SPEECH-TO-TEXT (MİKROFONDAN METNE)",
            Font = new Font("Segoe UI", 10.2f, FontStyle.Bold),
            ForeColor = Color.FromArgb(146, 64, 14),
            Location = new Point(14, 11),
            AutoSize = true
        };
        sttHeader.Controls.Add(lblSttTitle);

        Label lblRecLang = new()
        {
            Text = "Tanıma Motoru:",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(14, 58),
            AutoSize = true
        };
        sttCard.Controls.Add(lblRecLang);

        _cmbRecognizers = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f),
            Location = new Point(116, 54),
            Size = new Size(348, 26)
        };
        PopulateRecognizers();
        sttCard.Controls.Add(_cmbRecognizers);

        // STT Butonları
        _btnSttStart = CreateStudioButton("🎙️ Dinlemeyi Başlat", 14, 90, 142, 36, Color.FromArgb(5, 150, 105), Color.White);
        _btnSttStart.Click += (_, _) => HandleSttStart();
        sttCard.Controls.Add(_btnSttStart);

        _btnSttStop = CreateStudioButton("⏹ Durdur", 162, 90, 92, 36, Color.FromArgb(71, 85, 105), Color.White);
        _btnSttStop.Enabled = false;
        _btnSttStop.Click += (_, _) => HandleSttStop();
        sttCard.Controls.Add(_btnSttStop);

        _btnSttClear = CreateStudioButton("🗑 Temizle", 260, 90, 92, 36, Color.FromArgb(239, 68, 68), Color.White);
        _btnSttClear.Click += (_, _) =>
        {
            _rtbSttOutput.Clear();
            _lstSttTimestampLog.Items.Clear();
            _recognizedEntries.Clear();
            _lblFooterStatus.Text = "Durum: Speech-to-Text metin alanı ve zaman damgalı kayıtlar temizlendi.";
        };
        sttCard.Controls.Add(_btnSttClear);

        _btnSaveTxt = CreateStudioButton("💾 .TXT Kaydet", 358, 90, 106, 36, Color.FromArgb(14, 165, 233), Color.White);
        _btnSaveTxt.Click += (_, _) => HandleExportTxt();
        sttCard.Controls.Add(_btnSaveTxt);

        Label lblSttBoxHeader = new()
        {
            Text = "Mikrofondan Aktarılan Konuşma Metni (RichTextBox):",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(14, 138),
            AutoSize = true
        };
        sttCard.Controls.Add(lblSttBoxHeader);

        _rtbSttOutput = new RichTextBox
        {
            Location = new Point(14, 160),
            Size = new Size(450, 138),
            Font = new Font("Segoe UI", 10.2f),
            BackColor = Color.FromArgb(250, 250, 250),
            ForeColor = Color.FromArgb(15, 23, 42),
            BorderStyle = BorderStyle.FixedSingle
        };
        sttCard.Controls.Add(_rtbSttOutput);

        Label lblSttLogHeader = new()
        {
            Text = "ZAMAN DAMGALI KONUŞMA TANIMA SONUÇLARI (ListBox):",
            Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(146, 64, 14),
            Location = new Point(14, 310),
            AutoSize = true
        };
        sttCard.Controls.Add(lblSttLogHeader);

        _lstSttTimestampLog = new ListBox
        {
            Location = new Point(14, 332),
            Size = new Size(450, 180),
            Font = new Font("Consolas", 9.2f),
            BackColor = Color.FromArgb(24, 24, 27),
            ForeColor = Color.FromArgb(167, 243, 208),
            BorderStyle = BorderStyle.FixedSingle,
            HorizontalScrollbar = true
        };
        sttCard.Controls.Add(_lstSttTimestampLog);

        // 4. Alt Durum Çubuğu
        _lblFooterStatus = new Label
        {
            Text = "Hazır: Metni sesli okutmak için sol paneli, mikrofondan konuşmayı metne aktarmak için sağ paneli kullanın.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(76, 5, 25),
            Dock = DockStyle.Bottom,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0)
        };
        Controls.Add(_lblFooterStatus);

        // Servis Olay Abonelikleri
        _ttsService.StateChanged += state => SafeInvoke(() =>
        {
            if (state == SynthesizerState.Speaking)
            {
                SetActiveMode(ActiveSpeechMode.TtsSpeaking, "Text-to-Speech aktif: Metin sesli olarak okunuyor...");
            }
            else if (state == SynthesizerState.Paused)
            {
                SetActiveMode(ActiveSpeechMode.TtsPaused, "Text-to-Speech duraklatıldı.");
            }
        });

        _ttsService.SpeakCompleted += msg => SafeInvoke(() =>
        {
            _btnTtsPauseResume.Enabled = false;
            _btnTtsStop.Enabled = false;
            _btnTtsPauseResume.Text = "⏸ Duraklat";
            if (!_sttService.IsListening)
            {
                SetActiveMode(ActiveSpeechMode.Idle, msg);
            }
        });

        _sttService.SpeechRecognized += entry => SafeInvoke(() => AppendRecognizedEntry(entry));
        _sttService.RecognitionError += err => SafeInvoke(() => _lblFooterStatus.Text = $"Bilgi: {err}");

        FormClosing += (_, _) =>
        {
            _ttsService.Dispose();
            _sttService.Dispose();
        };
    }

    private static Button CreateStudioButton(string text, int x, int y, int width, int height, Color bg, Color fg)
    {
        Button btn = new()
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(x, y),
            Size = new Size(width, height),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SafeInvoke(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    private void PopulateInstalledVoices()
    {
        _cmbVoices.Items.Clear();
        IReadOnlyList<string> voices = _ttsService.GetInstalledVoiceNames();
        foreach (string v in voices)
        {
            _cmbVoices.Items.Add(v);
        }

        if (_cmbVoices.Items.Count == 0)
        {
            _cmbVoices.Items.Add("Microsoft Default Desktop Voice");
        }

        _cmbVoices.SelectedIndex = 0;
    }

    private void PopulateRecognizers()
    {
        _cmbRecognizers.Items.Clear();
        IReadOnlyList<string> recs = _sttService.GetAvailableRecognizers();
        foreach (string r in recs)
        {
            _cmbRecognizers.Items.Add(r);
        }

        if (_cmbRecognizers.Items.Count == 0)
        {
            _cmbRecognizers.Items.Add("Varsayılan Sistem Mikrofon ve Konuşma Motoru");
        }

        _cmbRecognizers.SelectedIndex = 0;
    }

    private void SetActiveMode(ActiveSpeechMode mode, string footerMessage)
    {
        _lblFooterStatus.Text = $"Durum: {footerMessage}";

        switch (mode)
        {
            case ActiveSpeechMode.TtsSpeaking:
                _lblActiveOperationBadge.Text = "🔊 AKTİF: METİN SESLİ OKUNUYOR (TTS)";
                _lblActiveOperationBadge.BackColor = Color.FromArgb(251, 113, 133);
                _lblActiveOperationBadge.ForeColor = Color.White;
                break;

            case ActiveSpeechMode.TtsPaused:
                _lblActiveOperationBadge.Text = "⏸ DURAKLATILDI: TEXT-TO-SPEECH";
                _lblActiveOperationBadge.BackColor = Color.FromArgb(253, 224, 71);
                _lblActiveOperationBadge.ForeColor = Color.FromArgb(113, 63, 18);
                break;

            case ActiveSpeechMode.SttListening:
                _lblActiveOperationBadge.Text = "🎙️ AKTİF: MİKROFON DİNLENİYOR (STT)";
                _lblActiveOperationBadge.BackColor = Color.FromArgb(52, 211, 153);
                _lblActiveOperationBadge.ForeColor = Color.FromArgb(6, 78, 59);
                break;

            case ActiveSpeechMode.Error:
                _lblActiveOperationBadge.Text = "⚠️ UYARI: SERVİS / MİKROFON HATASI";
                _lblActiveOperationBadge.BackColor = Color.FromArgb(254, 202, 202);
                _lblActiveOperationBadge.ForeColor = Color.FromArgb(153, 27, 27);
                break;

            default:
                _lblActiveOperationBadge.Text = "⚪ AKTİF İŞLEM: BOŞTA (HAZIR)";
                _lblActiveOperationBadge.BackColor = Color.FromArgb(254, 226, 226);
                _lblActiveOperationBadge.ForeColor = Color.FromArgb(136, 19, 55);
                break;
        }
    }

    private void HandleTtsStart()
    {
        string text = _rtbTtsInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(
                "Lütfen sesli okutmak için metin kutusuna bir metin girin.",
                "Metin Boş",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (_cmbVoices.SelectedItem is string selectedVoice)
            {
                _ttsService.SelectVoiceIfAvailable(selectedVoice);
            }

            _ttsService.ConfigureAudio(_trkRate.Value, _trkVolume.Value);
            _ttsService.SpeakTextAsync(text);

            _btnTtsPauseResume.Enabled = true;
            _btnTtsPauseResume.Text = "⏸ Duraklat";
            _btnTtsStop.Enabled = true;
            SetActiveMode(ActiveSpeechMode.TtsSpeaking, "Text-to-Speech başlatıldı: Metin seslendiriliyor...");
        }
        catch (Exception ex)
        {
            SetActiveMode(ActiveSpeechMode.Error, $"Ses sentezleme hatası: {ex.Message}");
            MessageBox.Show(
                $"Sesli okuma servisine erişilirken bir hata oluştu:\n\n{ex.Message}",
                "Text-to-Speech Hatası",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void HandleTtsPauseResume()
    {
        try
        {
            _ttsService.TogglePauseResume();
            if (_ttsService.IsPaused)
            {
                _btnTtsPauseResume.Text = "▶ Devam Et";
                SetActiveMode(ActiveSpeechMode.TtsPaused, "Sesli okuma duraklatıldı.");
            }
            else
            {
                _btnTtsPauseResume.Text = "⏸ Duraklat";
                SetActiveMode(ActiveSpeechMode.TtsSpeaking, "Sesli okumaya devam ediliyor...");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Duraklatma işlemi sırasında hata:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void HandleTtsStop()
    {
        _ttsService.Stop();
        _btnTtsPauseResume.Enabled = false;
        _btnTtsStop.Enabled = false;
        _btnTtsPauseResume.Text = "⏸ Duraklat";
        SetActiveMode(ActiveSpeechMode.Idle, "Text-to-Speech kullanıcı tarafından durduruldu.");
    }

    private void HandleSttStart()
    {
        try
        {
            string? selectedRec = _cmbRecognizers.SelectedItem?.ToString();
            _sttService.StartListening(selectedRec);
            _btnSttStart.Enabled = false;
            _btnSttStop.Enabled = true;
            SetActiveMode(ActiveSpeechMode.SttListening, "Mikrofon dinleniyor... Konuştuğunuz cümleler metin alanına aktarılacaktır.");
        }
        catch (Exception ex)
        {
            SetActiveMode(ActiveSpeechMode.Error, "Mikrofon veya konuşma tanıma servisine erişilemedi.");
            MessageBox.Show(
                $"Mikrofon veya Konuşma Tanıma servisi başlatılamadı!\n\n{ex.Message}",
                "Speech-to-Text Erişim Hatası",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void HandleSttStop()
    {
        _sttService.StopListening();
        _btnSttStart.Enabled = true;
        _btnSttStop.Enabled = false;
        SetActiveMode(ActiveSpeechMode.Idle, "Mikrofon dinleme işlemi durduruldu.");
    }

    private void AppendRecognizedEntry(RecognizedSpeechEntry entry)
    {
        _recognizedEntries.Add(entry);
        if (_rtbSttOutput.TextLength > 0)
        {
            _rtbSttOutput.AppendText(" ");
        }
        _rtbSttOutput.AppendText(entry.Text + ".");
        _rtbSttOutput.ScrollToCaret();

        _lstSttTimestampLog.Items.Add(entry.ToFormattedLog());
        _lstSttTimestampLog.TopIndex = Math.Max(0, _lstSttTimestampLog.Items.Count - 1);
    }

    private void HandleExportTxt()
    {
        using SaveFileDialog sfd = new()
        {
            Title = "Dönüştürülen Metni ve Konuşma Kayıtlarını .TXT Dosyasına Kaydet",
            Filter = "Metin Dosyası (*.txt)|*.txt",
            FileName = $"SpeechTranscript_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _fileExporter.ExportToFile(sfd.FileName, _rtbTtsInput.Text, _rtbSttOutput.Text, _recognizedEntries);
            _lblFooterStatus.Text = $"Başarılı: Metin ve konuşma kayıtları '{Path.GetFileName(sfd.FileName)}' dosyasına kaydedildi.";
            MessageBox.Show($"Dosya başarıyla kaydedildi:\n{sfd.FileName}", "Kayıt Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dosya kaydedilirken hata oluştu:\n{ex.Message}", "Dosya Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void CaptureScreenshots(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        Show();
        Application.DoEvents();

        // Ekran Görüntüsü 1: Text-to-Speech (TTS) Aktif, Ses Hızı/Ton Ayarlanmış
        _trkRate.Value = 2;
        _lblRateVal.Text = "+2 (Hızlı)";
        _trkVolume.Value = 95;
        _btnTtsPauseResume.Enabled = true;
        _btnTtsStop.Enabled = true;
        SetActiveMode(ActiveSpeechMode.TtsSpeaking, "Text-to-Speech aktif: RichTextBox içindeki metin sesli olarak okunuyor (Hız: +2, Ses: %95).");

        DateTime t0 = DateTime.Today.AddHours(15).AddMinutes(10).AddSeconds(12);
        _recognizedEntries.Clear();
        _rtbSttOutput.Clear();
        _lstSttTimestampLog.Items.Clear();
        AppendRecognizedEntry(new RecognizedSpeechEntry(t0, 0.96f, "Merhaba dünya ses tanıma testi başarılı"));
        AppendRecognizedEntry(new RecognizedSpeechEntry(t0.AddSeconds(6), 0.93f, "Nesne tabanlı programlama ödevi tamamlandı"));

        Refresh();
        Application.DoEvents();

        using (Bitmap bmp1 = new(Width, Height))
        {
            DrawToBitmap(bmp1, new Rectangle(0, 0, Width, Height));
            bmp1.Save(Path.Combine(outputDir, "tts_active.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        // Ekran Görüntüsü 2: Speech-to-Text (STT) Mikrofon Dinleme Aktif + Zaman Damgalı Sonuç Listesi + .TXT Kayıt
        _btnTtsPauseResume.Enabled = false;
        _btnTtsStop.Enabled = false;
        _btnSttStart.Enabled = false;
        _btnSttStop.Enabled = true;
        AppendRecognizedEntry(new RecognizedSpeechEntry(t0.AddSeconds(14), 0.95f, "Mikrofondan alınan konuşma metin alanına aktarıldı"));
        AppendRecognizedEntry(new RecognizedSpeechEntry(t0.AddSeconds(21), 0.91f, "Dönüştürülen metin dosyaya kaydedilmeye hazır"));
        SetActiveMode(ActiveSpeechMode.SttListening, "Speech-to-Text aktif: Mikrofon dinleniyor ve cümleler zaman damgasıyla listeleniyor.");

        Refresh();
        Application.DoEvents();

        using (Bitmap bmp2 = new(Width, Height))
        {
            DrawToBitmap(bmp2, new Rectangle(0, 0, Width, Height));
            bmp2.Save(Path.Combine(outputDir, "stt_listening.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        Close();
    }
}
