using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace JzeroCompilerNativeLite
{
    internal sealed class ThemePalette
    {
        public string Name { get; set; }
        public Color WindowBack { get; set; }
        public Color PanelBack { get; set; }
        public Color SurfaceBack { get; set; }
        public Color EditorBack { get; set; }
        public Color EditorGutter { get; set; }
        public Color TerminalBack { get; set; }
        public Color Text { get; set; }
        public Color MutedText { get; set; }
        public Color Accent { get; set; }
        public Color Danger { get; set; }
        public Color Grid { get; set; }
        public Color Selection { get; set; }
        public Color EditorCurrentLine { get; set; }
        public Color TabActive { get; set; }
        public Color TabInactive { get; set; }
    }

    internal sealed class EditorSettings
    {
        public string EditorFontFamily { get; set; }
        public float EditorFontSize { get; set; }
        public string OutputFontFamily { get; set; }
        public float OutputFontSize { get; set; }
        public string ThemeName { get; set; }
    }

    internal sealed class OffsetTextButton : Button
    {
        public Point TextOffset { get; set; }

        public OffsetTextButton()
        {
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var bounds = ClientRectangle;
            var fillColor = Enabled ? BackColor : ControlPaint.Light(BackColor);
            using (var backBrush = new SolidBrush(fillColor))
            using (var borderPen = new Pen(FlatAppearance.BorderColor.IsEmpty ? ControlPaint.Dark(fillColor) : FlatAppearance.BorderColor))
            {
                e.Graphics.FillRectangle(backBrush, bounds);
                e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, bounds.Width - 1), Math.Max(0, bounds.Height - 1));
            }

            var textBounds = new Rectangle(TextOffset.X, TextOffset.Y, bounds.Width, bounds.Height);
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, ForeColor, flags);
        }
    }

    internal sealed class SettingsDialog : Form
    {
        private static string[] cachedFontNames;
        private readonly ComboBox themeBox;
        private readonly ComboBox editorFontBox;
        private readonly ComboBox outputFontBox;
        private readonly TrackBar editorSizeSlider;
        private readonly TrackBar outputSizeSlider;
        private readonly Label editorSizeValue;
        private readonly Label outputSizeValue;
        private readonly FastColoredTextBox previewEditor;
        private readonly RichTextBox previewOutput;
        private readonly Dictionary<string, ThemePalette> themes;
        private readonly Label previewEditorMeta;
        private readonly Label previewOutputMeta;
        private readonly Label appearanceHeaderLabel;
        private readonly Label shortcutsHeaderLabel;
        private readonly Label editorPreviewHeaderLabel;
        private readonly Label outputPreviewHeaderLabel;
        private readonly Panel settingsPanel;
        private readonly Panel previewPanel;
        private readonly Panel buttonsPanel;
        private readonly TextBox shortcutsBox;
        private readonly OffsetTextButton okButton;
        private readonly OffsetTextButton cancelButton;
        private readonly OffsetTextButton resetButton;

        internal EditorSettings Result { get; private set; }

        private EditorSettings GetDefaultSettings()
        {
            return new EditorSettings
            {
                ThemeName = "Carbon",
                EditorFontFamily = "Consolas",
                EditorFontSize = 11F,
                OutputFontFamily = "Consolas",
                OutputFontSize = 10F
            };
        }

        internal SettingsDialog(IWin32Window owner, EditorSettings current, Dictionary<string, ThemePalette> themes)
        {
            this.themes = themes;

            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1080, 740);
            BackColor = Color.FromArgb(18, 18, 18);
            ForeColor = Color.FromArgb(236, 236, 236);
            Font = new Font("Segoe UI", 9F);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 2;
            root.Padding = new Padding(14);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            Controls.Add(root);

            settingsPanel = new Panel();
            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.BackColor = Color.FromArgb(24, 24, 24);
            settingsPanel.Padding = new Padding(14);

            previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.BackColor = Color.FromArgb(24, 24, 24);
            previewPanel.Padding = new Padding(14);

            buttonsPanel = new Panel();
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.BackColor = Color.Transparent;

            root.Controls.Add(settingsPanel, 0, 0);
            root.Controls.Add(previewPanel, 1, 0);
            root.Controls.Add(buttonsPanel, 0, 1);
            root.SetColumnSpan(buttonsPanel, 2);

            var settingsLayout = new TableLayoutPanel();
            settingsLayout.Dock = DockStyle.Fill;
            settingsLayout.ColumnCount = 1;
            settingsLayout.RowCount = 8;
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            settingsPanel.Controls.Add(settingsLayout);

            appearanceHeaderLabel = CreateSectionLabel("Appearance");
            settingsLayout.Controls.Add(appearanceHeaderLabel, 0, 0);

            themeBox = CreateComboBox(themes.Keys.OrderBy(item => item).ToArray(), current.ThemeName);
            settingsLayout.Controls.Add(CreateInlineRow("Theme", themeBox), 0, 1);

            var fontNames = GetFontNames();
            editorFontBox = CreateComboBox(fontNames, current.EditorFontFamily);
            ConfigureSearchableFontBox(editorFontBox);
            settingsLayout.Controls.Add(CreateInlineRow("Editor Font", editorFontBox), 0, 2);

            outputFontBox = CreateComboBox(fontNames, current.OutputFontFamily);
            ConfigureSearchableFontBox(outputFontBox);
            settingsLayout.Controls.Add(CreateInlineRow("Output Font", outputFontBox), 0, 3);

            editorSizeSlider = CreateSlider((int)current.EditorFontSize, 8, 28);
            editorSizeValue = CreateValueLabel();
            settingsLayout.Controls.Add(CreateSliderRow("Editor Size", editorSizeSlider, editorSizeValue), 0, 4);

            outputSizeSlider = CreateSlider((int)current.OutputFontSize, 8, 28);
            outputSizeValue = CreateValueLabel();
            settingsLayout.Controls.Add(CreateSliderRow("Output Size", outputSizeSlider, outputSizeValue), 0, 5);

            shortcutsHeaderLabel = CreateSectionLabel("Shortcuts");
            shortcutsHeaderLabel.Margin = new Padding(0);
            settingsLayout.Controls.Add(shortcutsHeaderLabel, 0, 6);

            shortcutsBox = new TextBox();
            shortcutsBox.Dock = DockStyle.Fill;
            shortcutsBox.Multiline = true;
            shortcutsBox.ReadOnly = true;
            shortcutsBox.BackColor = Color.FromArgb(14, 14, 14);
            shortcutsBox.ForeColor = Color.FromArgb(220, 220, 220);
            shortcutsBox.BorderStyle = BorderStyle.FixedSingle;
            shortcutsBox.Font = new Font("Consolas", 9F);
            shortcutsBox.Text =
                "Keyboard shortcuts" + Environment.NewLine +
                "Ctrl+S  Save active file" + Environment.NewLine +
                "Ctrl+W  Close active tab" + Environment.NewLine +
                "Ctrl+Z  Undo" + Environment.NewLine +
                "Ctrl+Y  Redo" + Environment.NewLine +
                "Ctrl+B  Hide/show workspace" + Environment.NewLine +
                "F5      Compile + Run" + Environment.NewLine +
                "Tab     Indent (editor control default)" + Environment.NewLine +
                "Double-click file in workspace to open" + Environment.NewLine +
                "Right-click file/folder for menu and Reveal in Explorer";
            settingsLayout.Controls.Add(shortcutsBox, 0, 7);

            var previewLayout = new TableLayoutPanel();
            previewLayout.Dock = DockStyle.Fill;
            previewLayout.ColumnCount = 1;
            previewLayout.RowCount = 6;
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            previewPanel.Controls.Add(previewLayout);

            editorPreviewHeaderLabel = CreateSectionLabel("Editor Preview");
            previewLayout.Controls.Add(editorPreviewHeaderLabel, 0, 0);

            previewEditorMeta = new Label();
            previewEditorMeta.Dock = DockStyle.Fill;
            previewEditorMeta.ForeColor = Color.FromArgb(170, 170, 170);
            previewLayout.Controls.Add(previewEditorMeta, 0, 1);

            previewEditor = new FastColoredTextBox();
            previewEditor.Dock = DockStyle.Fill;
            previewEditor.ReadOnly = true;
            previewEditor.Language = Language.CSharp;
            previewEditor.Text = "#include <stdio.h>\n\nint main() {\n    printf(\"Preview theme\\n\");\n    return 0;\n}\n";
            previewEditor.ShowLineNumbers = true;
            previewEditor.WordWrap = true;
            previewEditor.WordWrapMode = WordWrapMode.WordWrapControlWidth;
            previewEditor.BorderStyle = BorderStyle.FixedSingle;
            previewEditor.Paddings = new Padding(0);
            previewLayout.Controls.Add(previewEditor, 0, 2);

            outputPreviewHeaderLabel = CreateSectionLabel("Output Preview");
            previewLayout.Controls.Add(outputPreviewHeaderLabel, 0, 3);

            previewOutputMeta = new Label();
            previewOutputMeta.Dock = DockStyle.Fill;
            previewOutputMeta.ForeColor = Color.FromArgb(170, 170, 170);
            previewLayout.Controls.Add(previewOutputMeta, 0, 4);

            previewOutput = new RichTextBox();
            previewOutput.Dock = DockStyle.Fill;
            previewOutput.ReadOnly = true;
            previewOutput.BorderStyle = BorderStyle.FixedSingle;
            previewOutput.WordWrap = true;
            previewOutput.Text = "Compiling preview.c\r\nRunning...\r\nPreview theme\r\n[Process exited with code 0]";
            previewLayout.Controls.Add(previewOutput, 0, 5);

            okButton = new OffsetTextButton();
            okButton.Text = "Apply";
            okButton.DialogResult = DialogResult.OK;
            okButton.Size = new Size(110, 34);
            okButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            okButton.Location = new Point(832, 10);
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.BackColor = Color.FromArgb(0, 102, 204);
            okButton.ForeColor = Color.White;
            okButton.FlatAppearance.BorderColor = Color.FromArgb(0, 102, 204);
            okButton.TextAlign = ContentAlignment.MiddleCenter;
            okButton.Padding = Padding.Empty;
            okButton.TextOffset = Point.Empty;

            cancelButton = new OffsetTextButton();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Size = new Size(110, 34);
            cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cancelButton.Location = new Point(952, 10);
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.BackColor = Color.FromArgb(52, 52, 52);
            cancelButton.ForeColor = Color.White;
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(72, 72, 72);
            cancelButton.TextAlign = ContentAlignment.MiddleCenter;
            cancelButton.Padding = Padding.Empty;
            cancelButton.TextOffset = new Point(-5, 0);

            resetButton = new OffsetTextButton();
            resetButton.Text = "Reset All";
            resetButton.Size = new Size(110, 34);
            resetButton.Location = new Point(14, 10);
            resetButton.FlatStyle = FlatStyle.Flat;
            resetButton.BackColor = Color.FromArgb(72, 52, 24);
            resetButton.ForeColor = Color.White;
            resetButton.FlatAppearance.BorderColor = Color.FromArgb(96, 72, 36);
            resetButton.TextAlign = ContentAlignment.MiddleCenter;
            resetButton.Padding = Padding.Empty;
            resetButton.TextOffset = Point.Empty;
            resetButton.Click += delegate
            {
                var defaults = GetDefaultSettings();
                themeBox.Text = defaults.ThemeName;
                editorFontBox.Text = defaults.EditorFontFamily;
                outputFontBox.Text = defaults.OutputFontFamily;
                editorSizeSlider.Value = (int)defaults.EditorFontSize;
                outputSizeSlider.Value = (int)defaults.OutputFontSize;
                UpdateLabels();
                UpdatePreview();
            };

            buttonsPanel.Controls.Add(resetButton);
            buttonsPanel.Controls.Add(okButton);
            buttonsPanel.Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            themeBox.SelectedIndexChanged += PreviewSettingsChanged;
            editorFontBox.SelectedIndexChanged += PreviewSettingsChanged;
            outputFontBox.SelectedIndexChanged += PreviewSettingsChanged;
            editorSizeSlider.ValueChanged += PreviewSettingsChanged;
            outputSizeSlider.ValueChanged += PreviewSettingsChanged;

            UpdateLabels();
            UpdatePreview();

            if (ShowDialog(owner) == DialogResult.OK)
            {
                Result = new EditorSettings
                {
                    ThemeName = themeBox.Text,
                    EditorFontFamily = editorFontBox.Text,
                    EditorFontSize = editorSizeSlider.Value,
                    OutputFontFamily = outputFontBox.Text,
                    OutputFontSize = outputSizeSlider.Value
                };
            }
        }

        private void PreviewSettingsChanged(object sender, EventArgs e)
        {
            UpdateLabels();
            UpdatePreview();
        }

        private void UpdateLabels()
        {
            editorSizeValue.Text = editorSizeSlider.Value + " pt";
            outputSizeValue.Text = outputSizeSlider.Value + " pt";
        }

        private void UpdatePreview()
        {
            ThemePalette palette;
            if (!themes.TryGetValue(themeBox.Text, out palette))
            {
                return;
            }

            BackColor = palette.WindowBack;
            ForeColor = palette.Text;
            settingsPanel.BackColor = palette.PanelBack;
            previewPanel.BackColor = palette.PanelBack;
            buttonsPanel.BackColor = palette.WindowBack;
            appearanceHeaderLabel.ForeColor = palette.Text;
            shortcutsHeaderLabel.ForeColor = palette.Text;
            editorPreviewHeaderLabel.ForeColor = palette.Text;
            outputPreviewHeaderLabel.ForeColor = palette.Text;
            shortcutsBox.BackColor = palette.SurfaceBack;
            shortcutsBox.ForeColor = palette.Text;

            previewEditor.Font = new Font(editorFontBox.Text, editorSizeSlider.Value);
            previewEditor.BackColor = palette.EditorBack;
            previewEditor.PaddingBackColor = palette.EditorBack;
            previewEditor.ForeColor = palette.Text;
            previewEditor.LineNumberColor = palette.MutedText;
            previewEditor.IndentBackColor = palette.EditorGutter;
            previewEditor.ServiceLinesColor = palette.Grid;
            previewEditor.SelectionColor = palette.Selection;
            previewEditor.CurrentLineColor = palette.EditorCurrentLine;
            previewEditor.CaretColor = palette.Text;
            previewEditorMeta.Text = "Editor preview: " + editorFontBox.Text + ", " + editorSizeSlider.Value + " pt";
            previewEditorMeta.Font = new Font("Segoe UI", 9F);

            previewOutput.Font = new Font(outputFontBox.Text, outputSizeSlider.Value);
            previewOutput.BackColor = palette.TerminalBack;
            previewOutput.ForeColor = palette.Text;
            previewOutputMeta.Text = "Output preview: " + outputFontBox.Text + ", " + outputSizeSlider.Value + " pt";
            previewOutputMeta.Font = new Font("Segoe UI", 9F);

            themeBox.BackColor = palette.SurfaceBack;
            themeBox.ForeColor = palette.Text;
            editorFontBox.BackColor = palette.SurfaceBack;
            editorFontBox.ForeColor = palette.Text;
            outputFontBox.BackColor = palette.SurfaceBack;
            outputFontBox.ForeColor = palette.Text;
            okButton.BackColor = palette.Accent;
            okButton.ForeColor = palette.Text;
            okButton.FlatAppearance.BorderColor = palette.Accent;
            cancelButton.BackColor = palette.SurfaceBack;
            cancelButton.ForeColor = palette.Text;
            cancelButton.FlatAppearance.BorderColor = palette.Grid;
            resetButton.BackColor = palette.TabInactive;
            resetButton.ForeColor = palette.Text;
            resetButton.FlatAppearance.BorderColor = palette.Grid;
        }

        private Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 245, 245),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Control CreateInlineRow(string labelText, Control control)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(0, 8, 0, 0);

            var label = new Label();
            label.Text = labelText;
            label.Location = new Point(0, 8);
            label.Size = new Size(110, 24);
            label.TextAlign = ContentAlignment.MiddleLeft;

            control.Location = new Point(118, 4);
            control.Width = 243;
            control.Height = 32;

            panel.Controls.Add(control);
            panel.Controls.Add(label);
            return panel;
        }

        private Control CreateSliderRow(string labelText, TrackBar slider, Label valueLabel)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(0, 4, 0, 0);

            var label = new Label();
            label.Text = labelText;
            label.Location = new Point(0, 8);
            label.Size = new Size(110, 20);

            slider.Location = new Point(118, 0);
            slider.Size = new Size(210, 45);

            valueLabel.Location = new Point(336, 8);
            valueLabel.Size = new Size(60, 20);

            panel.Controls.Add(label);
            panel.Controls.Add(slider);
            panel.Controls.Add(valueLabel);
            return panel;
        }

        private ComboBox CreateComboBox(string[] values, string currentValue)
        {
            var combo = new ComboBox();
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.BackColor = Color.FromArgb(14, 14, 14);
            combo.ForeColor = Color.White;
            combo.FlatStyle = FlatStyle.Flat;
            combo.Items.AddRange(values.Cast<object>().ToArray());
            if (!string.IsNullOrWhiteSpace(currentValue) && values.Contains(currentValue))
            {
                combo.SelectedItem = currentValue;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            return combo;
        }

        private void ConfigureSearchableFontBox(ComboBox combo)
        {
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        private TrackBar CreateSlider(int value, int minimum, int maximum)
        {
            var slider = new TrackBar();
            slider.Minimum = minimum;
            slider.Maximum = maximum;
            slider.TickStyle = TickStyle.None;
            slider.Value = Math.Max(minimum, Math.Min(maximum, value));
            return slider;
        }

        private Label CreateValueLabel()
        {
            return new Label
            {
                ForeColor = Color.FromArgb(220, 220, 220)
            };
        }

        private string[] GetFontNames()
        {
            if (cachedFontNames != null)
            {
                return cachedFontNames;
            }

            var collection = new InstalledFontCollection();
            cachedFontNames = collection.Families
                .Where(IsCompatibleEditorFont)
                .Select(item => item.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToArray();
            return cachedFontNames;
        }

        private bool IsCompatibleEditorFont(FontFamily family)
        {
            if (family == null || string.IsNullOrWhiteSpace(family.Name))
            {
                return false;
            }

            if (!family.IsStyleAvailable(FontStyle.Regular))
            {
                return false;
            }

            try
            {
                using (var narrowFont = new Font(family, 11F, FontStyle.Regular))
                using (var wideFont = new Font(family, 11F, FontStyle.Regular))
                {
                    var narrow = TextRenderer.MeasureText("iiiiiiiiii", narrowFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    var wide = TextRenderer.MeasureText("WWWWWWWWWW", wideFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    return Math.Abs(narrow.Width - wide.Width) <= 2;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
