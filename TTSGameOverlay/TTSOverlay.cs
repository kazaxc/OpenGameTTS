using NAudio.Wave;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;

namespace TTSGameOverlay
{
    public partial class TtsOverlayForm : Form
    {
        // Windows API imports for advanced window control
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int WM_HOTKEY = 0x0312;
        private const int SW_RESTORE = 9;

        private const uint MOD_CONTROL = 0x0002;
        private const int HOTKEY_ID_FOCUS = 0x1001;
        private const uint VK_RETURN = 0x0D;

        // Constants for SetWindowPos
        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // TTS components
        private SpeechSynthesizer? synthesizer;
        private BufferedWaveProvider? waveProvider;
        private WaveOutEvent? waveOut;
        private WaveOutEvent? waveOutVBCable;

        // UI Controls
        private TextBox? textInput;
        private Button? dropdownButton;
        private Panel? settingsPanel;
        private Button? exitButton;
        private ListBox? voiceListBox;
        private Label? voiceLabel;
        private Label? volumeLabel;
        private TrackBar? volumeSlider;

        // For dragging the window
        private bool isDragging = false;
        private Point dragCursor;
        private Point dragForm;

        // Settings panel state
        private bool isSettingsExpanded = false;
        private readonly int collapsedHeight = 50;
        private readonly int expandedHeight = 270; // increased to prevent cut-off

        public TtsOverlayForm()
        {
            InitializeComponent();
            InitializeTTS();
            SetupAlwaysOnTop();
        }

        private void InitializeComponent()
        {
            // Form properties - minimal dark overlay
            this.Text = "TTS Overlay";
            this.Size = new Size(350, collapsedHeight);
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(45, 45, 45); // Dark grey
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(100, 100);
            this.ShowInTaskbar = false;
            this.AllowTransparency = true;
            this.Opacity = 0.85;

            // Make form draggable by clicking anywhere (except controls)
            this.MouseDown += Form_MouseDown;
            this.MouseMove += Form_MouseMove;
            this.MouseUp += Form_MouseUp;

            // Create rounded text input
            textInput = new TextBox
            {
                Location = new Point(15, 12),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 13),
                BackColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                Text = "Type to speak...",
                ForeColor = Color.Gray
            };

            // Handle placeholder text
            textInput.GotFocus += TextInput_GotFocus;
            textInput.LostFocus += TextInput_LostFocus;
            textInput.KeyPress += TextInput_KeyPress;

            // Create dropdown arrow button
            dropdownButton = new Button
            {
                Location = new Point(305, 12),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Text = "",
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = false
            };
            dropdownButton.FlatAppearance.BorderSize = 0;
            dropdownButton.Click += DropdownButton_Click;
            dropdownButton.Paint += DropdownButton_Paint;

            // Create settings panel (initially hidden)
            // increased height slightly to fit larger ListBox + controls
            settingsPanel = new Panel
            {
                Location = new Point(10, 50),
                Size = new Size(330, 210), // increased to match expandedHeight comfortably
                BackColor = Color.FromArgb(50, 50, 50),
                Visible = false
            };

