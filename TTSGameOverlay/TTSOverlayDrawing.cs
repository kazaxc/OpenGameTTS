namespace TTSGameOverlay
{
    public partial class TtsOverlayForm : Form
    {
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
    }
}
