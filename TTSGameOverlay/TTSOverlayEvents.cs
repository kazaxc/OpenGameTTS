namespace TTSGameOverlay
{
    public partial class TtsOverlayForm : Form
    {
        private void DropdownButton_Click(object? sender, EventArgs e)
        {
            ToggleSettingsPanel();
        }

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

                }
            }
        }

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
    }
}
