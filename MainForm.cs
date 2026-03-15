using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace JzeroCompilerNativeLite
{
    internal sealed class MainForm : Form
    {
        private readonly string[] allowedExtensions = { ".c", ".h", ".txt" };
        private readonly string[] statusTexts = { "READY", "SAVED", "COMPILING", "COMPILE FAILED", "RUNNING" };
        private const int WorkspacePanelMinWidth = 220;
        private const int EditorPanelMinWidth = 360;
        private const int TerminalPanelMinWidth = 250;
        private readonly string configDirectory;
        private readonly string configPath;
        private readonly Dictionary<string, EditorDocument> openDocuments = new Dictionary<string, EditorDocument>(StringComparer.OrdinalIgnoreCase);
        private readonly System.Windows.Forms.Timer autoSaveTimer;
        private readonly string[] cKeywords =
        {
            "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else",
            "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register",
            "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union",
            "unsigned", "void", "volatile", "while", "bool", "true", "false", "NULL"
        };

        private string workspacePath;
        private bool autoClearTerminal = true;
        private float editorFontSize = 11F;
        private float outputFontSize = 10F;
        private string editorFontFamily = "Consolas";
        private string outputFontFamily = "Consolas";
        private string themeName = "Carbon";
        private List<string> lastOpenFiles = new List<string>();
        private List<string> recentFiles = new List<string>();
        private string lastSelectedFilePath;
        private Process currentProcess;
        private Process compileProcess;
        private CompileRequest activeCompileRequest;
        private bool restartExecutionRequested;
        private bool stopRequested;
        private int terminalInputStart;
        private int terminalWidthPercent = 45;
        private string currentWorkspaceDirectory;
        private readonly List<string> workspaceHistory = new List<string>();
        private int workspaceHistoryIndex = -1;
        private bool suppressDocumentDirtyTracking;
        private bool suppressWorkspaceHistoryPush;
        private bool suppressAutocompleteUpdates;
        private bool suppressAutocompleteCommitErrors;
        private string workspaceDropHighlightPath;
        private bool workspaceUpDropHighlighted;
        private bool workspaceRootDropHighlighted;
        private bool workspaceLabelDropHighlighted;
        private FastColoredTextBox autocompleteEditor;
        private ToolStripDropDown autocompletePopup;
        private ListBox autocompleteList;
        private int autocompleteReplaceStart = -1;
        private int autocompleteReplaceLength;

        private WorkspaceListView workspaceList;
        private ImageList workspaceRowHeightImageList;
        private TextBox searchBox;
        private Label workspaceLabel;
        private Label workspaceHintLabel;
        private TabControl editorTabs;
        private Panel emptyEditorPanel;
        private HiddenScrollRichTextBox terminalBox;
        private SplitContainer workspaceSplit;
        private SplitContainer editorTerminalSplit;
        private Label statusLabel;
        private Button workspaceButton;
        private Button workspaceBackButton;
        private Button workspaceRootButton;
        private Button workspaceUpButton;
        private Button workspaceRefreshButton;
        private Button newFileButton;
        private Button recentFilesButton;
        private Button saveButton;
        private Button settingsButton;
        private Button runButton;
        private Button stopButton;
        private Button autoClearButton;
        private Label caretLabel;
        private bool workspaceVisible = true;
        private int workspaceSplitterDistance = 320;
        private int terminalSplitterDistance = 760;
        private readonly Dictionary<string, ThemePalette> themes = CreateThemes();
        private readonly string[] autocompleteSnippets =
        {
            "printf", "scanf", "fprintf", "fscanf", "snprintf", "sprintf", "puts", "gets",
            "main", "#include", "return", "malloc", "calloc", "realloc", "free", "memset",
            "memcpy", "strlen", "strcmp", "strncmp", "strcpy", "strncpy", "fopen", "fclose",
            "fgets", "fputs", "FILE", "size_t", "stdin", "stdout", "stderr", "NULL"
        };

        private static Dictionary<string, ThemePalette> CreateThemes()
        {
            return new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "Carbon",
                    new ThemePalette
                    {
                        Name = "Carbon",
                        WindowBack = Color.FromArgb(10, 10, 10),
                        PanelBack = Color.FromArgb(20, 20, 20),
                        SurfaceBack = Color.FromArgb(34, 34, 34),
                        EditorBack = Color.FromArgb(12, 12, 12),
                        EditorGutter = Color.FromArgb(16, 16, 16),
                        TerminalBack = Color.FromArgb(12, 12, 12),
                        Text = Color.FromArgb(236, 236, 236),
                        MutedText = Color.FromArgb(120, 120, 120),
                        Accent = Color.FromArgb(0, 102, 204),
                        Danger = Color.FromArgb(132, 32, 32),
                        Grid = Color.FromArgb(56, 56, 56),
                        Selection = Color.FromArgb(60, 90, 140, 200),
                        EditorCurrentLine = Color.FromArgb(24, 24, 24),
                        TabActive = Color.FromArgb(30, 30, 30),
                        TabInactive = Color.FromArgb(18, 18, 18)
                    }
                },
                {
                    "Forest",
                    new ThemePalette
                    {
                        Name = "Forest",
                        WindowBack = Color.FromArgb(12, 18, 12),
                        PanelBack = Color.FromArgb(24, 34, 24),
                        SurfaceBack = Color.FromArgb(34, 48, 34),
                        EditorBack = Color.FromArgb(15, 22, 15),
                        EditorGutter = Color.FromArgb(20, 30, 20),
                        TerminalBack = Color.FromArgb(14, 20, 14),
                        Text = Color.FromArgb(228, 238, 220),
                        MutedText = Color.FromArgb(132, 160, 132),
                        Accent = Color.FromArgb(46, 143, 86),
                        Danger = Color.FromArgb(138, 62, 42),
                        Grid = Color.FromArgb(56, 78, 56),
                        Selection = Color.FromArgb(70, 120, 90, 180),
                        EditorCurrentLine = Color.FromArgb(24, 36, 24),
                        TabActive = Color.FromArgb(34, 48, 34),
                        TabInactive = Color.FromArgb(22, 30, 22)
                    }
                },
                {
                    "Sand",
                    new ThemePalette
                    {
                        Name = "Sand",
                        WindowBack = Color.FromArgb(28, 24, 18),
                        PanelBack = Color.FromArgb(42, 34, 24),
                        SurfaceBack = Color.FromArgb(58, 48, 34),
                        EditorBack = Color.FromArgb(24, 20, 16),
                        EditorGutter = Color.FromArgb(34, 28, 20),
                        TerminalBack = Color.FromArgb(20, 18, 14),
                        Text = Color.FromArgb(242, 228, 204),
                        MutedText = Color.FromArgb(180, 154, 120),
                        Accent = Color.FromArgb(196, 126, 34),
                        Danger = Color.FromArgb(154, 70, 42),
                        Grid = Color.FromArgb(92, 76, 52),
                        Selection = Color.FromArgb(160, 120, 52, 160),
                        EditorCurrentLine = Color.FromArgb(38, 30, 22),
                        TabActive = Color.FromArgb(50, 40, 28),
                        TabInactive = Color.FromArgb(34, 28, 20)
                    }
                },
                {
                    "Ocean",
                    new ThemePalette
                    {
                        Name = "Ocean",
                        WindowBack = Color.FromArgb(8, 16, 24),
                        PanelBack = Color.FromArgb(16, 28, 40),
                        SurfaceBack = Color.FromArgb(24, 42, 58),
                        EditorBack = Color.FromArgb(10, 18, 28),
                        EditorGutter = Color.FromArgb(14, 24, 34),
                        TerminalBack = Color.FromArgb(8, 16, 24),
                        Text = Color.FromArgb(220, 236, 244),
                        MutedText = Color.FromArgb(128, 162, 180),
                        Accent = Color.FromArgb(34, 138, 196),
                        Danger = Color.FromArgb(170, 74, 68),
                        Grid = Color.FromArgb(40, 66, 88),
                        Selection = Color.FromArgb(48, 116, 168, 180),
                        EditorCurrentLine = Color.FromArgb(18, 30, 42),
                        TabActive = Color.FromArgb(18, 30, 42),
                        TabInactive = Color.FromArgb(12, 22, 32)
                    }
                },
                {
                    "Cherry",
                    new ThemePalette
                    {
                        Name = "Cherry",
                        WindowBack = Color.FromArgb(24, 10, 16),
                        PanelBack = Color.FromArgb(38, 16, 24),
                        SurfaceBack = Color.FromArgb(62, 28, 38),
                        EditorBack = Color.FromArgb(18, 10, 16),
                        EditorGutter = Color.FromArgb(28, 14, 22),
                        TerminalBack = Color.FromArgb(16, 10, 14),
                        Text = Color.FromArgb(244, 224, 228),
                        MutedText = Color.FromArgb(184, 134, 144),
                        Accent = Color.FromArgb(214, 82, 116),
                        Danger = Color.FromArgb(160, 52, 52),
                        Grid = Color.FromArgb(88, 38, 52),
                        Selection = Color.FromArgb(180, 78, 112, 170),
                        EditorCurrentLine = Color.FromArgb(30, 16, 24),
                        TabActive = Color.FromArgb(40, 18, 28),
                        TabInactive = Color.FromArgb(24, 12, 18)
                    }
                },
                {
                    "Slate",
                    new ThemePalette
                    {
                        Name = "Slate",
                        WindowBack = Color.FromArgb(18, 22, 28),
                        PanelBack = Color.FromArgb(28, 34, 42),
                        SurfaceBack = Color.FromArgb(42, 50, 60),
                        EditorBack = Color.FromArgb(16, 20, 26),
                        EditorGutter = Color.FromArgb(22, 28, 34),
                        TerminalBack = Color.FromArgb(14, 18, 24),
                        Text = Color.FromArgb(232, 236, 240),
                        MutedText = Color.FromArgb(148, 156, 166),
                        Accent = Color.FromArgb(108, 146, 214),
                        Danger = Color.FromArgb(176, 84, 84),
                        Grid = Color.FromArgb(64, 74, 86),
                        Selection = Color.FromArgb(88, 122, 180, 160),
                        EditorCurrentLine = Color.FromArgb(26, 32, 40),
                        TabActive = Color.FromArgb(34, 40, 48),
                        TabInactive = Color.FromArgb(22, 28, 34)
                    }
                },
                {
                    "Mint Light",
                    new ThemePalette
                    {
                        Name = "Mint Light",
                        WindowBack = Color.FromArgb(232, 244, 234),
                        PanelBack = Color.FromArgb(214, 232, 216),
                        SurfaceBack = Color.FromArgb(196, 220, 198),
                        EditorBack = Color.FromArgb(246, 252, 246),
                        EditorGutter = Color.FromArgb(224, 238, 224),
                        TerminalBack = Color.FromArgb(242, 249, 242),
                        Text = Color.FromArgb(34, 68, 42),
                        MutedText = Color.FromArgb(92, 128, 98),
                        Accent = Color.FromArgb(54, 148, 82),
                        Danger = Color.FromArgb(176, 84, 84),
                        Grid = Color.FromArgb(162, 192, 166),
                        Selection = Color.FromArgb(120, 186, 132, 130),
                        EditorCurrentLine = Color.FromArgb(232, 246, 232),
                        TabActive = Color.FromArgb(228, 242, 228),
                        TabInactive = Color.FromArgb(206, 226, 208)
                    }
                }
            };
        }

        internal MainForm()
        {
            configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JzeroCompilerNativeLite");
            configPath = Path.Combine(configDirectory, "config.json");
            workspacePath = GetDefaultWorkspace();
            currentWorkspaceDirectory = workspacePath;
            LoadConfig();

            autoSaveTimer = new System.Windows.Forms.Timer();
            autoSaveTimer.Interval = 1000;
            autoSaveTimer.Tick += AutoSaveTimerTick;

            BuildUi();
            InitializeAutocomplete();
            ApplyEditorSettings(new EditorSettings
            {
                ThemeName = themeName,
                EditorFontFamily = editorFontFamily,
                EditorFontSize = editorFontSize,
                OutputFontFamily = outputFontFamily,
                OutputFontSize = outputFontSize
            });
            EnsureWorkspaceExists();
            LoadTree();
            Load += MainFormLoad;
            Shown += MainFormShown;
            FormClosing += MainFormClosing;
        }

        private void BuildUi()
        {
            Text = "Jzero Compiler Native Lite";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1380, 820);
            Size = new Size(1500, 900);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }
            BackColor = Color.FromArgb(10, 10, 10);
            ForeColor = Color.FromArgb(228, 228, 228);
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;
            KeyDown += MainFormKeyDown;
            Resize += MainFormResize;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 2;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.BackColor = Color.FromArgb(10, 10, 10);
            Controls.Add(root);

            root.Controls.Add(BuildTopBar(), 0, 0);
            root.Controls.Add(BuildMainLayout(), 0, 1);
        }

        private Control BuildTopBar()
        {
            var bar = new Panel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = Color.FromArgb(20, 20, 20);
            bar.Padding = new Padding(10, 8, 10, 8);

            var title = new Label();
            title.Text = "JZERO COMPILER NATIVE LITE";
            title.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(245, 245, 245);
            title.AutoSize = true;
            title.Location = new Point(10, 12);

            var leftButtons = new FlowLayoutPanel();
            leftButtons.Dock = DockStyle.Left;
            leftButtons.AutoSize = true;
            leftButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            leftButtons.WrapContents = false;
            leftButtons.FlowDirection = FlowDirection.LeftToRight;
            leftButtons.Padding = new Padding(280, 0, 0, 0);
            leftButtons.BackColor = Color.Transparent;

            var rightButtons = new FlowLayoutPanel();
            rightButtons.Dock = DockStyle.Right;
            rightButtons.AutoSize = true;
            rightButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rightButtons.WrapContents = false;
            rightButtons.FlowDirection = FlowDirection.LeftToRight;
            rightButtons.BackColor = Color.Transparent;

            workspaceButton = CreateButton("Workspace", 110, Color.FromArgb(34, 34, 34));
            workspaceButton.Click += WorkspaceButtonClick;

            newFileButton = CreateButton("New", 70, Color.FromArgb(34, 34, 34));
            newFileButton.Click += delegate { CreateItem(false, string.IsNullOrWhiteSpace(currentWorkspaceDirectory) ? workspacePath : currentWorkspaceDirectory); };

            saveButton = CreateButton("Save", 80, Color.FromArgb(34, 34, 34));
            saveButton.Click += SaveButtonClick;

            recentFilesButton = CreateButton("Recent", 80, Color.FromArgb(34, 34, 34));
            recentFilesButton.Click += RecentFilesButtonClick;

            settingsButton = CreateButton("Settings", 100, Color.FromArgb(34, 34, 34));
            settingsButton.Click += SettingsButtonClick;

            autoClearButton = CreateButton(string.Empty, 120, Color.FromArgb(34, 34, 34));
            autoClearButton.Click += AutoClearButtonClick;
            UpdateAutoClearButton();

            stopButton = CreateButton("Stop", 80, Color.FromArgb(80, 80, 80));
            stopButton.Click += StopButtonClick;

            runButton = CreateButton("Compile", 100, Color.FromArgb(0, 102, 204));
            runButton.Click += RunButtonClick;

            caretLabel = new Label();
            caretLabel.AutoSize = false;
            caretLabel.TextAlign = ContentAlignment.MiddleCenter;
            caretLabel.Size = new Size(90, 30);
            caretLabel.Margin = new Padding(0, 3, 6, 0);
            caretLabel.BackColor = Color.FromArgb(34, 34, 34);
            caretLabel.BorderStyle = BorderStyle.FixedSingle;
            caretLabel.ForeColor = Color.FromArgb(168, 168, 168);
            caretLabel.Text = "Ln 1, Col 1";

            var statusWidth = CalculateStatusBoxWidth();
            statusLabel = new Label();
            statusLabel.Text = "READY";
            statusLabel.AutoSize = false;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Size = new Size(statusWidth, 30);
            statusLabel.Margin = new Padding(0, 3, 6, 0);
            statusLabel.BackColor = Color.FromArgb(34, 34, 34);
            statusLabel.BorderStyle = BorderStyle.FixedSingle;
            statusLabel.ForeColor = Color.FromArgb(168, 168, 168);

            leftButtons.Controls.Add(workspaceButton);
            leftButtons.Controls.Add(newFileButton);
            leftButtons.Controls.Add(saveButton);
            leftButtons.Controls.Add(recentFilesButton);
            leftButtons.Controls.Add(settingsButton);

            rightButtons.Controls.Add(caretLabel);
            rightButtons.Controls.Add(statusLabel);
            rightButtons.Controls.Add(autoClearButton);
            rightButtons.Controls.Add(stopButton);
            rightButtons.Controls.Add(runButton);
            UpdateStopButtonState();

            bar.Controls.Add(title);
            bar.Controls.Add(rightButtons);
            bar.Controls.Add(leftButtons);
            return bar;
        }

        private Control BuildMainLayout()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(8);
            panel.BackColor = Color.FromArgb(10, 10, 10);

            workspaceSplit = new SplitContainer();
            workspaceSplit.Dock = DockStyle.Fill;
            workspaceSplit.Orientation = Orientation.Vertical;
            workspaceSplit.BackColor = Color.FromArgb(10, 10, 10);
            workspaceSplit.FixedPanel = FixedPanel.Panel1;
            workspaceSplit.IsSplitterFixed = false;
            workspaceSplit.SplitterWidth = 6;
            workspaceSplit.SplitterMoved += WorkspaceSplitSplitterMoved;

            editorTerminalSplit = new SplitContainer();
            editorTerminalSplit.Dock = DockStyle.Fill;
            editorTerminalSplit.Orientation = Orientation.Vertical;
            editorTerminalSplit.BackColor = Color.FromArgb(10, 10, 10);
            editorTerminalSplit.SplitterWidth = 6;
            editorTerminalSplit.SplitterMoved += EditorTerminalSplitSplitterMoved;

            workspaceSplit.Panel1.Controls.Add(BuildWorkspacePanel());
            workspaceSplit.Panel2.Controls.Add(editorTerminalSplit);
            editorTerminalSplit.Panel1.Controls.Add(BuildEditorPanel());
            editorTerminalSplit.Panel2.Controls.Add(BuildTerminalPanel());

            panel.Controls.Add(workspaceSplit);
            return panel;
        }

        private Control BuildWorkspacePanel()
        {
            var panel = CreateBoxPanel();
            panel.Padding = new Padding(14);

            var header = CreateHeaderLabel("WORKSPACE");
            header.Dock = DockStyle.Top;

            workspaceLabel = new Label();
            workspaceLabel.Text = workspacePath;
            workspaceLabel.AutoEllipsis = true;
            workspaceLabel.ForeColor = Color.FromArgb(186, 186, 186);
            workspaceLabel.Dock = DockStyle.Top;
            workspaceLabel.Height = 34;
            workspaceLabel.BackColor = Color.FromArgb(14, 14, 14);
            workspaceLabel.Padding = new Padding(10, 0, 10, 0);
            workspaceLabel.TextAlign = ContentAlignment.MiddleLeft;
            workspaceLabel.AllowDrop = true;
            workspaceLabel.DragEnter += WorkspaceNavDragEnter;
            workspaceLabel.DragLeave += WorkspaceNavDragLeave;
            workspaceLabel.DragDrop += WorkspaceLabelDragDrop;

            var searchLabel = new Label();
            searchLabel.Text = "Search";
            searchLabel.Dock = DockStyle.Top;
            searchLabel.Height = 20;
            searchLabel.ForeColor = Color.FromArgb(150, 150, 150);
            searchLabel.TextAlign = ContentAlignment.MiddleLeft;

            searchBox = CreateInputBox();
            searchBox.Dock = DockStyle.Top;
            searchBox.Height = 28;
            searchBox.Font = new Font("Segoe UI", 9.5F);
            searchBox.TextChanged += SearchBoxTextChanged;

            var navPanel = new Panel();
            navPanel.Dock = DockStyle.Top;
            navPanel.Height = 34;
            navPanel.BackColor = Color.Transparent;

            workspaceBackButton = CreateButton("Back", 68, Color.FromArgb(34, 34, 34));
            workspaceBackButton.Location = new Point(0, 2);
            workspaceBackButton.Click += WorkspaceBackButtonClick;

            workspaceUpButton = CreateButton("Up", 58, Color.FromArgb(34, 34, 34));
            workspaceUpButton.Location = new Point(74, 2);
            workspaceUpButton.Click += WorkspaceUpButtonClick;
            workspaceUpButton.AllowDrop = true;
            workspaceUpButton.DragEnter += WorkspaceNavDragEnter;
            workspaceUpButton.DragLeave += WorkspaceNavDragLeave;
            workspaceUpButton.DragDrop += WorkspaceUpButtonDragDrop;

            workspaceRootButton = CreateButton("Root", 64, Color.FromArgb(34, 34, 34));
            workspaceRootButton.Location = new Point(138, 2);
            workspaceRootButton.Click += WorkspaceRootButtonClick;
            workspaceRootButton.AllowDrop = true;
            workspaceRootButton.DragEnter += WorkspaceNavDragEnter;
            workspaceRootButton.DragLeave += WorkspaceNavDragLeave;
            workspaceRootButton.DragDrop += WorkspaceRootButtonDragDrop;

            workspaceRefreshButton = CreateButton("Refresh", 82, Color.FromArgb(34, 34, 34));
            workspaceRefreshButton.Location = new Point(208, 2);
            workspaceRefreshButton.Click += delegate { LoadTree(searchBox.Text.Trim(), GetSelectedWorkspacePath()); };

            navPanel.Controls.Add(workspaceBackButton);
            navPanel.Controls.Add(workspaceUpButton);
            navPanel.Controls.Add(workspaceRootButton);
            navPanel.Controls.Add(workspaceRefreshButton);

            workspaceHintLabel = new Label();
            workspaceHintLabel.Dock = DockStyle.Top;
            workspaceHintLabel.Height = 22;
            workspaceHintLabel.ForeColor = Color.FromArgb(134, 134, 134);
            workspaceHintLabel.Text = "Double-click folders to enter. Drag entries onto folders to move them.";
            workspaceHintLabel.TextAlign = ContentAlignment.MiddleLeft;

            var listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.Padding = new Padding(8);
            listHost.BackColor = Color.FromArgb(14, 14, 14);

            workspaceList = new WorkspaceListView();
            workspaceList.Dock = DockStyle.Fill;
            workspaceList.BackColor = Color.FromArgb(14, 14, 14);
            workspaceList.ForeColor = Color.FromArgb(228, 228, 228);
            workspaceList.BorderStyle = BorderStyle.FixedSingle;
            workspaceList.FullRowSelect = true;
            workspaceList.HideSelection = false;
            workspaceList.GridLines = false;
            workspaceList.MultiSelect = false;
            workspaceList.View = View.Details;
            workspaceList.HeaderStyle = ColumnHeaderStyle.None;
            workspaceList.OwnerDraw = true;
            workspaceList.AllowDrop = true;
            workspaceList.Columns.Add(string.Empty, 220);
            workspaceRowHeightImageList = new ImageList();
            workspaceRowHeightImageList.ImageSize = new Size(1, 34);
            workspaceList.SmallImageList = workspaceRowHeightImageList;
            workspaceList.ItemActivate += WorkspaceListItemActivate;
            workspaceList.MouseUp += WorkspaceListMouseUp;
            workspaceList.ItemDrag += WorkspaceListItemDrag;
            workspaceList.DragEnter += WorkspaceListDragEnter;
            workspaceList.DragOver += WorkspaceListDragOver;
            workspaceList.DragDrop += WorkspaceListDragDrop;
            workspaceList.DragLeave += WorkspaceListDragLeave;
            workspaceList.Resize += WorkspaceListResize;
            workspaceList.DrawColumnHeader += WorkspaceListDrawColumnHeader;
            workspaceList.DrawItem += WorkspaceListDrawItem;
            workspaceList.DrawSubItem += WorkspaceListDrawSubItem;
            listHost.Controls.Add(workspaceList);

            var footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 36;
            footer.BackColor = Color.Transparent;
            footer.FlowDirection = FlowDirection.LeftToRight;
            footer.WrapContents = false;
            footer.AutoSize = false;
            footer.Padding = new Padding(0);

            var fileButton = CreateButton("New File", 92, Color.FromArgb(34, 34, 34));
            fileButton.Margin = new Padding(4, 0, 4, 0);
            fileButton.Click += delegate { CreateItem(false, string.IsNullOrWhiteSpace(currentWorkspaceDirectory) ? workspacePath : currentWorkspaceDirectory); };

            var folderButton = CreateButton("New Folder", 100, Color.FromArgb(34, 34, 34));
            folderButton.Margin = new Padding(4, 0, 4, 0);
            folderButton.Click += delegate { CreateItem(true, string.IsNullOrWhiteSpace(currentWorkspaceDirectory) ? workspacePath : currentWorkspaceDirectory); };

            var refreshButton = CreateButton("Refresh", 88, Color.FromArgb(34, 34, 34));
            refreshButton.Margin = new Padding(4, 0, 4, 0);
            refreshButton.Click += delegate { LoadTree(searchBox.Text.Trim()); };

            footer.Controls.Add(fileButton);
            footer.Controls.Add(folderButton);
            footer.Controls.Add(refreshButton);

            panel.Resize += delegate
            {
                footer.Left = Math.Max(0, (panel.ClientSize.Width - footer.PreferredSize.Width) / 2);
            };

            var spacer4 = CreateSpacer(10);
            var spacer3 = CreateSpacer(10);
            var spacer2 = CreateSpacer(8);
            var spacer1 = CreateSpacer(8);
            var spacer0 = CreateSpacer(6);

            panel.Controls.Add(listHost);
            panel.Controls.Add(footer);
            panel.Controls.Add(spacer4);
            panel.Controls.Add(workspaceHintLabel);
            panel.Controls.Add(spacer3);
            panel.Controls.Add(navPanel);
            panel.Controls.Add(spacer2);
            panel.Controls.Add(searchBox);
            panel.Controls.Add(searchLabel);
            panel.Controls.Add(spacer0);
            panel.Controls.Add(workspaceLabel);
            panel.Controls.Add(spacer1);
            panel.Controls.Add(header);
            return panel;
        }

        private Control BuildEditorPanel()
        {
            var panel = CreateBoxPanel();
            panel.Padding = new Padding(12);

            var header = CreateHeaderLabel("EDITOR");
            header.Dock = DockStyle.Top;
            header.TextAlign = ContentAlignment.MiddleCenter;

            editorTabs = new TabControl();
            editorTabs.Dock = DockStyle.Fill;
            editorTabs.Appearance = TabAppearance.Normal;
            editorTabs.Multiline = false;
            editorTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            editorTabs.BackColor = Color.FromArgb(12, 12, 12);
            editorTabs.ForeColor = Color.FromArgb(236, 236, 236);
            editorTabs.ItemSize = new Size(180, 28);
            editorTabs.SizeMode = TabSizeMode.Fixed;
            editorTabs.DrawItem += EditorTabsDrawItem;
            editorTabs.SelectedIndexChanged += EditorTabsSelectedIndexChanged;
            editorTabs.MouseDown += EditorTabsMouseDown;
            editorTabs.Resize += EditorTabsResize;

            emptyEditorPanel = new Panel();
            emptyEditorPanel.Dock = DockStyle.Fill;
            emptyEditorPanel.BackColor = Color.FromArgb(12, 12, 12);

            var emptyEditorLabel = new Label();
            emptyEditorLabel.Dock = DockStyle.Fill;
            emptyEditorLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyEditorLabel.Text = "No file open";
            emptyEditorLabel.ForeColor = Color.FromArgb(100, 100, 100);
            emptyEditorLabel.Font = new Font("Segoe UI", 11F);
            emptyEditorPanel.Controls.Add(emptyEditorLabel);

            var spacer = new Panel();
            spacer.Dock = DockStyle.Top;
            spacer.Height = 8;

            panel.Controls.Add(emptyEditorPanel);
            panel.Controls.Add(editorTabs);
            panel.Controls.Add(spacer);
            panel.Controls.Add(header);
            return panel;
        }

        private Control BuildTerminalPanel()
        {
            var panel = CreateBoxPanel();
            panel.Padding = new Padding(12);

            var headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 30;
            headerPanel.BackColor = Color.Transparent;

            var header = CreateHeaderLabel("TERMINAL");
            header.Location = new Point(0, 4);
            header.TextAlign = ContentAlignment.MiddleLeft;
            headerPanel.Controls.Add(header);

            terminalBox = new RichTextBox();
            terminalBox.Dock = DockStyle.Fill;
            terminalBox.ReadOnly = false;
            terminalBox.BackColor = Color.FromArgb(12, 12, 12);
            terminalBox.ForeColor = Color.FromArgb(228, 228, 228);
            terminalBox.BorderStyle = BorderStyle.FixedSingle;
            terminalBox.Font = new Font(outputFontFamily, outputFontSize);
            terminalBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            terminalBox.WordWrap = true;
            terminalBox.TabStop = true;
            terminalBox.KeyDown += TerminalBoxKeyDown;
            terminalBox.MouseDown += TerminalBoxMouseDown;

            var spacer = new Panel();
            spacer.Dock = DockStyle.Top;
            spacer.Height = 8;

            panel.Controls.Add(terminalBox);
            panel.Controls.Add(spacer);
            panel.Controls.Add(headerPanel);
            return panel;
        }

        private Panel CreateBoxPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(20, 20, 20);
            panel.BorderStyle = BorderStyle.FixedSingle;
            return panel;
        }

        private Label CreateHeaderLabel(string text)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(245, 245, 245);
            label.AutoSize = true;
            return label;
        }

        private Control CreateSpacer(int height)
        {
            var spacer = new Panel();
            spacer.Dock = DockStyle.Top;
            spacer.Height = height;
            return spacer;
        }

        private TextBox CreateInputBox()
        {
            var box = new TextBox();
            box.BackColor = Color.FromArgb(14, 14, 14);
            box.ForeColor = Color.White;
            box.BorderStyle = BorderStyle.FixedSingle;
            return box;
        }

        private Button CreateButton(string text, int width, Color backColor)
        {
            var button = new Button();
            button.Text = text;
            button.Size = new Size(width, 30);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(62, 62, 62);
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            workspaceLabel.Text = workspacePath;
            AppendOutput("Workspace: " + workspacePath + Environment.NewLine);
        }

        private void MainFormShown(object sender, EventArgs e)
        {
            try
            {
                ApplySavedLayoutState();
                RestoreSession();
            }
            catch (Exception ex)
            {
                AppendOutput("[Startup restore failed]" + Environment.NewLine + ex + Environment.NewLine);
                if (workspaceSplit != null)
                {
                    workspaceVisible = true;
                    workspaceSplit.Panel1Collapsed = false;
                }
                UpdateWindowTitle();
            }
        }

        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmCloseAllDocuments())
            {
                e.Cancel = true;
                return;
            }

            StopRunningProcess();
            SaveConfig();
        }

        private void WorkspaceButtonClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = workspacePath;
                dialog.Description = "Choose C workspace";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    workspacePath = dialog.SelectedPath;
                    currentWorkspaceDirectory = workspacePath;
                    workspaceHistory.Clear();
                    workspaceHistoryIndex = -1;
                    workspaceLabel.Text = workspacePath;
                    EnsureWorkspaceExists();
                    SaveConfig();
                    LoadTree(searchBox.Text.Trim());
                    AppendOutput("Workspace changed to: " + workspacePath + Environment.NewLine);
                }
            }
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            SaveActiveDocument();
        }

        private void RecentFilesButtonClick(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            var existing = recentFiles
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Take(10)
                .ToList();

            if (existing.Count == 0)
            {
                menu.Items.Add("(No recent files)");
            }
            else
            {
                foreach (var path in existing)
                {
                    var item = new ToolStripMenuItem(Path.GetFileName(path) + "  [" + Path.GetDirectoryName(path) + "]");
                    item.Click += delegate { OpenFile(path); };
                    menu.Items.Add(item);
                }
            }

            menu.Show(recentFilesButton, new Point(0, recentFilesButton.Height));
        }

        private void SettingsButtonClick(object sender, EventArgs e)
        {
            var current = new EditorSettings
            {
                ThemeName = themeName,
                EditorFontFamily = editorFontFamily,
                EditorFontSize = editorFontSize,
                OutputFontFamily = outputFontFamily,
                OutputFontSize = outputFontSize
            };

            using (var dialog = new SettingsDialog(this, current, themes))
            {
                if (dialog.Result == null)
                {
                    return;
                }

                ApplyEditorSettings(dialog.Result);
                SaveConfig();
            }
        }

        private void AutoClearButtonClick(object sender, EventArgs e)
        {
            autoClearTerminal = !autoClearTerminal;
            UpdateAutoClearButton();
            SaveConfig();
        }

        private void MainFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                SaveActiveDocument();
            }
            else if (e.Control && e.KeyCode == Keys.B)
            {
                e.SuppressKeyPress = true;
                ToggleWorkspaceVisibility();
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                e.SuppressKeyPress = true;
                ReloadActiveDocument();
            }
        }

        private void MainFormResize(object sender, EventArgs e)
        {
            RefreshEditorChrome();
        }

        private void SearchBoxTextChanged(object sender, EventArgs e)
        {
            LoadTree(searchBox.Text.Trim(), GetSelectedWorkspacePath());
        }

        private void WorkspaceBackButtonClick(object sender, EventArgs e)
        {
            if (workspaceHistoryIndex > 0)
            {
                workspaceHistoryIndex--;
                suppressWorkspaceHistoryPush = true;
                try
                {
                    currentWorkspaceDirectory = workspaceHistory[workspaceHistoryIndex];
                    LoadTree(searchBox.Text.Trim());
                }
                finally
                {
                    suppressWorkspaceHistoryPush = false;
                }
            }
        }

        private void WorkspaceUpButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentWorkspaceDirectory))
            {
                return;
            }

            var parent = Directory.GetParent(currentWorkspaceDirectory);
            if (parent == null || !IsPathUnderRoot(parent.FullName, workspacePath))
            {
                return;
            }

            NavigateWorkspace(parent.FullName);
        }

        private void WorkspaceRootButtonClick(object sender, EventArgs e)
        {
            NavigateWorkspace(workspacePath);
        }

        private void WorkspaceListItemActivate(object sender, EventArgs e)
        {
            if (workspaceList == null || workspaceList.SelectedItems.Count == 0)
            {
                return;
            }

            var entry = workspaceList.SelectedItems[0].Tag as FileEntry;
            if (entry == null)
            {
                return;
            }

            if (entry.IsParentLink)
            {
                NavigateWorkspace(entry.FullPath);
            }
            else if (entry.IsDirectory)
            {
                NavigateWorkspace(entry.FullPath);
            }
            else
            {
                OpenFile(entry.FullPath);
            }
        }

        private void WorkspaceListMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || workspaceList == null)
            {
                return;
            }

            var item = workspaceList.GetItemAt(e.X, e.Y);
            if (item != null)
            {
                item.Selected = true;
            }

            var entry = item == null ? null : item.Tag as FileEntry;
            ShowWorkspaceContextMenu(entry, e.Location);
        }

        private void WorkspaceListResize(object sender, EventArgs e)
        {
            if (workspaceList == null || workspaceList.Columns.Count < 1)
            {
                return;
            }

            var width = Math.Max(180, workspaceList.ClientSize.Width - 4);
            workspaceList.Columns[0].Width = width;
        }

        private void WorkspaceListDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var brush = new SolidBrush(GetCurrentTheme().EditorBack))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
        }

        private void WorkspaceListDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            var entry = e.Item.Tag as FileEntry;
            var theme = GetCurrentTheme();
            var selected = e.Item.Selected;
            var highlightedDrop = entry != null &&
                !string.IsNullOrWhiteSpace(workspaceDropHighlightPath) &&
                string.Equals(entry.FullPath, workspaceDropHighlightPath, StringComparison.OrdinalIgnoreCase);
            var bounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 2, Math.Max(0, e.Bounds.Width - 8), Math.Max(0, e.Bounds.Height - 4));
            var backColor = highlightedDrop ? theme.TabActive : selected ? theme.Selection : theme.EditorBack;
            var accentColor = entry != null && (entry.IsDirectory || entry.IsParentLink) ? theme.Accent : theme.Grid;

            using (var backBrush = new SolidBrush(backColor))
            using (var accentBrush = new SolidBrush(accentColor))
            using (var borderPen = new Pen(highlightedDrop || selected ? theme.Accent : theme.Grid))
            {
                e.Graphics.FillRectangle(backBrush, bounds);
                e.Graphics.FillRectangle(accentBrush, new Rectangle(bounds.X, bounds.Y, 4, bounds.Height));
                e.Graphics.DrawRectangle(borderPen, bounds);
            }

            var title = entry == null
                ? e.Item.Text
                : entry.IsParentLink
                    ? "..  Go Up"
                    : (entry.IsDirectory ? Path.GetFileName(entry.FullPath) + Path.DirectorySeparatorChar : Path.GetFileName(entry.FullPath));
            using (var titleFont = new Font(Font, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, title, titleFont, new Rectangle(bounds.X + 14, bounds.Y + 6, Math.Max(0, bounds.Width - 22), 20), theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void WorkspaceListDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
        }

        private void WorkspaceListItemDrag(object sender, ItemDragEventArgs e)
        {
            if (searchBox != null && !string.IsNullOrWhiteSpace(searchBox.Text))
            {
                return;
            }

            var item = e.Item as ListViewItem;
            var entry = item == null ? null : item.Tag as FileEntry;
            if (entry == null || entry.IsParentLink || string.IsNullOrWhiteSpace(entry.FullPath))
            {
                return;
            }

            workspaceList.DoDragDrop(entry.FullPath, DragDropEffects.Move);
        }

        private void WorkspaceListDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.Text) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void WorkspaceListDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.None;
                UpdateWorkspaceDropHighlight(null);
                return;
            }

            string ignored;
            string highlightPath;
            e.Effect = ResolveWorkspaceDropTarget(e.X, e.Y, out ignored, out highlightPath) == null ? DragDropEffects.None : DragDropEffects.Move;
            UpdateWorkspaceDropHighlight(highlightPath);
        }

        private void WorkspaceListDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.Text))
            {
                UpdateWorkspaceDropHighlight(null);
                return;
            }

            var sourcePath = e.Data.GetData(DataFormats.Text) as string;
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                UpdateWorkspaceDropHighlight(null);
                return;
            }

            string targetName;
            string highlightPath;
            var targetDirectory = ResolveWorkspaceDropTarget(e.X, e.Y, out targetName, out highlightPath);
            UpdateWorkspaceDropHighlight(null);
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                return;
            }

            MoveWorkspaceEntry(sourcePath, targetDirectory, targetName);
        }

        private void WorkspaceListDragLeave(object sender, EventArgs e)
        {
            UpdateWorkspaceDropHighlight(null);
        }

        private void WorkspaceNavDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.Text) ? DragDropEffects.Move : DragDropEffects.None;
            UpdateWorkspaceDropHighlight(null);
            UpdateWorkspaceNavDropHighlight(sender as Control);
        }

        private void WorkspaceNavDragLeave(object sender, EventArgs e)
        {
            ClearWorkspaceNavDropHighlight();
        }

        private void WorkspaceUpButtonDragDrop(object sender, DragEventArgs e)
        {
            var sourcePath = e.Data.GetData(DataFormats.Text) as string;
            UpdateWorkspaceDropHighlight(null);
            ClearWorkspaceNavDropHighlight();
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(currentWorkspaceDirectory))
            {
                return;
            }

            var parent = Directory.GetParent(currentWorkspaceDirectory);
            if (parent != null && IsPathUnderRoot(parent.FullName, workspacePath))
            {
                MoveWorkspaceEntry(sourcePath, parent.FullName, null);
            }
        }

        private void WorkspaceRootButtonDragDrop(object sender, DragEventArgs e)
        {
            var sourcePath = e.Data.GetData(DataFormats.Text) as string;
            UpdateWorkspaceDropHighlight(null);
            ClearWorkspaceNavDropHighlight();
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                MoveWorkspaceEntry(sourcePath, workspacePath, null);
            }
        }

        private void WorkspaceLabelDragDrop(object sender, DragEventArgs e)
        {
            var sourcePath = e.Data.GetData(DataFormats.Text) as string;
            UpdateWorkspaceDropHighlight(null);
            ClearWorkspaceNavDropHighlight();
            if (!string.IsNullOrWhiteSpace(sourcePath) && !string.IsNullOrWhiteSpace(currentWorkspaceDirectory))
            {
                MoveWorkspaceEntry(sourcePath, currentWorkspaceDirectory, null);
            }
        }

        private void EditorTabsDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= editorTabs.TabPages.Count)
            {
                return;
            }

            var page = editorTabs.TabPages[e.Index];
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var theme = GetCurrentTheme();
            using (var back = new SolidBrush(selected ? theme.TabActive : theme.TabInactive))
            using (var text = new SolidBrush(selected ? theme.Text : theme.MutedText))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
                using (var borderPen = new Pen(theme.Grid))
                {
                    e.Graphics.DrawRectangle(borderPen, e.Bounds);
                }
                var textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height);
                var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(e.Graphics, page.Text, Font, textBounds, ((SolidBrush)text).Color, flags);
                var closeBounds = new Rectangle(e.Bounds.Right - 18, e.Bounds.Top + 7, 10, 10);
                using (var closePen = new Pen(theme.MutedText))
                {
                    e.Graphics.DrawLine(closePen, closeBounds.Left, closeBounds.Top, closeBounds.Right, closeBounds.Bottom);
                    e.Graphics.DrawLine(closePen, closeBounds.Right, closeBounds.Top, closeBounds.Left, closeBounds.Bottom);
                }
            }
        }

        private void EditorTabsSelectedIndexChanged(object sender, EventArgs e)
        {
            var active = GetActiveDocument();
            AutoRefreshTextDocument(active);
            if (active != null && !string.IsNullOrWhiteSpace(active.Path))
            {
                LoadTree(searchBox == null ? string.Empty : searchBox.Text.Trim(), active.Path);
            }
            UpdateWindowTitle();
            UpdateCaretPosition(active == null ? null : active.Editor);
            SaveConfig();
        }

        private void EditorTabsMouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < editorTabs.TabPages.Count; i++)
            {
                var bounds = editorTabs.GetTabRect(i);
                var closeBounds = new Rectangle(bounds.Right - 18, bounds.Top + 7, 10, 10);
                if (closeBounds.Contains(e.Location))
                {
                    CloseDocumentTab(editorTabs.TabPages[i]);
                    return;
                }
            }
        }

        private void TerminalBoxMouseDown(object sender, MouseEventArgs e)
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                BeginInvoke(new Action(MoveTerminalCaretToEnd));
            }
        }

        private void TerminalBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (currentProcess == null || currentProcess.HasExited)
            {
                if (!e.Control)
                {
                    e.SuppressKeyPress = true;
                }
                return;
            }

            MoveTerminalCaretToEnd();

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendTerminalInput();
                return;
            }

            if (e.KeyCode == Keys.Back)
            {
                if (terminalBox.SelectionStart <= terminalInputStart)
                {
                    e.SuppressKeyPress = true;
                }
                return;
            }

            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Left || e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.Home)
            {
                e.SuppressKeyPress = true;
                return;
            }
        }

        private void SendTerminalInput()
        {
            if (currentProcess == null || currentProcess.HasExited)
            {
                return;
            }

            try
            {
                var input = terminalBox.Text.Substring(terminalInputStart);
                terminalBox.AppendText(Environment.NewLine);
                terminalInputStart = terminalBox.TextLength;
                currentProcess.StandardInput.WriteLine(input);
                currentProcess.StandardInput.Flush();
            }
            catch
            {
            }
        }

        private void MoveTerminalCaretToEnd()
        {
            terminalBox.SelectionStart = terminalBox.TextLength;
            terminalBox.SelectionLength = 0;
            terminalBox.ScrollToCaret();
            terminalBox.Focus();
        }

        private void AutoSaveTimerTick(object sender, EventArgs e)
        {
            autoSaveTimer.Stop();
            SaveDirtyDocuments();
        }

        private void RunButtonClick(object sender, EventArgs e)
        {
            CompileAndRun();
        }

        private void StopButtonClick(object sender, EventArgs e)
        {
            StopRunningProcess();
        }

        private bool IsExecutionActive()
        {
            return (currentProcess != null && !currentProcess.HasExited) || (compileProcess != null && !compileProcess.HasExited);
        }

        private void AutoRefreshTextDocument(EditorDocument document)
        {
            if (document == null || string.IsNullOrWhiteSpace(document.Path))
            {
                return;
            }

            if (!string.Equals(Path.GetExtension(document.Path), ".txt", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!File.Exists(document.Path))
            {
                return;
            }

            try
            {
                suppressDocumentDirtyTracking = true;
                suppressAutocompleteUpdates = true;
                var selectionStart = document.Editor.SelectionStart;
                document.Editor.Text = File.ReadAllText(document.Path);
                document.Editor.SelectionStart = Math.Max(0, Math.Min(selectionStart, (document.Editor.Text ?? string.Empty).Length));
                document.IsDirty = false;
                document.Page.Text = document.BaseTitle;
            }
            catch
            {
            }
            finally
            {
                suppressAutocompleteUpdates = false;
                suppressDocumentDirtyTracking = false;
            }
        }

        private void OpenFile(string path)
        {
            EditorDocument existing;
            if (openDocuments.TryGetValue(path, out existing))
            {
                editorTabs.SelectedTab = existing.Page;
                AutoRefreshTextDocument(existing);
                return;
            }

            CreateDocumentTab(path, Path.GetFileName(path), File.ReadAllText(path));
        }

        private void CreateDocumentTab(string path, string title, string content)
        {
            var page = new TabPage();
            page.Text = title;
            page.BackColor = Color.FromArgb(12, 12, 12);
            page.ForeColor = Color.FromArgb(236, 236, 236);
            page.Padding = new Padding(0);
            page.Margin = new Padding(0);
            page.UseVisualStyleBackColor = false;

            var editorHost = new Panel();
            editorHost.Dock = DockStyle.Fill;
            editorHost.BackColor = Color.FromArgb(12, 12, 12);
            editorHost.Padding = new Padding(0);
            editorHost.Margin = new Padding(0);

            var editor = new FastColoredTextBox();
            editor.AcceptsTab = true;
            editor.AcceptsReturn = true;
            editor.WordWrap = true;
            editor.WordWrapMode = WordWrapMode.WordWrapControlWidth;
            editor.Dock = DockStyle.Fill;
            editor.Margin = new Padding(0);
            editor.Paddings = new Padding(0);
            editor.Font = new Font(editorFontFamily, editorFontSize);
            editor.BackColor = Color.FromArgb(12, 12, 12);
            editor.PaddingBackColor = Color.FromArgb(12, 12, 12);
            editor.ForeColor = Color.FromArgb(236, 236, 236);
            editor.BorderStyle = BorderStyle.None;
            editor.ShowLineNumbers = true;
            editor.AutoIndent = true;
            editor.Language = GetEditorLanguage(path);
            editor.ShowScrollBars = true;
            editor.LeftBracket = '(';
            editor.RightBracket = ')';
            editor.LeftBracket2 = '{';
            editor.RightBracket2 = '}';
            editor.BracketsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(60, 80, 120)));
            editor.ServiceLinesColor = Color.FromArgb(36, 36, 36);
            editor.IndentBackColor = Color.FromArgb(16, 16, 16);
            editor.LineNumberColor = Color.FromArgb(120, 120, 120);
            editor.CaretColor = Color.FromArgb(236, 236, 236);
            editor.DisabledColor = Color.FromArgb(100, 100, 100, 60);
            editor.SelectionColor = Color.FromArgb(60, 90, 140, 200);
            editor.CurrentLineColor = Color.FromArgb(24, 24, 24);
            editor.Text = content ?? string.Empty;
            editor.KeyDown += EditorKeyDown;
            editor.TextChanged += EditorTextChanged;
            editor.SelectionChanged += EditorSelectionChanged;
            editor.Resize += EditorResize;

            editorHost.Controls.Add(editor);
            page.Controls.Add(editorHost);
            editorTabs.TabPages.Add(page);
            editorTabs.SelectedTab = page;

            var document = new EditorDocument();
            document.Path = path;
            document.Page = page;
            document.Editor = editor;
            document.BaseTitle = title;
            document.IsDirty = false;
            page.Tag = document;

            if (!string.IsNullOrWhiteSpace(path))
            {
                openDocuments[path] = document;
                PushRecentFile(path);
            }

            ApplyThemeToEditor(editor);
            UpdateEditorSurfaceState();
            UpdateWindowTitle();
            RefreshEditorChrome();
            SaveConfig();
        }

        private void EditorSelectionChanged(object sender, EventArgs e)
        {
            var editor = sender as FastColoredTextBox;
            UpdateCaretPosition(editor);
            if (!suppressAutocompleteUpdates)
            {
                UpdateAutocompleteForEditor(editor, false);
            }
        }

        private Language GetEditorLanguage(string path)
        {
            var extension = string.IsNullOrWhiteSpace(path) ? ".c" : Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".txt")
            {
                return Language.Custom;
            }

            return Language.CSharp;
        }

        private void EditorKeyDown(object sender, KeyEventArgs e)
        {
            var editor = sender as FastColoredTextBox;
            if (editor == null)
            {
                return;
            }

            if (e.Control && e.KeyCode == Keys.S)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SaveActiveDocument();
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                editor.Undo();
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                editor.Redo();
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (editorTabs.SelectedTab != null)
                {
                    CloseDocumentTab(editorTabs.SelectedTab);
                }
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ReloadActiveDocument();
            }
            else if (e.Control && e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                UpdateAutocompleteForEditor(editor, true);
            }
            else if (autocompletePopup != null && autocompletePopup.Visible)
            {
                if (e.KeyCode == Keys.Down)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    MoveAutocompleteSelection(1);
                }
                else if (e.KeyCode == Keys.Up)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    MoveAutocompleteSelection(-1);
                }
                else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    CommitAutocompleteSelection();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    HideAutocomplete();
                }
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CompileAndRun();
            }
        }

        private void EditorTextChanged(object sender, TextChangedEventArgs e)
        {
            var document = GetDocumentByEditor(sender as FastColoredTextBox);
            if (document == null)
            {
                return;
            }

            if (suppressDocumentDirtyTracking)
            {
                return;
            }

            if (!document.IsDirty)
            {
                document.IsDirty = true;
                document.Page.Text = document.BaseTitle + " *";
                UpdateWindowTitle();
            }

            autoSaveTimer.Stop();
            autoSaveTimer.Start();
            if (!suppressAutocompleteUpdates)
            {
                UpdateAutocompleteForEditor(sender as FastColoredTextBox, false);
            }
        }

        private EditorDocument GetActiveDocument()
        {
            if (editorTabs.SelectedTab == null)
            {
                return null;
            }

            return editorTabs.SelectedTab.Tag as EditorDocument;
        }

        private void CloseDocumentTab(TabPage page)
        {
            if (page == null)
            {
                return;
            }

            var document = page.Tag as EditorDocument;
            if (document != null && document.IsDirty)
            {
                editorTabs.SelectedTab = page;
                var result = MessageBox.Show(this, "Save changes to " + document.BaseTitle + "?", "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                {
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    SaveActiveDocument();
                    document = page.Tag as EditorDocument;
                }
            }

            if (document != null && !string.IsNullOrWhiteSpace(document.Path))
            {
                openDocuments.Remove(document.Path);
            }

            if (autocompleteEditor != null && document != null && ReferenceEquals(autocompleteEditor, document.Editor))
            {
                HideAutocomplete();
            }

            editorTabs.TabPages.Remove(page);
            page.Dispose();

            UpdateEditorSurfaceState();
            UpdateWindowTitle();
            RefreshEditorChrome();
            SaveConfig();
        }

        private void SaveActiveDocument()
        {
            var document = GetActiveDocument();
            if (document == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(document.Path))
            {
                var savePath = PromptDialog.Show(this, "Save File", "Enter file name:", document.BaseTitle);
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    return;
                }

                document.Path = Path.Combine(workspacePath, savePath);
                document.BaseTitle = Path.GetFileName(document.Path);
                openDocuments[document.Path] = document;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(document.Path));
            File.WriteAllText(document.Path, document.Editor.Text);
            document.IsDirty = false;
            document.Page.Text = document.BaseTitle;
            PushRecentFile(document.Path);
            LoadTree(searchBox.Text.Trim(), document.Path);
            SetStatus("SAVED");
            UpdateWindowTitle();
            SaveConfig();
        }

        private void SaveDirtyDocuments()
        {
            var dirtyDocuments = new List<EditorDocument>();
            foreach (TabPage page in editorTabs.TabPages)
            {
                var document = page.Tag as EditorDocument;
                if (document != null && document.IsDirty && !string.IsNullOrWhiteSpace(document.Path))
                {
                    dirtyDocuments.Add(document);
                }
            }

            foreach (var document in dirtyDocuments)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(document.Path));
                    File.WriteAllText(document.Path, document.Editor.Text);
                    document.IsDirty = false;
                    document.Page.Text = document.BaseTitle;
                    PushRecentFile(document.Path);
                }
                catch
                {
                }
            }

            if (dirtyDocuments.Count > 0)
            {
                var active = GetActiveDocument();
                LoadTree(searchBox.Text.Trim(), active == null ? null : active.Path);
                SetStatus("SAVED");
                UpdateWindowTitle();
                SaveConfig();
            }
        }

        private bool ConfirmCloseAllDocuments()
        {
            foreach (TabPage page in editorTabs.TabPages)
            {
                var document = page.Tag as EditorDocument;
                if (document == null || !document.IsDirty)
                {
                    continue;
                }

                editorTabs.SelectedTab = page;
                var result = MessageBox.Show(this, "Save changes to " + document.BaseTitle + "?", "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                {
                    return false;
                }

                if (result == DialogResult.Yes)
                {
                    SaveActiveDocument();
                }
            }

            return true;
        }

        private void CompileAndRun()
        {
            if (IsExecutionActive())
            {
                restartExecutionRequested = true;
                StopRunningProcess();
                return;
            }

            var request = BuildCompileRequest();
            if (request == null)
            {
                return;
            }

            var gccPath = @"C:\MinGW\bin\gcc.exe";
            if (!File.Exists(gccPath))
            {
                AppendOutput("[gcc.exe not found at C:\\MinGW\\bin\\gcc.exe]" + Environment.NewLine);
                RestoreOriginalFileIfNeeded(request.Document, request.TempSourcePath, request.OriginalText);
                return;
            }

            try
            {
                if (File.Exists(request.ExePath))
                {
                    File.Delete(request.ExePath);
                }
            }
            catch
            {
            }

            if (autoClearTerminal)
            {
                terminalBox.Clear();
                terminalInputStart = 0;
            }

            restartExecutionRequested = false;
            stopRequested = false;
            activeCompileRequest = request;
            SetStatus("COMPILING");
            AppendOutput("Compiling " + Path.GetFileName(request.SourcePath) + Environment.NewLine);
            StartCompileProcess(gccPath, request);
        }

        private CompileRequest BuildCompileRequest()
        {
            var document = GetActiveDocument();
            if (document == null)
            {
                return null;
            }

            var sourcePath = document.Path;
            var originalText = document.Editor.Text;
            string tempSourcePath = null;
            string exePath;

            if (string.IsNullOrWhiteSpace(sourcePath) || !sourcePath.EndsWith(".c", StringComparison.OrdinalIgnoreCase))
            {
                tempSourcePath = Path.Combine(workspacePath, "_run_temp.c");
                File.WriteAllText(tempSourcePath, InjectBufferedOutputFix(originalText));
                sourcePath = tempSourcePath;
                exePath = Path.Combine(workspacePath, "_run_temp.exe");
            }
            else
            {
                SaveActiveDocument();
                exePath = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + ".exe");
                File.WriteAllText(sourcePath, InjectBufferedOutputFix(originalText));
            }

            return new CompileRequest
            {
                Document = document,
                SourcePath = sourcePath,
                TempSourcePath = tempSourcePath,
                OriginalText = originalText,
                ExePath = exePath,
                WorkingDirectory = Path.GetDirectoryName(sourcePath)
            };
        }

        private void StartCompileProcess(string gccPath, CompileRequest request)
        {
            compileProcess = new Process();
            compileProcess.StartInfo = new ProcessStartInfo
            {
                FileName = gccPath,
                Arguments = "\"" + request.SourcePath + "\" -o \"" + request.ExePath + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = request.WorkingDirectory
            };
            compileProcess.EnableRaisingEvents = true;
            compileProcess.Exited += CompileProcessExited;
            compileProcess.Start();
            StartStreamPump(compileProcess.StandardOutput, false);
            StartStreamPump(compileProcess.StandardError, true);
            UpdateStopButtonState();
        }

        private void StartExecutable(string exePath, string workingDirectory)
        {
            currentProcess = new Process();
            currentProcess.StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            currentProcess.EnableRaisingEvents = true;
            currentProcess.Exited += ProcessExited;
            currentProcess.Start();
            terminalInputStart = terminalBox.TextLength;
            MoveTerminalCaretToEnd();
            StartStreamPump(currentProcess.StandardOutput, false);
            StartStreamPump(currentProcess.StandardError, true);
            UpdateStopButtonState();
        }

        private void CompileProcessExited(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, EventArgs>(CompileProcessExited), sender, e);
                return;
            }

            var request = activeCompileRequest;
            var exitedProcess = compileProcess;
            var exitCode = exitedProcess == null ? -1 : exitedProcess.ExitCode;

            if (request != null)
            {
                RestoreOriginalFileIfNeeded(request.Document, request.TempSourcePath, request.OriginalText);
            }

            if (exitedProcess != null)
            {
                exitedProcess.Dispose();
            }

            compileProcess = null;
            activeCompileRequest = null;
            UpdateStopButtonState();

            if (restartExecutionRequested)
            {
                restartExecutionRequested = false;
                stopRequested = false;
                CompileAndRun();
                return;
            }

            if (stopRequested)
            {
                stopRequested = false;
                AppendOutput(Environment.NewLine + "[Compilation stopped]" + Environment.NewLine);
                SetStatus("READY");
                return;
            }

            if (request == null)
            {
                SetStatus("COMPILE FAILED");
                return;
            }

            if (exitCode != 0 || !File.Exists(request.ExePath))
            {
                JumpToFirstCompilerError(terminalBox.Text, request.SourcePath);
                SetStatus("COMPILE FAILED");
                return;
            }

            SetStatus("RUNNING");
            AppendOutput("Running..." + Environment.NewLine + "------------------------" + Environment.NewLine);
            StartExecutable(request.ExePath, Path.GetDirectoryName(request.ExePath));
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, EventArgs>(ProcessExited), sender, e);
                return;
            }

            var code = currentProcess.ExitCode;
            AppendOutput(Environment.NewLine + "[Process exited with code " + code + "]" + Environment.NewLine);
            currentProcess.Dispose();
            currentProcess = null;
            UpdateStopButtonState();

            if (restartExecutionRequested)
            {
                restartExecutionRequested = false;
                stopRequested = false;
                CompileAndRun();
                return;
            }

            SetStatus("READY");
        }

        private void StopRunningProcess()
        {
            stopRequested = true;

            if (compileProcess != null && !compileProcess.HasExited)
            {
                try
                {
                    compileProcess.Kill();
                }
                catch
                {
                }
                return;
            }

            if (currentProcess == null || currentProcess.HasExited)
            {
                stopRequested = false;
                return;
            }

            try
            {
                currentProcess.Kill();
                AppendOutput(Environment.NewLine + "[Process stopped]" + Environment.NewLine);
            }
            catch
            {
            }
        }

        private void StartStreamPump(StreamReader reader, bool isError)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                var buffer = new char[128];
                try
                {
                    while (reader != null)
                    {
                        var read = reader.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                        {
                            break;
                        }

                        var text = new string(buffer, 0, read);
                        if (isError)
                        {
                            AppendOutput(text);
                        }
                        else
                        {
                            AppendOutput(text);
                        }
                    }
                }
                catch
                {
                }
            });
        }

        private void LoadTree(string query)
        {
            LoadTree(query, null);
        }

        private void LoadTree(string query, string focusPath)
        {
            if (workspaceList == null)
            {
                return;
            }

            EnsureWorkspaceExists();

            if (!string.IsNullOrWhiteSpace(focusPath))
            {
                var focusDirectory = Directory.Exists(focusPath) ? focusPath : Path.GetDirectoryName(focusPath);
                if (!string.IsNullOrWhiteSpace(focusDirectory) && IsPathUnderRoot(focusDirectory, workspacePath))
                {
                    currentWorkspaceDirectory = focusDirectory;
                }
            }

            if (string.IsNullOrWhiteSpace(currentWorkspaceDirectory) || !Directory.Exists(currentWorkspaceDirectory) || !IsPathUnderRoot(currentWorkspaceDirectory, workspacePath))
            {
                currentWorkspaceDirectory = workspacePath;
            }

            if (!suppressWorkspaceHistoryPush && string.IsNullOrWhiteSpace(query))
            {
                PushWorkspaceHistory(currentWorkspaceDirectory);
            }

            workspaceList.BeginUpdate();
            workspaceList.Items.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                var parent = Directory.GetParent(currentWorkspaceDirectory);
                if (parent != null && IsPathUnderRoot(parent.FullName, workspacePath))
                {
                    workspaceList.Items.Add(CreateWorkspaceItem(new FileEntry
                    {
                        FullPath = parent.FullName,
                        IsDirectory = true,
                        IsParentLink = true
                    }, currentWorkspaceDirectory));
                }

                foreach (var entry in GetWorkspaceEntriesForDirectory(currentWorkspaceDirectory))
                {
                    workspaceList.Items.Add(CreateWorkspaceItem(entry, currentWorkspaceDirectory));
                }
            }
            else
            {
                foreach (var entry in SearchWorkspaceEntries(query))
                {
                    workspaceList.Items.Add(CreateWorkspaceItem(entry, workspacePath));
                }
            }

            workspaceList.EndUpdate();
            SelectWorkspaceItemByPath(focusPath);
            UpdateWorkspaceHeader(query);
            UpdateWorkspaceNavigationState();
        }

        private bool MatchesSearch(string path, string query, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var lower = query.ToLowerInvariant();
            if (Path.GetFileName(path).ToLowerInvariant().Contains(lower))
            {
                return true;
            }

            if (isDirectory)
            {
                return false;
            }

            try
            {
                return File.ReadAllText(path).ToLowerInvariant().Contains(lower);
            }
            catch
            {
                return false;
            }
        }

        private IEnumerable<FileEntry> GetWorkspaceEntriesForDirectory(string directory)
        {
            IEnumerable<string> directories = Enumerable.Empty<string>();
            IEnumerable<string> files = Enumerable.Empty<string>();

            try
            {
                directories = Directory.GetDirectories(directory)
                    .Where(item => !string.Equals(Path.GetFileName(item), "node_modules", StringComparison.OrdinalIgnoreCase))
                    .Where(item => !string.Equals(Path.GetFileName(item), ".git", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(item => item);

                files = Directory.GetFiles(directory)
                    .Where(item => allowedExtensions.Contains(Path.GetExtension(item).ToLowerInvariant()))
                    .OrderBy(item => FilePriority(item))
                    .ThenBy(item => item);
            }
            catch
            {
            }

            foreach (var item in directories)
            {
                yield return new FileEntry { FullPath = item, IsDirectory = true };
            }

            foreach (var item in files)
            {
                yield return new FileEntry { FullPath = item, IsDirectory = false };
            }
        }

        private IEnumerable<FileEntry> SearchWorkspaceEntries(string query)
        {
            var results = new List<FileEntry>();
            var pending = new Stack<string>();
            pending.Push(workspacePath);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                IEnumerable<string> directories = Enumerable.Empty<string>();
                IEnumerable<string> files = Enumerable.Empty<string>();

                try
                {
                    directories = Directory.GetDirectories(current)
                        .Where(item => !string.Equals(Path.GetFileName(item), "node_modules", StringComparison.OrdinalIgnoreCase))
                        .Where(item => !string.Equals(Path.GetFileName(item), ".git", StringComparison.OrdinalIgnoreCase));
                    files = Directory.GetFiles(current)
                        .Where(item => allowedExtensions.Contains(Path.GetExtension(item).ToLowerInvariant()));
                }
                catch
                {
                }

                foreach (var directory in directories.OrderBy(item => item))
                {
                    pending.Push(directory);
                    if (MatchesSearch(directory, query, true))
                    {
                        results.Add(new FileEntry { FullPath = directory, IsDirectory = true });
                    }
                }

                foreach (var file in files.OrderBy(item => FilePriority(item)).ThenBy(item => item))
                {
                    if (MatchesSearch(file, query, false))
                    {
                        results.Add(new FileEntry { FullPath = file, IsDirectory = false });
                    }
                }
            }

            return results;
        }

        private ListViewItem CreateWorkspaceItem(FileEntry entry, string relativeBasePath)
        {
            var item = new ListViewItem(entry.IsDirectory ? Path.GetFileName(entry.FullPath) + Path.DirectorySeparatorChar : Path.GetFileName(entry.FullPath));
            item.Tag = entry;
            return item;
        }

        private void SelectWorkspaceItemByPath(string focusPath)
        {
            if (workspaceList == null || string.IsNullOrWhiteSpace(focusPath))
            {
                return;
            }

            foreach (ListViewItem item in workspaceList.Items)
            {
                var entry = item.Tag as FileEntry;
                if (entry != null && string.Equals(entry.FullPath, focusPath, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    return;
                }
            }
        }

        private void UpdateWorkspaceHeader(string query)
        {
            if (workspaceLabel != null)
            {
                var relative = MakeRelativePath(workspacePath, currentWorkspaceDirectory);
                var locationText = string.Equals(relative, ".", StringComparison.OrdinalIgnoreCase)
                    ? workspacePath
                    : workspacePath + "  >  " + relative.Replace(Path.DirectorySeparatorChar.ToString(), " > ");
                workspaceLabel.Text = string.IsNullOrWhiteSpace(query) ? locationText : "Search in " + workspacePath;
            }

            if (workspaceHintLabel != null)
            {
                workspaceHintLabel.Text = string.IsNullOrWhiteSpace(query)
                    ? "Double-click folders to enter. Drag entries onto folders to move them."
                    : "Search is showing workspace-wide matches. Clear search to drag or navigate normally.";
            }
        }

        private int FilePriority(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".c")
            {
                return 0;
            }
            if (ext == ".h")
            {
                return 1;
            }
            return 2;
        }

        private void CreateItem(bool isFolder, string parentDirectory)
        {
            var name = PromptDialog.Show(this, isFolder ? "New Folder" : "New File", "Enter name:", "");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var targetPath = Path.Combine(parentDirectory, name);
            try
            {
                if (isFolder)
                {
                    Directory.CreateDirectory(targetPath);
                    NavigateWorkspace(targetPath);
                }
                else
                {
                    File.WriteAllText(targetPath, string.Empty);
                    OpenFile(targetPath);
                }

                LoadTree(searchBox.Text.Trim(), targetPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Create failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenameItem(string path)
        {
            var isDirectory = Directory.Exists(path);
            var currentName = Path.GetFileName(path);
            var newName = PromptDialog.Show(this, "Rename", "Enter new name:", currentName);
            if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName, currentName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var newPath = Path.Combine(Path.GetDirectoryName(path), newName);
            try
            {
                if (isDirectory)
                {
                    Directory.Move(path, newPath);
                }
                else
                {
                    File.Move(path, newPath);
                }

                UpdateTrackedPathsAfterMove(path, newPath, isDirectory);
                LoadTree(searchBox.Text.Trim(), newPath);
                UpdateWindowTitle();
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Rename failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteItem(string path)
        {
            var isDirectory = Directory.Exists(path);
            var prompt = isDirectory
                ? "Delete folder?\r\n\r\n" + path + "\r\n\r\nThis will remove everything inside it."
                : "Delete file?\r\n\r\n" + path;
            if (MessageBox.Show(this, prompt, "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                if (isDirectory)
                {
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }

                RemoveTrackedPathsForDelete(path, isDirectory);

                if (isDirectory && IsPathUnderRoot(currentWorkspaceDirectory, path))
                {
                    currentWorkspaceDirectory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(currentWorkspaceDirectory) || !IsPathUnderRoot(currentWorkspaceDirectory, workspacePath))
                    {
                        currentWorkspaceDirectory = workspacePath;
                    }
                }

                LoadTree(searchBox.Text.Trim());
                UpdateWindowTitle();
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowWorkspaceContextMenu(FileEntry entry, Point location)
        {
            var targetPath = entry == null ? currentWorkspaceDirectory : entry.FullPath;
            var targetDirectory = entry == null
                ? currentWorkspaceDirectory
                : entry.IsDirectory ? entry.FullPath : Path.GetDirectoryName(entry.FullPath);
            var menu = new ContextMenuStrip();

            if (entry != null && !entry.IsDirectory)
            {
                menu.Items.Add("Open", null, delegate { OpenFile(entry.FullPath); });
            }

            if (entry != null)
            {
                menu.Items.Add("Reveal in Explorer", null, delegate { RevealInExplorer(entry.FullPath, entry.IsDirectory); });
            }

            menu.Items.Add("New File", null, delegate { CreateItem(false, targetDirectory); });
            menu.Items.Add("New Folder", null, delegate { CreateItem(true, targetDirectory); });

            if (entry != null)
            {
                menu.Items.Add("Rename", null, delegate { RenameItem(entry.FullPath); });
                menu.Items.Add("Delete", null, delegate { DeleteItem(entry.FullPath); });
            }

            menu.Show(workspaceList, location);
        }

        private void NavigateWorkspace(string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory) || !Directory.Exists(targetDirectory))
            {
                return;
            }

            if (!IsPathUnderRoot(targetDirectory, workspacePath))
            {
                return;
            }

            currentWorkspaceDirectory = targetDirectory;
            LoadTree(searchBox == null ? string.Empty : searchBox.Text.Trim());
        }

        private void PushWorkspaceHistory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (workspaceHistoryIndex >= 0 && workspaceHistoryIndex < workspaceHistory.Count &&
                string.Equals(workspaceHistory[workspaceHistoryIndex], path, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (workspaceHistoryIndex < workspaceHistory.Count - 1)
            {
                workspaceHistory.RemoveRange(workspaceHistoryIndex + 1, workspaceHistory.Count - workspaceHistoryIndex - 1);
            }

            workspaceHistory.Add(path);
            workspaceHistoryIndex = workspaceHistory.Count - 1;
        }

        private void UpdateWorkspaceNavigationState()
        {
            if (workspaceBackButton != null)
            {
                workspaceBackButton.Enabled = workspaceHistoryIndex > 0;
            }

            if (workspaceUpButton != null)
            {
                workspaceUpButton.Enabled = !string.IsNullOrWhiteSpace(currentWorkspaceDirectory) &&
                    !string.Equals(Path.GetFullPath(currentWorkspaceDirectory).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(workspacePath).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
            }

            if (workspaceRootButton != null)
            {
                workspaceRootButton.Enabled = Directory.Exists(workspacePath);
            }

            if (workspaceRefreshButton != null)
            {
                workspaceRefreshButton.Enabled = true;
            }
        }

        private void UpdateWorkspaceNavDropHighlight(Control target)
        {
            var nextUp = ReferenceEquals(target, workspaceUpButton);
            var nextRoot = ReferenceEquals(target, workspaceRootButton);
            var nextLabel = ReferenceEquals(target, workspaceLabel);
            if (workspaceUpDropHighlighted == nextUp &&
                workspaceRootDropHighlighted == nextRoot &&
                workspaceLabelDropHighlighted == nextLabel)
            {
                return;
            }

            workspaceUpDropHighlighted = nextUp;
            workspaceRootDropHighlighted = nextRoot;
            workspaceLabelDropHighlighted = nextLabel;
            ApplyWorkspaceNavDropHighlightVisuals();
        }

        private void ClearWorkspaceNavDropHighlight()
        {
            if (!workspaceUpDropHighlighted && !workspaceRootDropHighlighted && !workspaceLabelDropHighlighted)
            {
                return;
            }

            workspaceUpDropHighlighted = false;
            workspaceRootDropHighlighted = false;
            workspaceLabelDropHighlighted = false;
            ApplyWorkspaceNavDropHighlightVisuals();
        }

        private void ApplyWorkspaceNavDropHighlightVisuals()
        {
            var theme = GetCurrentTheme();

            if (workspaceUpButton != null)
            {
                workspaceUpButton.BackColor = workspaceUpDropHighlighted ? theme.Accent : theme.SurfaceBack;
                workspaceUpButton.ForeColor = workspaceUpDropHighlighted ? theme.WindowBack : theme.Text;
                workspaceUpButton.FlatAppearance.BorderColor = workspaceUpDropHighlighted ? theme.Accent : theme.Grid;
            }

            if (workspaceRootButton != null)
            {
                workspaceRootButton.BackColor = workspaceRootDropHighlighted ? theme.Accent : theme.SurfaceBack;
                workspaceRootButton.ForeColor = workspaceRootDropHighlighted ? theme.WindowBack : theme.Text;
                workspaceRootButton.FlatAppearance.BorderColor = workspaceRootDropHighlighted ? theme.Accent : theme.Grid;
            }

            if (workspaceLabel != null)
            {
                workspaceLabel.BackColor = workspaceLabelDropHighlighted ? theme.TabActive : theme.SurfaceBack;
                workspaceLabel.ForeColor = theme.Text;
            }
        }

        private void UpdateWorkspaceDropHighlight(string targetPath)
        {
            if (string.Equals(workspaceDropHighlightPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            workspaceDropHighlightPath = targetPath;
            if (workspaceList != null)
            {
                workspaceList.InvalidateHighlightRows();
            }
        }

        private string GetSelectedWorkspacePath()
        {
            if (workspaceList == null || workspaceList.SelectedItems.Count == 0)
            {
                return null;
            }

            var entry = workspaceList.SelectedItems[0].Tag as FileEntry;
            return entry == null ? null : entry.FullPath;
        }

        private string ResolveWorkspaceDropTarget(int screenX, int screenY, out string targetName, out string highlightPath)
        {
            targetName = null;
            highlightPath = null;
            if (workspaceList == null || searchBox == null || !string.IsNullOrWhiteSpace(searchBox.Text))
            {
                return null;
            }

            var point = workspaceList.PointToClient(new Point(screenX, screenY));
            var targetItem = workspaceList.GetItemAt(point.X, point.Y);
            if (targetItem == null)
            {
                return currentWorkspaceDirectory;
            }

            var targetEntry = targetItem.Tag as FileEntry;
            if (targetEntry == null)
            {
                return null;
            }

            targetName = Path.GetFileName(targetEntry.FullPath);
            if (targetEntry.IsParentLink)
            {
                highlightPath = targetEntry.FullPath;
                return targetEntry.FullPath;
            }

            if (targetEntry.IsDirectory)
            {
                highlightPath = targetEntry.FullPath;
                return targetEntry.FullPath;
            }

            return Path.GetDirectoryName(targetEntry.FullPath);
        }

        private void MoveWorkspaceEntry(string sourcePath, string targetDirectory, string targetName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetDirectory))
            {
                return;
            }

            if (!IsPathUnderRoot(sourcePath, workspacePath) || !IsPathUnderRoot(targetDirectory, workspacePath))
            {
                return;
            }

            var isDirectory = Directory.Exists(sourcePath);
            if (!isDirectory && !File.Exists(sourcePath))
            {
                return;
            }

            var destinationPath = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));
            if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (isDirectory && IsPathUnderRoot(targetDirectory, sourcePath))
            {
                MessageBox.Show(this, "A folder cannot be moved inside itself.", "Move blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
            {
                MessageBox.Show(this, "An item with the same name already exists in the target folder.", "Move blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (isDirectory)
                {
                    Directory.Move(sourcePath, destinationPath);
                }
                else
                {
                    File.Move(sourcePath, destinationPath);
                }

                UpdateTrackedPathsAfterMove(sourcePath, destinationPath, isDirectory);
                LoadTree(searchBox.Text.Trim(), destinationPath);
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Move failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTrackedPathsAfterMove(string oldPath, string newPath, bool isDirectory)
        {
            var affectedDocuments = openDocuments.Values
                .Where(document => !string.IsNullOrWhiteSpace(document.Path) &&
                    (isDirectory ? IsPathUnderRoot(document.Path, oldPath) : string.Equals(document.Path, oldPath, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var document in affectedDocuments)
            {
                openDocuments.Remove(document.Path);
                document.Path = ReplaceTrackedPath(document.Path, oldPath, newPath, isDirectory);
                document.BaseTitle = Path.GetFileName(document.Path);
                document.Page.Text = document.IsDirty ? document.BaseTitle + " *" : document.BaseTitle;
                openDocuments[document.Path] = document;
            }

            lastOpenFiles = ReplaceTrackedPaths(lastOpenFiles, oldPath, newPath, isDirectory);
            recentFiles = ReplaceTrackedPaths(recentFiles, oldPath, newPath, isDirectory);
            if (!string.IsNullOrWhiteSpace(lastSelectedFilePath))
            {
                lastSelectedFilePath = ReplaceTrackedPath(lastSelectedFilePath, oldPath, newPath, isDirectory);
            }

            if (!string.IsNullOrWhiteSpace(currentWorkspaceDirectory))
            {
                currentWorkspaceDirectory = ReplaceTrackedPath(currentWorkspaceDirectory, oldPath, newPath, isDirectory);
            }

            UpdateWindowTitle();
        }

        private void RemoveTrackedPathsForDelete(string path, bool isDirectory)
        {
            var toClose = openDocuments.Values
                .Where(document => !string.IsNullOrWhiteSpace(document.Path) &&
                    (isDirectory ? IsPathUnderRoot(document.Path, path) : string.Equals(document.Path, path, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var document in toClose)
            {
                openDocuments.Remove(document.Path);
                if (autocompleteEditor != null && ReferenceEquals(autocompleteEditor, document.Editor))
                {
                    HideAutocomplete();
                }
                editorTabs.TabPages.Remove(document.Page);
                document.Page.Dispose();
            }

            lastOpenFiles = lastOpenFiles
                .Where(item => !(isDirectory ? IsPathUnderRoot(item, path) : string.Equals(item, path, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            recentFiles = recentFiles
                .Where(item => !(isDirectory ? IsPathUnderRoot(item, path) : string.Equals(item, path, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!string.IsNullOrWhiteSpace(lastSelectedFilePath) &&
                (isDirectory ? IsPathUnderRoot(lastSelectedFilePath, path) : string.Equals(lastSelectedFilePath, path, StringComparison.OrdinalIgnoreCase)))
            {
                lastSelectedFilePath = null;
            }

            UpdateEditorSurfaceState();
            RefreshEditorChrome();
        }

        private List<string> ReplaceTrackedPaths(List<string> items, string oldPath, string newPath, bool isDirectory)
        {
            return items
                .Select(item => ReplaceTrackedPath(item, oldPath, newPath, isDirectory))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string ReplaceTrackedPath(string value, string oldPath, string newPath, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (!isDirectory)
            {
                return string.Equals(value, oldPath, StringComparison.OrdinalIgnoreCase) ? newPath : value;
            }

            if (!IsPathUnderRoot(value, oldPath))
            {
                return value;
            }

            var suffix = value.Substring(oldPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrWhiteSpace(suffix) ? newPath : Path.Combine(newPath, suffix);
        }

        private bool IsPathUnderRoot(string path, string rootPath)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var fullRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string MakeRelativePath(string basePath, string path)
        {
            try
            {
                var baseUri = new Uri(Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                var targetUri = new Uri(Path.GetFullPath(path));
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch
            {
                return path;
            }
        }

        private void RevealInExplorer(string path, bool isDirectory)
        {
            try
            {
                var targetPath = isDirectory ? path : Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(targetPath) || (!Directory.Exists(targetPath) && !File.Exists(path)))
                {
                    return;
                }

                var processInfo = new ProcessStartInfo();
                processInfo.FileName = "explorer.exe";
                processInfo.Arguments = isDirectory
                    ? "\"" + path + "\""
                    : "/select,\"" + path + "\"";
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Reveal failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTree()
        {
            LoadTree(string.Empty);
        }

        private void AppendOutput(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendOutput), text);
                return;
            }

            var hadPendingInput = terminalBox.TextLength > terminalInputStart;
            terminalBox.AppendText(text);
            if (!hadPendingInput)
            {
                terminalInputStart = terminalBox.TextLength;
            }
            terminalBox.SelectionStart = terminalBox.TextLength;
            terminalBox.ScrollToCaret();
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
            UpdateStopButtonState();
        }

        private void UpdateCaretPosition(FastColoredTextBox editor)
        {
            if (caretLabel == null)
            {
                return;
            }

            if (editor == null)
            {
                caretLabel.Text = "Ln -, Col -";
                return;
            }

            var place = editor.Selection.Start;
            caretLabel.Text = "Ln " + (place.iLine + 1) + ", Col " + (place.iChar + 1);
        }

        private void PushRecentFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            recentFiles.RemoveAll(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
            recentFiles.Insert(0, path);
            if (recentFiles.Count > 12)
            {
                recentFiles.RemoveRange(12, recentFiles.Count - 12);
            }
        }

        private int CalculateStatusBoxWidth()
        {
            var maxWidth = 0;
            foreach (var statusText in statusTexts)
            {
                var size = TextRenderer.MeasureText(statusText, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                if (size.Width > maxWidth)
                {
                    maxWidth = size.Width;
                }
            }

            return maxWidth + 20;
        }

        private void JumpToFirstCompilerError(string compilerOutput, string fallbackSourcePath)
        {
            if (string.IsNullOrWhiteSpace(compilerOutput))
            {
                return;
            }

            var match = Regex.Match(compilerOutput, @"^(?<path>[A-Za-z]:\\[^:\r\n]+|[^:\r\n]+):(?<line>\d+):(?<col>\d+)?", RegexOptions.Multiline);
            if (!match.Success)
            {
                return;
            }

            var errorPath = match.Groups["path"].Value;
            int lineNumber;
            int columnNumber;
            if (!int.TryParse(match.Groups["line"].Value, out lineNumber))
            {
                return;
            }
            if (!int.TryParse(match.Groups["col"].Value, out columnNumber))
            {
                columnNumber = 1;
            }

            if (!Path.IsPathRooted(errorPath) && !string.IsNullOrWhiteSpace(fallbackSourcePath))
            {
                errorPath = Path.Combine(Path.GetDirectoryName(fallbackSourcePath), errorPath);
            }

            if (!File.Exists(errorPath))
            {
                return;
            }

            OpenFile(errorPath);
            var document = GetActiveDocument();
            if (document == null || document.Editor == null)
            {
                return;
            }

            var targetLine = Math.Max(0, lineNumber - 1);
            var targetColumn = Math.Max(0, columnNumber - 1);
            if (targetLine >= document.Editor.LinesCount)
            {
                targetLine = document.Editor.LinesCount - 1;
            }
            if (targetLine < 0)
            {
                return;
            }

            var lineLength = document.Editor.GetLineLength(targetLine);
            if (targetColumn > lineLength)
            {
                targetColumn = lineLength;
            }

            document.Editor.Selection.Start = new Place(targetColumn, targetLine);
            document.Editor.DoCaretVisible();
            document.Editor.Focus();
            UpdateCaretPosition(document.Editor);
        }

        private void UpdateAutoClearButton()
        {
            if (autoClearButton == null)
            {
                return;
            }

            var theme = GetCurrentTheme();
            autoClearButton.Text = autoClearTerminal ? "Auto Clear: On" : "Auto Clear: Off";
            autoClearButton.BackColor = autoClearTerminal ? theme.SurfaceBack : theme.TabInactive;
            autoClearButton.ForeColor = theme.Text;
        }

        private void UpdateStopButtonState()
        {
            if (stopButton == null)
            {
                return;
            }

            var theme = GetCurrentTheme();
            var active = IsExecutionActive();
            stopButton.Enabled = active;
            stopButton.BackColor = active ? theme.Danger : theme.SurfaceBack;
            stopButton.ForeColor = active ? theme.Text : theme.MutedText;
            stopButton.FlatAppearance.BorderColor = active ? theme.Danger : theme.Grid;
        }

        private void UpdateWindowTitle()
        {
            var document = GetActiveDocument();
            var name = document == null ? "No file open" : document.Page.Text;
            Text = "Jzero Compiler Native Lite - " + name;
        }

        private void UpdateEditorSurfaceState()
        {
            if (editorTabs == null || emptyEditorPanel == null)
            {
                return;
            }

            var hasOpenTabs = editorTabs.TabPages.Count > 0;
            editorTabs.Visible = hasOpenTabs;
            emptyEditorPanel.Visible = !hasOpenTabs;
        }

        private void EditorTabsResize(object sender, EventArgs e)
        {
            RefreshEditorChrome();
        }

        private void EditorResize(object sender, EventArgs e)
        {
            RefreshEditorChrome();
            if (!suppressAutocompleteUpdates)
            {
                UpdateAutocompleteForEditor(sender as FastColoredTextBox, false);
            }
        }

        private void RefreshEditorChrome()
        {
            if (editorTabs == null)
            {
                return;
            }

            editorTabs.Invalidate();
            editorTabs.Update();

            foreach (TabPage page in editorTabs.TabPages)
            {
                var document = page.Tag as EditorDocument;
                if (document != null && document.Editor != null)
                {
                    document.Editor.Invalidate();
                    document.Editor.Update();
                }
            }
        }

        private void ReloadActiveDocument()
        {
            var document = GetActiveDocument();
            if (document == null || string.IsNullOrWhiteSpace(document.Path))
            {
                return;
            }

            if (!File.Exists(document.Path))
            {
                MessageBox.Show(this, "The file no longer exists on disk.", "Reload failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (document.IsDirty)
            {
                var result = MessageBox.Show(this, "Discard unsaved changes and reload from disk?", "Reload file", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                var selectionStart = document.Editor.SelectionStart;
                suppressDocumentDirtyTracking = true;
                document.Editor.Text = File.ReadAllText(document.Path);
                document.Editor.SelectionStart = Math.Max(0, Math.Min(selectionStart, (document.Editor.Text ?? string.Empty).Length));
                document.IsDirty = false;
                document.Page.Text = document.BaseTitle;
                document.Editor.Invalidate();
                SetStatus("SAVED");
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Reload failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                suppressDocumentDirtyTracking = false;
            }
        }

        private void WorkspaceSplitSplitterMoved(object sender, SplitterEventArgs e)
        {
            if (workspaceSplit != null && workspaceVisible)
            {
                workspaceSplitterDistance = workspaceSplit.SplitterDistance;
                RefreshEditorChrome();
                SaveConfig();
            }
        }

        private void EditorTerminalSplitSplitterMoved(object sender, SplitterEventArgs e)
        {
            if (editorTerminalSplit != null)
            {
                terminalSplitterDistance = editorTerminalSplit.SplitterDistance;
                RefreshEditorChrome();
            }
        }

        private void ToggleWorkspaceVisibility()
        {
            workspaceVisible = !workspaceVisible;
            ApplyWorkspaceVisibility();
            SaveConfig();
        }

        private void ApplySavedLayoutState()
        {
            ApplyWorkspaceVisibility();
            terminalWidthPercent = 45;
            ApplyTerminalWidthPercent();
        }

        private void ApplyWorkspaceVisibility()
        {
            if (workspaceSplit == null)
            {
                return;
            }

            if (workspaceVisible)
            {
                workspaceSplit.Panel1Collapsed = false;
                var maxDistance = workspaceSplit.Width - (EditorPanelMinWidth + TerminalPanelMinWidth) - workspaceSplit.SplitterWidth;
                var minDistance = WorkspacePanelMinWidth;
                if (maxDistance >= minDistance)
                {
                    workspaceSplit.SplitterDistance = Math.Max(minDistance, Math.Min(maxDistance, workspaceSplitterDistance));
                }
            }
            else
            {
                if (!workspaceSplit.Panel1Collapsed)
                {
                    workspaceSplitterDistance = workspaceSplit.SplitterDistance;
                }
                workspaceSplit.Panel1Collapsed = true;
            }

            RefreshEditorChrome();
        }

        private void RestoreSession()
        {
            if (lastOpenFiles == null || lastOpenFiles.Count == 0)
            {
                UpdateWindowTitle();
                return;
            }

            foreach (var path in lastOpenFiles)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        OpenFile(path);
                    }
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(lastSelectedFilePath) && openDocuments.ContainsKey(lastSelectedFilePath))
            {
                editorTabs.SelectedTab = openDocuments[lastSelectedFilePath].Page;
            }

            UpdateWindowTitle();
        }

        private EditorDocument GetDocumentByEditor(FastColoredTextBox editor)
        {
            if (editor == null)
            {
                return null;
            }

            foreach (TabPage page in editorTabs.TabPages)
            {
                var document = page.Tag as EditorDocument;
                if (document != null && ReferenceEquals(document.Editor, editor))
                {
                    return document;
                }
            }

            return null;
        }

        private void ApplyEditorSettings(EditorSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            editorFontFamily = string.IsNullOrWhiteSpace(settings.EditorFontFamily) ? editorFontFamily : settings.EditorFontFamily;
            outputFontFamily = string.IsNullOrWhiteSpace(settings.OutputFontFamily) ? outputFontFamily : settings.OutputFontFamily;
            editorFontSize = ClampFontSize(settings.EditorFontSize);
            outputFontSize = ClampFontSize(settings.OutputFontSize);
            themeName = string.IsNullOrWhiteSpace(settings.ThemeName) ? themeName : settings.ThemeName;

            foreach (TabPage page in editorTabs.TabPages)
            {
                var document = page.Tag as EditorDocument;
                if (document != null && document.Editor != null)
                {
                    document.Editor.Font = new Font(editorFontFamily, editorFontSize);
                    ApplyThemeToEditor(document.Editor);
                }
            }

            if (terminalBox != null)
            {
                terminalBox.Font = new Font(outputFontFamily, outputFontSize);
            }

            ApplyCurrentTheme();
        }

        private float ClampFontSize(float size)
        {
            if (size < 8F)
            {
                return 8F;
            }

            if (size > 28F)
            {
                return 28F;
            }

            return size;
        }

        private void ApplyCurrentTheme()
        {
            var theme = GetCurrentTheme();
            BackColor = theme.WindowBack;
            ForeColor = theme.Text;

            foreach (Control control in Controls)
            {
                ApplyThemeRecursive(control, theme);
            }

            if (terminalBox != null)
            {
                terminalBox.BackColor = theme.TerminalBack;
                terminalBox.ForeColor = theme.Text;
            }

            if (emptyEditorPanel != null)
            {
                emptyEditorPanel.BackColor = theme.EditorBack;
                foreach (Control child in emptyEditorPanel.Controls)
                {
                    child.BackColor = theme.EditorBack;
                    child.ForeColor = theme.MutedText;
                }
            }

            foreach (TabPage page in editorTabs.TabPages)
            {
                page.BackColor = theme.EditorBack;
                page.ForeColor = theme.Text;
                var document = page.Tag as EditorDocument;
                if (document != null && document.Editor != null)
                {
                    ApplyThemeToEditor(document.Editor);
                }
            }

            UpdateAutoClearButton();
            UpdateStopButtonState();
            ApplyWorkspaceNavDropHighlightVisuals();
            editorTabs.Invalidate();
            ApplyAutocompleteTheme(theme);
        }

        private void ApplyThemeRecursive(Control control, ThemePalette theme)
        {
            var panel = control as Panel;
            var label = control as Label;
            var tree = control as TreeView;
            var list = control as ListView;
            var input = control as TextBox;
            var button = control as Button;
            var split = control as SplitContainer;

            if (panel != null)
            {
                panel.BackColor = panel == emptyEditorPanel ? theme.EditorBack : theme.PanelBack;
            }

            if (label != null)
            {
                label.ForeColor = theme.Text;
                if (label == workspaceLabel || label == statusLabel || label == caretLabel)
                {
                    label.BackColor = theme.SurfaceBack;
                }
                else if (label == workspaceHintLabel)
                {
                    label.BackColor = Color.Transparent;
                    label.ForeColor = theme.MutedText;
                }
            }

            if (tree != null)
            {
                tree.BackColor = theme.EditorBack;
                tree.ForeColor = theme.Text;
                tree.LineColor = theme.Grid;
            }

            if (list != null)
            {
                list.BackColor = theme.EditorBack;
                list.ForeColor = theme.Text;
            }

            if (input != null)
            {
                input.BackColor = theme.SurfaceBack;
                input.ForeColor = theme.Text;
            }

            if (button != null)
            {
                if (button == runButton)
                {
                    button.BackColor = theme.Accent;
                }
                else if (button == stopButton)
                {
                    button.BackColor = stopButton.Enabled ? theme.Danger : theme.SurfaceBack;
                }
                else if (button != autoClearButton)
                {
                    button.BackColor = theme.SurfaceBack;
                }
                button.ForeColor = button == stopButton && !stopButton.Enabled ? theme.MutedText : theme.Text;
                button.FlatAppearance.BorderColor = button == stopButton && stopButton.Enabled ? theme.Danger : theme.Grid;
            }

            if (split != null)
            {
                split.BackColor = theme.WindowBack;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeRecursive(child, theme);
            }
        }

        private void ApplyThemeToEditor(FastColoredTextBox editor)
        {
            var theme = GetCurrentTheme();
            editor.BackColor = theme.EditorBack;
            editor.PaddingBackColor = theme.EditorBack;
            editor.ForeColor = theme.Text;
            editor.LineNumberColor = theme.MutedText;
            editor.IndentBackColor = theme.EditorGutter;
            editor.ServiceLinesColor = theme.Grid;
            editor.SelectionColor = theme.Selection;
            editor.CurrentLineColor = theme.EditorCurrentLine;
            editor.CaretColor = theme.Text;
        }

        private void InitializeAutocomplete()
        {
            autocompleteList = new ListBox();
            autocompleteList.BorderStyle = BorderStyle.None;
            autocompleteList.IntegralHeight = false;
            autocompleteList.Font = new Font(editorFontFamily, Math.Max(9F, editorFontSize));
            autocompleteList.Width = 260;
            autocompleteList.Height = 180;
            autocompleteList.Click += AutocompleteListClick;
            autocompleteList.DoubleClick += AutocompleteListDoubleClick;
            autocompleteList.MouseMove += AutocompleteListMouseMove;

            var host = new ToolStripControlHost(autocompleteList);
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.Size = autocompleteList.Size;

            autocompletePopup = new ToolStripDropDown();
            autocompletePopup.Padding = Padding.Empty;
            autocompletePopup.Margin = Padding.Empty;
            autocompletePopup.AutoClose = false;
            autocompletePopup.Items.Add(host);
            ApplyAutocompleteTheme(GetCurrentTheme());
        }

        private void ApplyAutocompleteTheme(ThemePalette theme)
        {
            if (autocompleteList == null || autocompletePopup == null)
            {
                return;
            }

            autocompleteList.BackColor = theme.EditorBack;
            autocompleteList.ForeColor = theme.Text;
            autocompletePopup.BackColor = theme.PanelBack;
            autocompleteList.Font = new Font(editorFontFamily, Math.Max(9F, editorFontSize));
        }

        private void UpdateAutocompleteForEditor(FastColoredTextBox editor, bool forceShow)
        {
            if (editor == null || autocompleteList == null || autocompletePopup == null)
            {
                HideAutocomplete();
                return;
            }

            if (editor.SelectionLength > 0)
            {
                HideAutocomplete();
                return;
            }

            int replaceStart;
            string prefix;
            if (!TryGetAutocompletePrefix(editor, forceShow, out replaceStart, out prefix))
            {
                HideAutocomplete();
                return;
            }

            var suggestions = BuildAutocompleteSuggestions(editor, prefix, forceShow).ToList();
            if (suggestions.Count == 0)
            {
                HideAutocomplete();
                return;
            }

            autocompleteEditor = editor;
            autocompleteReplaceStart = replaceStart;
            autocompleteReplaceLength = editor.SelectionStart - replaceStart;

            autocompleteList.BeginUpdate();
            autocompleteList.Items.Clear();
            foreach (var suggestion in suggestions)
            {
                autocompleteList.Items.Add(suggestion);
            }
            autocompleteList.SelectedIndex = 0;
            autocompleteList.EndUpdate();

            var popupLocation = GetAutocompletePopupLocation(editor);
            autocompletePopup.Show(popupLocation);
            autocompleteList.Focus();
            editor.Focus();
        }

        private bool TryGetAutocompletePrefix(FastColoredTextBox editor, bool forceShow, out int replaceStart, out string prefix)
        {
            replaceStart = editor.SelectionStart;
            prefix = string.Empty;

            var position = editor.SelectionStart;
            if (position < 0 || position > (editor.Text ?? string.Empty).Length)
            {
                return false;
            }

            var text = editor.Text ?? string.Empty;
            var start = position;
            while (start > 0)
            {
                var ch = text[start - 1];
                if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '#')
                {
                    break;
                }
                start--;
            }

            prefix = text.Substring(start, position - start);
            replaceStart = start;
            return forceShow || prefix.Length >= 1;
        }

        private IEnumerable<string> BuildAutocompleteSuggestions(FastColoredTextBox editor, string prefix, bool forceShow)
        {
            var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var keyword in cKeywords)
            {
                suggestions.Add(keyword);
            }
            foreach (var snippet in autocompleteSnippets)
            {
                suggestions.Add(snippet);
            }

            foreach (Match match in Regex.Matches(editor.Text ?? string.Empty, @"\b[A-Za-z_][A-Za-z0-9_]*\b"))
            {
                suggestions.Add(match.Value);
            }

            var filtered = suggestions
                .Where(item => forceShow || item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Where(item => forceShow || !string.Equals(item, prefix, StringComparison.Ordinal))
                .OrderBy(item => item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(item => item.Length)
                .ThenBy(item => item)
                .Take(14);

            return filtered;
        }

        private Point GetAutocompletePopupLocation(FastColoredTextBox editor)
        {
            var caretPlace = editor.PositionToPlace(editor.SelectionStart);
            var caretPoint = editor.PlaceToPoint(caretPlace);
            var clientPoint = new Point(caretPoint.X, caretPoint.Y + editor.CharHeight + 6);
            return editor.PointToScreen(clientPoint);
        }

        private void MoveAutocompleteSelection(int offset)
        {
            if (autocompleteList == null || autocompleteList.Items.Count == 0)
            {
                return;
            }

            var nextIndex = autocompleteList.SelectedIndex + offset;
            if (nextIndex < 0)
            {
                nextIndex = autocompleteList.Items.Count - 1;
            }
            else if (nextIndex >= autocompleteList.Items.Count)
            {
                nextIndex = 0;
            }

            autocompleteList.SelectedIndex = nextIndex;
        }

        private void CommitAutocompleteSelection()
        {
            if (autocompleteEditor == null || autocompleteList == null || autocompleteList.SelectedItem == null)
            {
                HideAutocomplete();
                return;
            }

            var value = autocompleteList.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                HideAutocomplete();
                return;
            }

            var editor = autocompleteEditor;
            var replaceStart = autocompleteReplaceStart;
            var replaceLength = autocompleteReplaceLength;

            suppressAutocompleteUpdates = true;
            try
            {
                HideAutocomplete();
                if (editor == null || editor.IsDisposed)
                {
                    return;
                }

                replaceStart = Math.Max(0, Math.Min(replaceStart, (editor.Text ?? string.Empty).Length));
                replaceLength = Math.Max(0, Math.Min(replaceLength, (editor.Text ?? string.Empty).Length - replaceStart));
                var text = editor.Text ?? string.Empty;
                var updatedText = text.Substring(0, replaceStart) + value + text.Substring(replaceStart + replaceLength);
                editor.Text = updatedText;
                editor.SelectionStart = replaceStart + value.Length;
                editor.Focus();
            }
            catch
            {
                if (!suppressAutocompleteCommitErrors)
                {
                    suppressAutocompleteCommitErrors = true;
                    try
                    {
                        HideAutocomplete();
                    }
                    finally
                    {
                        suppressAutocompleteCommitErrors = false;
                    }
                }
            }
            finally
            {
                suppressAutocompleteUpdates = false;
            }
        }

        private void HideAutocomplete()
        {
            if (autocompletePopup != null && autocompletePopup.Visible)
            {
                autocompletePopup.Close();
            }

            autocompleteEditor = null;
            autocompleteReplaceStart = -1;
            autocompleteReplaceLength = 0;
        }

        private void AutocompleteListClick(object sender, EventArgs e)
        {
            if (autocompleteList != null && autocompleteList.SelectedIndex >= 0)
            {
                CommitAutocompleteSelection();
            }
        }

        private void AutocompleteListDoubleClick(object sender, EventArgs e)
        {
            CommitAutocompleteSelection();
        }

        private void AutocompleteListMouseMove(object sender, MouseEventArgs e)
        {
            var index = autocompleteList == null ? -1 : autocompleteList.IndexFromPoint(e.Location);
            if (autocompleteList != null && index >= 0 && index < autocompleteList.Items.Count)
            {
                autocompleteList.SelectedIndex = index;
            }
        }

        private ThemePalette GetCurrentTheme()
        {
            ThemePalette theme;
            if (!themes.TryGetValue(themeName, out theme))
            {
                theme = themes["Carbon"];
            }
            return theme;
        }

        private void ApplyTerminalWidthPercent()
        {
            if (editorTerminalSplit == null || editorTerminalSplit.Width <= 0)
            {
                return;
            }

            var totalWidth = editorTerminalSplit.Width - editorTerminalSplit.SplitterWidth;
            var terminalWidth = (int)(totalWidth * (terminalWidthPercent / 100F));
            var maxDistance = editorTerminalSplit.Width - TerminalPanelMinWidth - editorTerminalSplit.SplitterWidth;
            var minDistance = EditorPanelMinWidth;
            var desiredDistance = totalWidth - terminalWidth;
            editorTerminalSplit.SplitterDistance = Math.Max(minDistance, Math.Min(maxDistance, desiredDistance));
            terminalSplitterDistance = editorTerminalSplit.SplitterDistance;
        }

        private int GetTerminalWidthPercentFromCurrentLayout()
        {
            if (editorTerminalSplit == null || editorTerminalSplit.Width <= editorTerminalSplit.SplitterWidth)
            {
                return terminalWidthPercent;
            }

            var totalWidth = editorTerminalSplit.Width - editorTerminalSplit.SplitterWidth;
            var terminalWidth = totalWidth - editorTerminalSplit.SplitterDistance;
            return Math.Max(20, Math.Min(60, (int)Math.Round((terminalWidth * 100F) / totalWidth)));
        }

        private string GetDefaultWorkspace()
        {
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            return Path.Combine(downloads, "C Programs");
        }

        private void EnsureWorkspaceExists()
        {
            if (!Directory.Exists(workspacePath))
            {
                Directory.CreateDirectory(workspacePath);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    return;
                }

                var serializer = new JavaScriptSerializer();
                var config = serializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                if (config != null && !string.IsNullOrWhiteSpace(config.Workspace))
                {
                    workspacePath = config.Workspace;
                    currentWorkspaceDirectory = workspacePath;
                }
                if (config != null && config.AutoClearTerminal.HasValue)
                {
                    autoClearTerminal = config.AutoClearTerminal.Value;
                }
                if (config != null && config.EditorFontSize.HasValue)
                {
                    editorFontSize = ClampFontSize(config.EditorFontSize.Value);
                }
                if (config != null && !string.IsNullOrWhiteSpace(config.EditorFontFamily))
                {
                    editorFontFamily = config.EditorFontFamily;
                }
                if (config != null && config.OutputFontSize.HasValue)
                {
                    outputFontSize = ClampFontSize(config.OutputFontSize.Value);
                }
                if (config != null && !string.IsNullOrWhiteSpace(config.OutputFontFamily))
                {
                    outputFontFamily = config.OutputFontFamily;
                }
                if (config != null && !string.IsNullOrWhiteSpace(config.ThemeName))
                {
                    themeName = config.ThemeName;
                    if (string.Equals(themeName, "Green Light", StringComparison.OrdinalIgnoreCase))
                    {
                        themeName = "Mint Light";
                    }
                }
                if (config != null && config.WorkspaceVisible.HasValue)
                {
                    workspaceVisible = config.WorkspaceVisible.Value;
                }
                if (config != null && config.WorkspaceSplitterDistance.HasValue)
                {
                    workspaceSplitterDistance = Math.Max(WorkspacePanelMinWidth, config.WorkspaceSplitterDistance.Value);
                }
                if (config != null && config.TerminalSplitterDistance.HasValue)
                {
                    terminalSplitterDistance = Math.Max(EditorPanelMinWidth, config.TerminalSplitterDistance.Value);
                }
                if (config != null && config.TerminalWidthPercent.HasValue)
                {
                    terminalWidthPercent = Math.Max(20, Math.Min(60, config.TerminalWidthPercent.Value));
                }
                if (config != null && config.OpenFiles != null)
                {
                    lastOpenFiles = config.OpenFiles
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                if (config != null && config.RecentFiles != null)
                {
                    recentFiles = config.RecentFiles
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                if (config != null && !string.IsNullOrWhiteSpace(config.SelectedFile))
                {
                    lastSelectedFilePath = config.SelectedFile;
                }
            }
            catch
            {
            }
        }

        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(configDirectory);
                var serializer = new JavaScriptSerializer();
                var openFilePaths = openDocuments.Values
                    .Where(document => !string.IsNullOrWhiteSpace(document.Path))
                    .Select(document => document.Path)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var activeDocument = GetActiveDocument();
                var serialized = serializer.Serialize(new AppConfig
                {
                    Workspace = workspacePath,
                    AutoClearTerminal = autoClearTerminal,
                    EditorFontFamily = editorFontFamily,
                    EditorFontSize = editorFontSize,
                    OutputFontFamily = outputFontFamily,
                    OutputFontSize = outputFontSize,
                    ThemeName = themeName,
                    WorkspaceVisible = workspaceVisible,
                    WorkspaceSplitterDistance = workspaceSplitterDistance,
                    TerminalSplitterDistance = terminalSplitterDistance,
                    TerminalWidthPercent = terminalWidthPercent,
                    OpenFiles = openFilePaths,
                    RecentFiles = recentFiles.Take(12).ToList(),
                    SelectedFile = activeDocument == null ? null : activeDocument.Path
                });
                var tempPath = configPath + ".tmp";
                File.WriteAllText(tempPath, serialized);
                if (File.Exists(configPath))
                {
                    File.Replace(tempPath, configPath, null);
                }
                else
                {
                    File.Move(tempPath, configPath);
                }
            }
            catch
            {
            }
        }

        private string InjectBufferedOutputFix(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return source;
            }

            var match = Regex.Match(source, @"(?:int|void)\s+main\s*\([^)]*\)\s*\{");
            if (!match.Success || source.Contains("setvbuf(stdout"))
            {
                return source;
            }

            return source.Insert(match.Index + match.Length, Environment.NewLine + "    setvbuf(stdout, NULL, _IONBF, 0);" + Environment.NewLine + "    setvbuf(stderr, NULL, _IONBF, 0);");
        }

        private void RestoreOriginalFileIfNeeded(EditorDocument document, string tempSourcePath, string originalText)
        {
            if (!string.IsNullOrWhiteSpace(tempSourcePath))
            {
                try
                {
                    if (File.Exists(tempSourcePath))
                    {
                        File.Delete(tempSourcePath);
                    }
                }
                catch
                {
                }
                return;
            }

            if (document != null && !string.IsNullOrWhiteSpace(document.Path))
            {
                try
                {
                    File.WriteAllText(document.Path, originalText);
                }
                catch
                {
                }
            }
        }

        private sealed class FileEntry
        {
            internal string FullPath;
            internal bool IsDirectory;
            internal bool IsParentLink;
        }

        private sealed class WorkspaceListView : ListView
        {
            internal WorkspaceListView()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
                UpdateStyles();
            }

            protected override void OnNotifyMessage(Message m)
            {
                const int WM_ERASEBKGND = 0x0014;
                if (m.Msg != WM_ERASEBKGND)
                {
                    base.OnNotifyMessage(m);
                }
            }

            internal void InvalidateHighlightRows()
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    Invalidate(GetItemRect(i));
                }
            }
        }

        private sealed class EditorDocument
        {
            internal string Path;
            internal string BaseTitle;
            internal bool IsDirty;
            internal TabPage Page;
            internal FastColoredTextBox Editor;
        }

        private sealed class CompileRequest
        {
            internal EditorDocument Document;
            internal string SourcePath;
            internal string TempSourcePath;
            internal string OriginalText;
            internal string ExePath;
            internal string WorkingDirectory;
        }

        private sealed class AppConfig
        {
            public string Workspace { get; set; }
            public bool? AutoClearTerminal { get; set; }
            public string EditorFontFamily { get; set; }
            public float? EditorFontSize { get; set; }
            public string OutputFontFamily { get; set; }
            public float? OutputFontSize { get; set; }
            public string ThemeName { get; set; }
            public bool? WorkspaceVisible { get; set; }
            public int? WorkspaceSplitterDistance { get; set; }
            public int? TerminalSplitterDistance { get; set; }
            public int? TerminalWidthPercent { get; set; }
            public List<string> OpenFiles { get; set; }
            public List<string> RecentFiles { get; set; }
            public string SelectedFile { get; set; }
        }
    }
}
