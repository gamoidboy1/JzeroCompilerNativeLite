using System;
using System.Drawing;
using System.Windows.Forms;

namespace JzeroCompilerNativeLite
{
    internal sealed class PromptDialog : Form
    {
        private readonly TextBox inputBox;

        private PromptDialog(string title, string message, string defaultValue)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 150);
            BackColor = Color.FromArgb(24, 30, 42);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            var messageLabel = new Label
            {
                Text = message,
                AutoSize = false,
                Size = new Size(380, 32),
                Location = new Point(20, 18),
                ForeColor = Color.FromArgb(220, 226, 236)
            };

            inputBox = new TextBox
            {
                Text = defaultValue ?? string.Empty,
                Location = new Point(20, 58),
                Size = new Size(380, 24),
                BackColor = Color.FromArgb(15, 20, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(244, 102),
                Size = new Size(75, 28)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(325, 102),
                Size = new Size(75, 28)
            };

            Controls.Add(messageLabel);
            Controls.Add(inputBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        internal static string Show(IWin32Window owner, string title, string message, string defaultValue)
        {
            using (var dialog = new PromptDialog(title, message, defaultValue))
            {
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.inputBox.Text.Trim() : null;
            }
        }
    }
}