            // Voice selection label
            voiceLabel = new Label
            {
                Location = new Point(5, 5),
                Size = new Size(55, 20),
                Text = "Voice",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            // Make ListBox tall enough to show 3 items at ItemHeight = 28
            voiceListBox = new ListBox
            {
                Location = new Point(5, 28),
                Size = new Size(320, 84), // 3 * 28 = 84
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.One,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28
            };
            voiceListBox.DrawItem += VoiceListBox_DrawItem;
            voiceListBox.SelectedIndexChanged += VoiceListBox_SelectedIndexChanged;

            volumeLabel = new Label
            {
                Location = new Point(5, 116), // placed under the taller list box
                Size = new Size(70, 20),
                Text = "Volume",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            volumeSlider = new TrackBar
            {
                Location = new Point(5, 136), // below the volume label
                Size = new Size(320, 30),
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickStyle = TickStyle.None,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            // Centered exit button with adequate spacing and larger size so text fits
            int exitBtnWidth = 100;
            int panelWidth = 330;
            exitButton = new Button
            {
                Location = new Point((panelWidth - exitBtnWidth) / 2, 176), // centered horizontally within panel
                Size = new Size(exitBtnWidth, 30), // wider and taller so "Exit" displays comfortably
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Text = "Exit",
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = false
            };
            exitButton.FlatAppearance.BorderSize = 0;
            exitButton.Click += ExitButton_Click;

            settingsPanel.Controls.Add(voiceLabel);
            settingsPanel.Controls.Add(voiceListBox);
            settingsPanel.Controls.Add(volumeLabel);
            settingsPanel.Controls.Add(volumeSlider);
            settingsPanel.Controls.Add(exitButton);

            this.Controls.Add(textInput);
            this.Controls.Add(dropdownButton);
            this.Controls.Add(settingsPanel);

            // Apply rounded corners
            this.Paint += Form_Paint;
            this.Resize += (s, e) => this.Invalidate();
        }

        private void InitializeTTS()
        {
            try
            {
                synthesizer = new SpeechSynthesizer
                {
                    // Configure speech settings for better quality and spacing
                    Rate = 0,    // Normal speech rate (-10 to 10, 0 is default)
                    Volume = 100 // Maximum volume (0 to 100)
                };

                // Populate voice list box
                if (voiceListBox != null)
                {
                    foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
                    {
                        voiceListBox.Items.Add(voice.VoiceInfo.Name);
                    }

                    // Set default voice if available
                    if (voiceListBox.Items.Count > 0)
                    {
                        voiceListBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception)
            {
                // Handle initialization errors silently
            }
        }

        private void SetupAlwaysOnTop()
        {
            this.TopMost = true;

            // Use Windows API for more reliable always-on-top behavior
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private static int FindVBCableDevice()
        {
            for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
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

        private void SpeakText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                // Stop any currently playing audio
                waveOut?.Stop();
                waveOut?.Dispose();
                waveOutVBCable?.Stop();
                waveOutVBCable?.Dispose();
                waveProvider = null;
                waveOut = null;
                waveOutVBCable = null;

                using var memoryStream = new System.IO.MemoryStream();
                synthesizer?.SetOutputToWaveStream(memoryStream);
                synthesizer?.Speak(text);

                byte[] audioData = memoryStream.ToArray();
                if (audioData.Length > 44)
                {
                    using var audioFileReader = new WaveFileReader(new System.IO.MemoryStream(audioData));

                    waveProvider = new BufferedWaveProvider(audioFileReader.WaveFormat);

                    // Add ALL the audio data at once (no chunking)
                    var audioDataWithoutHeader = new byte[audioData.Length - 44];
                    Array.Copy(audioData, 44, audioDataWithoutHeader, 0, audioDataWithoutHeader.Length);
                    waveProvider.AddSamples(audioDataWithoutHeader, 0, audioDataWithoutHeader.Length);

                    // Output to default device
                    waveOut = new WaveOutEvent();
                    waveOut.Init(waveProvider);
                    waveOut.Play();

                    // For VB Cable - create a completely separate provider
                    int vbCableDeviceId = FindVBCableDevice();
                    if (vbCableDeviceId != -1)
                    {
                        var waveProviderVB = new BufferedWaveProvider(audioFileReader.WaveFormat);
                        waveProviderVB.AddSamples(audioDataWithoutHeader, 0, audioDataWithoutHeader.Length);

                        waveOutVBCable = new WaveOutEvent();
                        waveOutVBCable.DeviceNumber = vbCableDeviceId;
                        waveOutVBCable.Init(waveProviderVB);
                        waveOutVBCable.Play();
                    }
                }
            }
            catch (Exception)
            {
                // Create an error box popup
                MessageBox.Show("An error occurred while trying to speak the text.", "TTS Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && (int)m.WParam == HOTKEY_ID_FOCUS)
            {
                try
                {
                    // restore if minimized, bring to front and set input focus
                    ShowWindow(this.Handle, SW_RESTORE);
                    SetForegroundWindow(this.Handle);
                    this.Activate();
                }
                catch
                {
                    // swallow errors
                }
                return;
            }

            // existing handling: keep always-on-top after system menu actions
            if (m.Msg == 0x0112 && this.WindowState == FormWindowState.Normal) // WM_SYSCOMMAND
            {
                SetupAlwaysOnTop();
            }

            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { UnregisterHotKey(this.Handle, HOTKEY_ID_FOCUS); } catch { }

            waveOut?.Stop();
            waveOut?.Dispose();
            waveOutVBCable?.Stop();
            waveOutVBCable?.Dispose();
            synthesizer?.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Register Ctrl + Enter as system-wide hotkey
            try
            {
                RegisterHotKey(this.Handle, HOTKEY_ID_FOCUS, MOD_CONTROL, VK_RETURN);
            }
            catch
            {
                // ignore registration failure in minimal UI
            }
        }
    }
}
