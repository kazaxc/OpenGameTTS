using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using NAudio.Gui;
using NAudio.Wave;

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
        private readonly int expandedHeight = 240;

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
            settingsPanel = new Panel
            {
                Location = new Point(10, 50),
                Size = new Size(330, 180),
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

            // Voice selection list box - now full width with margins
            voiceListBox = new ListBox
            {
                Location = new Point(5, 28),
                Size = new Size(320, 45), // Full width minus margins (330 - 10 = 320)
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.One,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22
            };
            voiceListBox.DrawItem += VoiceListBox_DrawItem;
            voiceListBox.SelectedIndexChanged += VoiceListBox_SelectedIndexChanged;

            // Volume label
            volumeLabel = new Label
            {
                Location = new Point(5, 85),
                Size = new Size(70, 20),
                Text = "Volume",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            // Volume slider - now full width with margins
            volumeSlider = new TrackBar
            {
                Location = new Point(5, 108),
                Size = new Size(320, 30), // Full width minus margins (330 - 10 = 320)
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickStyle = TickStyle.None,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            // Exit button (centered horizontally in settings panel)
            exitButton = new Button
            {
                Location = new Point(125, 145), // Centered: (330 - 80) / 2 = 125
                Size = new Size(80, 30),
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

            // Add controls to settings panel
            settingsPanel.Controls.Add(voiceLabel);
            settingsPanel.Controls.Add(voiceListBox);
            settingsPanel.Controls.Add(volumeLabel);
            settingsPanel.Controls.Add(volumeSlider);
            settingsPanel.Controls.Add(exitButton);

            // Add all controls to form
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
                synthesizer = new SpeechSynthesizer();

                // Configure speech settings for better quality and spacing
                synthesizer.Rate = 0;    // Normal speech rate (-10 to 10, 0 is default)
                synthesizer.Volume = 100; // Maximum volume (0 to 100)

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
                // Silently handle errors in minimal UI
            }
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

        // Event handlers for dropdown and settings
        private void DropdownButton_Click(object? sender, EventArgs e)
        {
            ToggleSettingsPanel();
        }

        private void ToggleSettingsPanel()
        {
            isSettingsExpanded = !isSettingsExpanded;

            if (isSettingsExpanded)
            {
                this.Size = new Size(350, expandedHeight);
                settingsPanel!.Visible = true;
            }
            else
            {
                this.Size = new Size(350, collapsedHeight);
                settingsPanel!.Visible = false;
            }

            // Repaint the dropdown button to update the arrow
            dropdownButton?.Invalidate();

            this.Invalidate(); // Redraw with new size
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void VoiceListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (synthesizer != null && voiceListBox != null && voiceListBox.SelectedItem != null)
            {
                try
                {
                    synthesizer.SelectVoice(voiceListBox.SelectedItem.ToString()!);
                }
                catch (Exception)
                {
                    // Handle voice selection errors silently
                }
            }
        }

        private void VolumeSlider_ValueChanged(object? sender, EventArgs e)
        {
            if (synthesizer != null && volumeSlider != null)
            {
                try
                {
                    synthesizer.Volume = volumeSlider.Value;
                }
                catch (Exception)
                {
                    // Handle volume change errors silently
                }
            }
        }

        // Update the VoiceListBox_DrawItem method to left-align the text:
        private void VoiceListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || voiceListBox?.Items == null) return;

            e.DrawBackground();

            // Set colors for selected vs unselected items
            Color textColor = e.State.HasFlag(DrawItemState.Selected)
                ? Color.Black  // Selected item text (dark on light background)
                : Color.White; // Unselected item text

            Color backColor = e.State.HasFlag(DrawItemState.Selected)
                ? Color.FromArgb(100, 149, 237) // Light blue highlight for selected
                : Color.FromArgb(50, 50, 50);   // Dark background for unselected (match ListBox background)

            // Fill background
            using var backBrush = new SolidBrush(backColor);
            e.Graphics.FillRectangle(backBrush, e.Bounds);

            // Draw text left-aligned
            using var textBrush = new SolidBrush(textColor);
            var text = voiceListBox.Items[e.Index].ToString();

            // Create StringFormat for left alignment
            using var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,        // Left alignment
                LineAlignment = StringAlignment.Center   // Vertical center
            };

            // Add some left padding to the text
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            e.Graphics.DrawString(text, e.Font!, textBrush, textRect, stringFormat);

            e.DrawFocusRectangle();
        }

        // Event handlers for minimalist UI
        private void TextInput_GotFocus(object? sender, EventArgs e)
        {
            if (textInput?.Text == "Type to speak..." && textInput.ForeColor == Color.Gray)
            {
                textInput.Text = "";
                textInput.ForeColor = Color.White;
            }
        }

        private void TextInput_LostFocus(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textInput?.Text) && textInput != null)
            {
                textInput.Text = "Type to speak...";
                textInput.ForeColor = Color.Gray;
            }
        }

        private void TextInput_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(textInput?.Text) &&
                    textInput.Text != "Type to speak...")
                {
                    SpeakText(textInput.Text);
                    textInput.Text = "";
                }
            }
            else if (e.KeyChar == (char)Keys.Escape)
            {
                this.Close();
            }
        }

        // Form dragging (only when clicking on form background, not controls)
        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            var control = this.GetChildAtPoint(e.Location);
            if (control == null)
            {
                isDragging = true;
                dragCursor = Cursor.Position;
                dragForm = this.Location;
            }
        }

        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursor));
                this.Location = Point.Add(dragForm, new Size(diff));
            }
        }

        private void Form_MouseUp(object? sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        // Rounded corners painting
        private void Form_Paint(object? sender, PaintEventArgs e)
        {
            // Create rounded rectangle path
            int cornerRadius = 15;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            var rect = new Rectangle(0, 0, this.Width, this.Height);

            path.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
            path.AddArc(rect.Right - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
            path.AddArc(rect.Right - cornerRadius * 2, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            path.CloseFigure();

            // Apply the rounded shape to the form
            this.Region = new Region(path);

            // Draw subtle border
            using var pen = new Pen(Color.FromArgb(80, 80, 80), 1);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOutVBCable?.Stop();
            waveOutVBCable?.Dispose();
            synthesizer?.Dispose();
            base.OnFormClosing(e);
        }

        // Override WndProc to handle special window messages (optional)
        protected override void WndProc(ref Message m)
        {
            // Handle minimize/restore to maintain always-on-top
            if (m.Msg == 0x0112 && this.WindowState == FormWindowState.Normal) // WM_SYSCOMMAND
            {
                SetupAlwaysOnTop();
            }
            base.WndProc(ref m);
        }

        private void DropdownButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is Button button)
            {
                // Clear the button
                e.Graphics.Clear(button.BackColor);

                // Draw the arrow manually centered
                using var brush = new SolidBrush(button.ForeColor);
                using var font = new Font("Segoe UI", 10);

                string text = isSettingsExpanded ? ">" : "v";
                var textSize = e.Graphics.MeasureString(text, font);

                float x = (button.Width - textSize.Width) / 2;
                float y = (button.Height - textSize.Height) / 2;

                e.Graphics.DrawString(text, font, brush, x, y);
            }
        }
    }

    // Program entry point
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create and show the overlay form
            var overlayForm = new TtsOverlayForm();
            Application.Run(overlayForm);
        }
    }
}