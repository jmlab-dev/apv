using System.ComponentModel;
using System.Diagnostics;
using AdrenalinProfileViewer.Models;
using AdrenalinProfileViewer.Services;

namespace AdrenalinProfileViewer.UI;

public sealed class MainForm : Form
{
    private const int CurrentSessionSchema = 13;
    private const int DefaultInspectorWidthLogical = 380;
    private const int CompareSelectorMaximumWidthLogical = 250;
    private const int MinimumInspectorWidthLogical = 300;
    private const int MinimumLibraryWidthLogical = 430;
    private const string ProfileGridKey = "profiles";
    private const string CompareGridKey = "compare";
    private const string RawGridKey = "raw-features";

    private readonly string[] _startupArgs;
    private readonly AdrenalinProfileParser _parser = new();
    private readonly ProfileMetadataStore _metadataStore = new();
    private readonly AppSessionStore _sessionStore = new();
    private readonly BindingList<AdrenalinProfile> _profiles = [];
    private readonly HashSet<string> _loadedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppSessionState _session;
    private readonly bool _isFirstRun;
    private readonly bool _migrateToCompactWindow;

    private AppThemeKind _themeKind;
    private ThemePalette _palette;
    private AdrenalinProfile? _displayedProfile;
    private bool _savingMetadata;
    private bool _suppressThemeChange;
    private bool _splitterInitialized;
    private bool _applyingSplitter;
    private int _desiredInspectorWidthLogical = DefaultInspectorWidthLogical;
    private bool _initialProfileGridFitPending;
    private bool _initialCompareGridFitPending;
    private bool _initialRawGridFitPending;

    private readonly SplitContainer _mainSplit = new();
    private readonly TableLayoutPanel _metricGrid = new();
    private readonly PolishedTabHost _tabs = new();
    private readonly BrandHeader _brandHeader = new();
    private readonly ComboBox _themeSelector = new();
    private readonly DataGridView _profileGrid = new();
    private readonly DataGridView _rawGrid = new();
    private readonly DataGridView _compareGrid = new();
    private readonly ComboBox _compareLeft = new();
    private readonly ComboBox _compareRight = new();
    private readonly RichTextBox _rawXml = new();
    private readonly TextBox _nameBox = new();
    private readonly TextBox _notesBox = new();
    private readonly Label _profileCountLabel = CreateLabel("0 profiles", ThemeRole.Muted, 9f);
    private readonly Label _fileValue = CreateValueLabel();
    private readonly Label _gpuValue = CreateValueLabel();
    private readonly Label _memoryValue = CreateValueLabel();
    private readonly Label _timingValue = CreateValueLabel();
    private readonly Label _fanValue = CreateValueLabel();
    private readonly Label _zeroRpmValue = CreateValueLabel();
    private readonly MetricCard _powerCard = new() { Caption = "Power limit" };
    private readonly MetricCard _coreCard = new() { Caption = "Max frequency" };
    private readonly MetricCard _voltageCard = new() { Caption = "Voltage offset" };
    private readonly MetricCard _memoryCard = new() { Caption = "Effective memory" };
    private readonly Label _status = new()
    {
        Text = "Drop one or more Adrenalin XML profiles here.",
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Tag = ThemeRole.Muted,
        Margin = Padding.Empty
    };
    private readonly Label _footer = new()
    {
        Text = "designed by jmlab_dev",
        AutoSize = true,
        Anchor = AnchorStyles.Right,
        TextAlign = ContentAlignment.MiddleRight,
        Tag = ThemeRole.Footer,
        Font = UiFonts.Code(8.5f, FontStyle.Bold),
        Margin = Padding.Empty
    };

    public MainForm(string[] startupArgs)
    {
        _startupArgs = startupArgs;
        _isFirstRun = !File.Exists(PortablePaths.SessionFilePath);
        _session = _sessionStore.Load();
        var loadedSessionSchema = _session.SchemaVersion;
        // Only very old layouts need a one-time window-size reset. A v1.1.8 -> v1.1.9
        // upgrade preserves the user's saved window geometry and changes only the inspector defaults.
        _migrateToCompactWindow = !_isFirstRun && loadedSessionSchema < 9;
        if (loadedSessionSchema < CurrentSessionSchema)
        {
            // Schema 3 introduced DPI-safe grid defaults. Schema 4 widened the XML library.
            // Schema 5 made the right pane compact. Schema 6 introduced the compact-window
            // migration. Schema 7 restructured the inspector around a 300-pixel logical width.
            // Schema 8 fixed first-paint splitter timing. Schema 9 restored user adjustment.
            // Schema 10 added the responsive 380-pixel inspector. Schema 11 keeps metric
            // cards on one row, adds vector tab/file-picker icons, and tightens Compare/Features.
            // Schema 12 refines metric typography and adds semantic numeric comparison highlighting.
            // Schema 13 distributes metric cards evenly and adds auto-fitting value text.
            if (loadedSessionSchema < 3)
            {
                _session.Grids.Clear();
            }
            else
            {
                // Refit the loaded-profile columns once after layout migrations.
                _session.Grids.Remove(ProfileGridKey);
            }
            if (loadedSessionSchema < 10)
            {
                _session.MainRightPaneWidthLogical = DefaultInspectorWidthLogical;
            }
            if (loadedSessionSchema < 11)
            {
                _session.Grids.Remove(CompareGridKey);
                _session.Grids.Remove(RawGridKey);
            }
            _session.SchemaVersion = CurrentSessionSchema;
        }
        _desiredInspectorWidthLogical = Math.Clamp(
            _session.MainRightPaneWidthLogical,
            MinimumInspectorWidthLogical,
            900);
        _themeKind = ThemeCatalog.Parse(_session.Theme);
        _palette = ThemeCatalog.Get(_themeKind);
        _initialProfileGridFitPending = !_session.Grids.ContainsKey(ProfileGridKey);
        _initialCompareGridFitPending = !_session.Grids.ContainsKey(CompareGridKey);
        _initialRawGridFitPending = !_session.Grids.ContainsKey(RawGridKey);

        Text = "AMD Adrenalin Profile Viewer";
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(900, 640);
        Font = new Font("Segoe UI", 9f);
        AllowDrop = true;
        ApplyInitialWindowBounds();

        Controls.Add(BuildMainLayout());
        Controls.Add(BuildStatusStrip());
        RestoreGridLayouts();
        ApplyTheme();
        ShowSelectedProfile();

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        Shown += OnShown;
        FormClosing += OnFormClosing;
    }

    private Control BuildMainLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14, 14, 14, 8),
            Tag = ThemeRole.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);
        root.Controls.Add(BuildSplitView(), 0, 2);
        return root;
    }

    private Control BuildHeader()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceLevel = SurfaceLevel.Surface,
            Padding = new Padding(12, 9, 12, 9),
            Margin = new Padding(0, 0, 0, 8)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Tag = ThemeRole.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 215));
        layout.Controls.Add(_brandHeader, 0, 0);
        layout.Controls.Add(BuildThemeSelector(), 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildThemeSelector()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 7, 4, 5),
            Tag = ThemeRole.Surface
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.Controls.Add(CreateLabel("THEME", ThemeRole.Muted, 8.5f, FontStyle.Bold), 0, 0);

        _themeSelector.Dock = DockStyle.Fill;
        _themeSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeSelector.FlatStyle = FlatStyle.Flat;
        _themeSelector.IntegralHeight = false;
        _themeSelector.DropDownHeight = 120;
        foreach (var kind in ThemeCatalog.All)
        {
            _themeSelector.Items.Add(ThemeCatalog.DisplayName(kind));
        }

        _suppressThemeChange = true;
        _themeSelector.SelectedIndex = Math.Max(0, ThemeCatalog.All.IndexOf(_themeKind));
        _suppressThemeChange = false;
        _themeSelector.SelectedIndexChanged += (_, _) => ChangeThemeFromSelector();
        panel.Controls.Add(_themeSelector, 0, 1);
        return panel;
    }

    private Control BuildToolbar()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 10,
            SurfaceLevel = SurfaceLevel.Surface,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 8)
        };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            Tag = ThemeRole.Surface
        };

        var openFiles = CreateButton("Open XML…", RoundedButtonStyle.Primary, (_, _) => OpenFiles());
        var openFolder = CreateButton("Open folder…", RoundedButtonStyle.Secondary, (_, _) => OpenFolder());
        var export = CreateButton("Export CSV…", RoundedButtonStyle.Secondary, (_, _) => ExportCsv());
        var openData = CreateButton("Data folder", RoundedButtonStyle.Secondary, (_, _) => OpenDataFolder());
        var remove = CreateButton("Remove selected", RoundedButtonStyle.Secondary, (_, _) => RemoveSelected());
        var clear = CreateButton("Clear all", RoundedButtonStyle.Danger, (_, _) => ClearAll());

        toolbar.Controls.AddRange([openFiles, openFolder, export, openData, remove, clear]);
        card.Controls.Add(toolbar);
        return card;
    }

    private Control BuildSplitView()
    {
        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.Orientation = Orientation.Vertical;
        // Keep the inspector at its chosen width when the main window is resized, while still
        // allowing the user to drag the splitter and choose a different inspector width.
        _mainSplit.FixedPanel = FixedPanel.Panel2;
        _mainSplit.IsSplitterFixed = false;
        _mainSplit.SplitterWidth = 8;
        _mainSplit.BackColor = _palette.Background;
        _mainSplit.Panel1.BackColor = _palette.Background;
        _mainSplit.Panel2.BackColor = _palette.Background;
        _mainSplit.Panel1.Padding = new Padding(0, 0, 5, 0);
        _mainSplit.Panel2.Padding = new Padding(5, 0, 0, 0);
        _mainSplit.Panel1.Controls.Add(BuildProfileLibrary());
        _mainSplit.Panel2.Controls.Add(BuildTabsCard());
        _mainSplit.Layout += (_, _) => InitializeSplitter();
        _mainSplit.SplitterMoved += (_, _) => CaptureAdjustedInspectorWidth();
        return _mainSplit;
    }

    private Control BuildProfileLibrary()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceLevel = SurfaceLevel.Surface,
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Tag = ThemeRole.Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Tag = ThemeRole.Surface
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        heading.Controls.Add(CreateLabel("Loaded profiles", ThemeRole.Heading, 12f, FontStyle.Bold), 0, 0);
        _profileCountLabel.Anchor = AnchorStyles.Right;
        heading.Controls.Add(_profileCountLabel, 1, 0);

        ConfigureProfileGrid();
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(WrapGrid(_profileGrid), 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildTabsCard()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            SurfaceLevel = SurfaceLevel.Surface,
            Padding = new Padding(8)
        };

        _tabs.Dock = DockStyle.Fill;
        _tabs.AddPage(BuildDetailsTab(), "Details", UiIconKind.Details);
        _tabs.AddPage(BuildCompareTab(), "Compare", UiIconKind.Compare);
        _tabs.AddPage(BuildRawFeaturesTab(), "Features", UiIconKind.Features);
        _tabs.AddPage(BuildRawXmlTab(), "XML", UiIconKind.Xml);
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke((Action)ApplyInitialGridSizing);
            }
            else
            {
                // Construction-time selection changes do not need marshaling; OnShown performs
                // the final sizing pass after all native handles and real dimensions exist.
                ApplyInitialGridSizing();
            }
        };
        card.Controls.Add(_tabs);
        return card;
    }

    private Control BuildDetailsTab()
    {
        var tab = NewTab("Profile details");
        var scroll = new DarkScrollHost
        {
            Dock = DockStyle.Fill,
            ContentPadding = 12,
            // Fill the inspector width chosen by the user. The four key metric cards remain
            // in one scanning row and resize proportionally with the pane.
            MaximumContentWidth = 0,
            Tag = ThemeRole.Surface
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            Tag = ThemeRole.Surface
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(BuildMetricCards(), 0, 0);
        content.Controls.Add(BuildProfileInformationCard(), 0, 1);
        content.Controls.Add(BuildMetadataCard(), 0, 2);

        var note = CreateLabel(
            $"Effective profile clock = stored XML value - {AdrenalinProfile.MemoryClockXmlOffsetMHz} MHz (for example, 2728 → 2714 MHz). This is the observed RDNA 4 Adrenalin profile conversion, not the doubled GDDR6 transfer rate. Unknown IDs remain available under Raw features.",
            ThemeRole.Muted,
            8.75f);
        note.Margin = new Padding(4, 12, 4, 4);
        content.Controls.Add(note, 0, 3);

        content.SizeChanged += (_, _) =>
        {
            var target = new Size(
                Math.Max(DpiMetrics.Scale(content, 150), content.ClientSize.Width - DpiMetrics.Scale(content, 8)),
                0);
            if (note.MaximumSize != target)
            {
                note.MaximumSize = target;
            }
        };

        scroll.SetContent(content);
        tab.Controls.Add(scroll);
        return tab;
    }

    private Control BuildMetricCards()
    {
        _metricGrid.Dock = DockStyle.Top;
        _metricGrid.AutoSize = true;
        _metricGrid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _metricGrid.ColumnCount = 4;
        _metricGrid.RowCount = 1;
        _metricGrid.Tag = ThemeRole.Surface;
        _metricGrid.Margin = new Padding(0, 0, 0, 10);

        // Keep all four key values in one evenly distributed scanning row. Long captions wrap,
        // while the plain value text automatically reduces its font size when required.
        _metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        _metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        _metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        _metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        _metricGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _powerCard.Dock = DockStyle.Fill;
        _coreCard.Dock = DockStyle.Fill;
        _voltageCard.Dock = DockStyle.Fill;
        _memoryCard.Dock = DockStyle.Fill;
        // Identical margins keep the visible card widths equal, including the outer cards.
        _powerCard.Margin = new Padding(4, 0, 4, 0);
        _coreCard.Margin = new Padding(4, 0, 4, 0);
        _voltageCard.Margin = new Padding(4, 0, 4, 0);
        _memoryCard.Margin = new Padding(4, 0, 4, 0);

        _metricGrid.Controls.Add(_powerCard, 0, 0);
        _metricGrid.Controls.Add(_coreCard, 1, 0);
        _metricGrid.Controls.Add(_voltageCard, 2, 0);
        _metricGrid.Controls.Add(_memoryCard, 3, 0);
        return _metricGrid;
    }

    private Control BuildProfileInformationCard()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            CornerRadius = 10,
            SurfaceLevel = SurfaceLevel.Raised,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 10)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 6,
            Tag = ThemeRole.Raised
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddCompactSettingRow(table, 0, "File", _fileValue);
        AddCompactSettingRow(table, 1, "GPU", _gpuValue);
        AddCompactSettingRow(table, 2, "Memory clock\n(stored XML)", _memoryValue);
        AddCompactSettingRow(table, 3, "Memory timings", _timingValue);
        AddCompactSettingRow(table, 4, "Fan mode", _fanValue);
        AddCompactSettingRow(table, 5, "Zero RPM", _zeroRpmValue);
        card.Controls.Add(table);
        return card;
    }

    private Control BuildMetadataCard()
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            CornerRadius = 10,
            SurfaceLevel = SurfaceLevel.Raised,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 0, 4)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 7,
            Tag = ThemeRole.Raised
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        table.Controls.Add(CreateLabel("Profile name", ThemeRole.Muted, 9f), 0, 0);
        _nameBox.Dock = DockStyle.Fill;
        _nameBox.Margin = new Padding(0, 0, 0, 6);
        StyleTextBox(_nameBox, multiline: false);
        table.Controls.Add(_nameBox, 0, 1);

        var notesLabel = CreateLabel("Notes", ThemeRole.Muted, 9f);
        notesLabel.Margin = new Padding(0, 2, 0, 2);
        table.Controls.Add(notesLabel, 0, 2);
        _notesBox.Dock = DockStyle.Fill;
        _notesBox.ScrollBars = ScrollBars.Vertical;
        _notesBox.Margin = new Padding(0, 0, 0, 7);
        StyleTextBox(_notesBox, multiline: true);
        table.Controls.Add(_notesBox, 0, 3);

        var saveButton = CreateButton("Save name and notes", RoundedButtonStyle.Primary, (_, _) => SaveMetadata());
        saveButton.Dock = DockStyle.Top;
        saveButton.Margin = new Padding(0, 2, 0, 5);
        table.Controls.Add(saveButton, 0, 4);

        var locationButton = CreateButton("Open file location", RoundedButtonStyle.Secondary, (_, _) => OpenSelectedLocation());
        locationButton.Dock = DockStyle.Top;
        locationButton.Margin = new Padding(0, 0, 0, 6);
        table.Controls.Add(locationButton, 0, 5);

        var autosave = CreateLabel("Names, notes, opened files and layout are saved automatically on exit.", ThemeRole.Muted, 8.25f);
        autosave.Dock = DockStyle.Top;
        table.Controls.Add(autosave, 0, 6);
        table.SizeChanged += (_, _) =>
        {
            var target = new Size(Math.Max(DpiMetrics.Scale(table, 140), table.ClientSize.Width), 0);
            if (autosave.MaximumSize != target)
            {
                autosave.MaximumSize = target;
            }
        };

        card.Controls.Add(table);
        return card;
    }

    private Control BuildCompareTab()
    {
        var tab = NewTab("Compare");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8),
            Tag = ThemeRole.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var selectorHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Tag = ThemeRole.Surface
        };
        var selectorCard = new RoundedPanel
        {
            CornerRadius = 9,
            SurfaceLevel = SurfaceLevel.Raised,
            Padding = new Padding(9, 7, 9, 7),
            Margin = Padding.Empty
        };
        var selectors = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Tag = ThemeRole.Raised
        };
        selectors.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        selectors.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        selectors.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        StyleComboBox(_compareLeft);
        StyleComboBox(_compareRight);
        _compareLeft.SelectedIndexChanged += (_, _) => UpdateComparison();
        _compareRight.SelectedIndexChanged += (_, _) => UpdateComparison();
        selectors.Controls.Add(BuildCompareProfilePicker(_compareLeft, "Profile A"), 0, 0);
        selectors.Controls.Add(new Label
        {
            Text = "VS",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiFonts.Code(8.25f, FontStyle.Bold),
            Tag = ThemeRole.Accent
        }, 0, 1);
        selectors.Controls.Add(BuildCompareProfilePicker(_compareRight, "Profile B"), 0, 2);
        selectorCard.Controls.Add(selectors);
        selectorHost.Controls.Add(selectorCard);

        void LayoutSelectorCard()
        {
            if (selectorHost.ClientSize.Width <= 0 || selectorHost.ClientSize.Height <= 0)
            {
                return;
            }

            var margin = DpiMetrics.Scale(selectorHost, 4);
            var available = Math.Max(1, selectorHost.ClientSize.Width - (margin * 2));
            var desired = Math.Min(DpiMetrics.Scale(selectorHost, CompareSelectorMaximumWidthLogical), available);
            var minimum = Math.Min(DpiMetrics.Scale(selectorHost, 190), available);
            var width = Math.Max(minimum, desired);
            selectorCard.Bounds = new Rectangle(
                Math.Max(0, (selectorHost.ClientSize.Width - width) / 2),
                margin,
                width,
                Math.Max(1, selectorHost.ClientSize.Height - (margin * 2)));
        }

        selectorHost.Resize += (_, _) => LayoutSelectorCard();
        selectorHost.HandleCreated += (_, _) => LayoutSelectorCard();

        ConfigureCompareGrid();
        root.Controls.Add(selectorHost, 0, 0);
        root.Controls.Add(WrapGrid(_compareGrid), 0, 1);
        tab.Controls.Add(root);
        return tab;
    }

    private Control BuildCompareProfilePicker(ComboBox combo, string labelText)
    {
        var picker = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 7,
            SurfaceLevel = SurfaceLevel.Surface,
            DrawBorder = true,
            Padding = new Padding(7, 3, 7, 3),
            Margin = Padding.Empty
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Tag = ThemeRole.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var icon = new IconGlyph
        {
            IconKind = UiIconKind.File,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 1, 4, 1)
        };
        var label = CreateLabel(labelText, ThemeRole.Muted, 9.25f, FontStyle.Bold);
        label.AutoSize = false;
        label.AutoEllipsis = false;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Margin = Padding.Empty;
        combo.Dock = DockStyle.Fill;
        combo.Margin = Padding.Empty;

        layout.Controls.Add(icon, 0, 0);
        layout.Controls.Add(label, 1, 0);
        layout.Controls.Add(combo, 2, 0);
        picker.Controls.Add(layout);
        return picker;
    }

    private Control BuildRawFeaturesTab()
    {
        var tab = NewTab("Raw features");
        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), Tag = ThemeRole.Surface };
        ConfigureRawGrid();
        host.Controls.Add(WrapGrid(_rawGrid));
        tab.Controls.Add(host);
        return tab;
    }

    private Control BuildRawXmlTab()
    {
        var tab = NewTab("Raw XML");
        var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), Tag = ThemeRole.Surface };
        var host = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 9,
            SurfaceLevel = SurfaceLevel.Raised,
            Padding = new Padding(1)
        };
        _rawXml.Dock = DockStyle.Fill;
        _rawXml.ReadOnly = true;
        _rawXml.WordWrap = false;
        _rawXml.BorderStyle = BorderStyle.None;
        _rawXml.Font = new Font("Cascadia Mono", 9f);
        host.Controls.Add(_rawXml);
        outer.Controls.Add(host);
        tab.Controls.Add(outer);
        return tab;
    }

    private Control BuildStatusStrip()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Padding = new Padding(10, 3, 14, 3),
            Tag = ThemeRole.Raised
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Tag = ThemeRole.Raised
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(_status, 0, 0);
        layout.Controls.Add(_footer, 1, 0);
        bar.Controls.Add(layout);
        return bar;
    }

    private RoundedPanel WrapGrid(DataGridView grid)
    {
        var host = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 9,
            SurfaceLevel = SurfaceLevel.Raised,
            Padding = new Padding(1)
        };
        host.Controls.Add(grid);
        return host;
    }

    private void ConfigureProfileGrid()
    {
        ConfigureGridBase(_profileGrid);
        _profileGrid.AutoGenerateColumns = false;
        _profileGrid.MultiSelect = true;
        _profileGrid.DataSource = _profiles;
        _profileGrid.SelectionChanged += (_, _) => ShowSelectedProfile();
        _profileGrid.Columns.Add(TextColumn("Profile", "DisplayName", 145));
        _profileGrid.Columns.Add(TextColumn("PL", "PowerLimitPercent", 42, "0'%';-0'%';0'%'"));
        _profileGrid.Columns.Add(TextColumn("Core", "CoreClockOffsetMHz", 56, "0' MHz';-0' MHz';0' MHz'"));
        _profileGrid.Columns.Add(TextColumn("mV", "VoltageOffsetMv", 50, "0' mV';-0' mV';0' mV'"));
        _profileGrid.Columns.Add(TextColumn("Mem XML", "MemoryClockMHz", 62, "0' MHz'"));
        _profileGrid.Columns.Add(TextColumn("Mem eff.", "CalculatedEffectiveMemoryClockMHz", 62, "0' MHz'"));
        _profileGrid.Columns.Add(TextColumn("Timing", "MemoryTimings", 50));
    }

    private void ConfigureCompareGrid()
    {
        ConfigureGridBase(_compareGrid);
        _compareGrid.MultiSelect = false;
        _compareGrid.Columns.Add(GridColumn("Setting", "Setting", 100, 82));
        _compareGrid.Columns.Add(GridColumn("Left", "Profile A", 78, 68));
        _compareGrid.Columns.Add(GridColumn("Right", "Profile B", 78, 68));
        _compareGrid.Columns.Add(GridColumn("Result", "Difference", 66, 58));
    }

    private void ConfigureRawGrid()
    {
        ConfigureGridBase(_rawGrid);
        _rawGrid.MultiSelect = false;
        _rawGrid.Columns.Add(GridColumn("Scope", "Scope", 52, 44));
        _rawGrid.Columns.Add(GridColumn("FeatureId", "Feature ID", 60, 52));
        _rawGrid.Columns.Add(GridColumn("FeatureEnabled", "Feature enabled", 68, 58));
        _rawGrid.Columns.Add(GridColumn("StateId", "State ID", 48, 42));
        _rawGrid.Columns.Add(GridColumn("StateEnabled", "State enabled", 68, 58));
        _rawGrid.Columns.Add(GridColumn("Value", "Value", 62, 54));
    }

    private static void ConfigureGridBase(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BorderStyle = BorderStyle.None;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToOrderColumns = true;
        grid.AllowUserToResizeColumns = true;
        grid.AllowUserToResizeRows = true;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.RowTemplate.Height = 32;
        grid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        _tabs.RefreshDpiMetrics();
        _brandHeader.Invalidate();
        _powerCard.RefreshDpiMetrics();
        _coreCard.RefreshDpiMetrics();
        _voltageCard.RefreshDpiMetrics();
        _memoryCard.RefreshDpiMetrics();
        _splitterInitialized = false;
        InitializeSplitter();
        NativeThemeHelper.Apply(this, _palette.IsDark);
        Invalidate(true);
    }

    private void OnShown(object? sender, EventArgs e)
    {
        _tabs.RefreshDpiMetrics();
        NativeThemeHelper.Apply(this, _palette.IsDark);
        LoadFiles(_session.OpenFiles.Where(File.Exists), showErrors: false);
        LoadStartupArguments(_startupArgs);
        RestoreSessionSelection();

        if (_session.SelectedTabIndex >= 0 && _session.SelectedTabIndex < _tabs.PageCount)
        {
            _tabs.SelectedIndex = _session.SelectedTabIndex;
        }

        if (!_migrateToCompactWindow && Enum.TryParse<FormWindowState>(_session.Window.State, ignoreCase: true, out var state) && state == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }

        // The SplitContainer can receive its first Layout event before the form has reached
        // its final DPI-scaled client size. In that state the 300-pixel inspector cannot yet be
        // applied and WinForms leaves Panel2 at its tiny construction-time width. Re-apply the
        // compact split one message-loop turn after Shown, when all handles and final bounds exist.
        BeginInvoke((Action)(() =>
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            _splitterInitialized = false;
            InitializeSplitter();
            ApplyInitialGridSizing();
        }));

        SetStatus(_profiles.Count == 0
            ? "Drop one or more Adrenalin XML profiles here."
            : $"Restored {_profiles.Count} profile(s) from the previous session.");
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            PersistDisplayedProfile(refreshGrid: false);
            SaveSession();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"The portable session could not be saved to the data folder.\n\n{ex.Message}",
                "Session save failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ApplyInitialWindowBounds()
    {
        if (_isFirstRun || _migrateToCompactWindow)
        {
            ApplyFirstRunWindowBounds();
            return;
        }

        var saved = _session.Window;
        var bounds = new Rectangle(
            saved.X,
            saved.Y,
            Math.Max(MinimumSize.Width, saved.Width),
            Math.Max(MinimumSize.Height, saved.Height));
        if (IsVisibleOnAnyScreen(bounds))
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
        }
        else
        {
            ApplyFirstRunWindowBounds();
        }
    }

    private void ApplyFirstRunWindowBounds()
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea
                          ?? new Rectangle(0, 0, 1600, 1000);
        const int desiredWidth = 1080;
        const int desiredHeight = 720;
        const int edgeMargin = 72;

        var availableWidth = Math.Max(MinimumSize.Width, workingArea.Width - edgeMargin);
        var availableHeight = Math.Max(MinimumSize.Height, workingArea.Height - edgeMargin);
        Width = Math.Min(desiredWidth, availableWidth);
        Height = Math.Min(desiredHeight, availableHeight);
        StartPosition = FormStartPosition.CenterScreen;
    }

    private static bool IsVisibleOnAnyScreen(Rectangle bounds)
    {
        return Screen.AllScreens.Any(screen =>
        {
            var intersection = Rectangle.Intersect(screen.WorkingArea, bounds);
            return intersection.Width >= 180 && intersection.Height >= 120;
        });
    }

    private void InitializeSplitter()
    {
        if (_splitterInitialized || _mainSplit.ClientSize.Width <= 0)
        {
            return;
        }

        var minimumLeft = DpiMetrics.Scale(_mainSplit, MinimumLibraryWidthLogical);
        var minimumRight = DpiMetrics.Scale(_mainSplit, MinimumInspectorWidthLogical);
        var available = _mainSplit.ClientSize.Width - _mainSplit.SplitterWidth;
        if (available < minimumLeft + minimumRight)
        {
            return;
        }

        var desiredRight = DpiMetrics.Scale(_mainSplit, _desiredInspectorWidthLogical);
        var maximumRight = Math.Max(minimumRight, available - minimumLeft);
        var rightWidth = Math.Clamp(desiredRight, minimumRight, maximumRight);

        _applyingSplitter = true;
        try
        {
            _mainSplit.Panel1MinSize = minimumLeft;
            _mainSplit.Panel2MinSize = minimumRight;
            _mainSplit.SplitterDistance = available - rightWidth;
            _splitterInitialized = true;
        }
        finally
        {
            _applyingSplitter = false;
        }
    }

    private void CaptureAdjustedInspectorWidth()
    {
        if (_applyingSplitter || !_splitterInitialized || _mainSplit.Panel2.ClientSize.Width <= 0)
        {
            return;
        }

        var scale = Math.Max(0.01f, DpiMetrics.Scale(_mainSplit));
        _desiredInspectorWidthLogical = Math.Clamp(
            (int)Math.Round(_mainSplit.Panel2.ClientSize.Width / scale),
            MinimumInspectorWidthLogical,
            900);
    }

    private void ChangeThemeFromSelector()
    {
        if (_suppressThemeChange || _themeSelector.SelectedIndex < 0 || _themeSelector.SelectedIndex >= ThemeCatalog.All.Count)
        {
            return;
        }

        _themeKind = ThemeCatalog.All[_themeSelector.SelectedIndex];
        ApplyTheme();
        SetStatus($"Theme changed to {ThemeCatalog.DisplayName(_themeKind)}. It will be restored automatically.");
    }

    private void ApplyTheme()
    {
        _palette = ThemeCatalog.Get(_themeKind);
        ThemeStyler.Apply(this, _palette);
        _mainSplit.BackColor = _palette.Background;
        _mainSplit.Panel1.BackColor = _palette.Background;
        _mainSplit.Panel2.BackColor = _palette.Background;
        UpdateComparison();
        if (IsHandleCreated)
        {
            NativeThemeHelper.Apply(this, _palette.IsDark);
        }
        Invalidate(true);
    }

    private void OpenFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open AMD Adrenalin tuning profiles",
            Filter = "XML profiles (*.xml)|*.xml|All files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
            InitialDirectory = PortablePaths.ProfilesDirectory,
            RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadFiles(dialog.FileNames, showErrors: true);
        }
    }

    private void OpenFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a folder containing exported Adrenalin XML profiles",
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true,
            InitialDirectory = PortablePaths.ProfilesDirectory
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadFiles(Directory.EnumerateFiles(dialog.SelectedPath, "*.xml", SearchOption.TopDirectoryOnly), showErrors: true);
        }
    }

    private void ExportCsv()
    {
        PersistDisplayedProfile(refreshGrid: true);
        if (_profiles.Count == 0)
        {
            MessageBox.Show(this, "Load at least one profile first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export profile summary",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"Adrenalin-profiles-{DateTime.Now:yyyy-MM-dd}.csv",
            AddExtension = true,
            InitialDirectory = PortablePaths.ExportsDirectory,
            RestoreDirectory = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            CsvExporter.ExportProfiles(dialog.FileName, _profiles);
            SetStatus($"Exported {_profiles.Count} profile(s) to {dialog.FileName}");
        }
    }

    private void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(PortablePaths.DataDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = PortablePaths.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open data folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveSelected()
    {
        PersistDisplayedProfile(refreshGrid: false);
        var selected = _profileGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem as AdrenalinProfile)
            .Where(profile => profile is not null)
            .Cast<AdrenalinProfile>()
            .Distinct()
            .ToList();

        foreach (var profile in selected)
        {
            _loadedPaths.Remove(profile.FilePath);
            _profiles.Remove(profile);
        }

        if (_displayedProfile is not null && selected.Contains(_displayedProfile))
        {
            _displayedProfile = null;
        }
        UpdateProfileCount();
        RefreshCompareSources();
        ShowSelectedProfile();
        SetStatus($"Removed {selected.Count} profile(s).");
    }

    private void ClearAll()
    {
        PersistDisplayedProfile(refreshGrid: false);
        _profiles.Clear();
        _loadedPaths.Clear();
        _displayedProfile = null;
        UpdateProfileCount();
        RefreshCompareSources();
        ShowSelectedProfile();
        SetStatus("Cleared all profiles.");
    }

    private void LoadStartupArguments(IEnumerable<string> args)
    {
        var files = new List<string>();
        foreach (var argument in args)
        {
            if (File.Exists(argument))
            {
                files.Add(argument);
            }
            else if (Directory.Exists(argument))
            {
                files.AddRange(Directory.EnumerateFiles(argument, "*.xml", SearchOption.TopDirectoryOnly));
            }
        }
        if (files.Count > 0)
        {
            LoadFiles(files, showErrors: true);
        }
    }

    private void LoadFiles(IEnumerable<string> paths, bool showErrors)
    {
        var loaded = 0;
        var errors = new List<string>();
        foreach (var path in paths.Where(path => string.Equals(Path.GetExtension(path), ".xml", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!_loadedPaths.Add(fullPath))
                {
                    continue;
                }

                var profile = _parser.Parse(fullPath);
                var metadata = _metadataStore.Load(fullPath);
                if (!string.IsNullOrWhiteSpace(metadata.DisplayName))
                {
                    profile.DisplayName = metadata.DisplayName;
                }
                profile.Notes = metadata.Notes;
                _profiles.Add(profile);
                loaded++;
            }
            catch (Exception ex)
            {
                try { _loadedPaths.Remove(Path.GetFullPath(path)); } catch { }
                errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        UpdateProfileCount();
        RefreshCompareSources();
        ApplySavedRowHeights(_profileGrid, ProfileGridKey);
        if (_profiles.Count > 0 && _profileGrid.SelectedRows.Count == 0)
        {
            _profileGrid.Rows[0].Selected = true;
            _profileGrid.CurrentCell = _profileGrid.Rows[0].Cells[0];
        }

        if (loaded > 0)
        {
            SetStatus($"Loaded {loaded} new profile(s). Total: {_profiles.Count}.");
        }
        if (showErrors && errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Some profiles could not be loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateProfileCount()
    {
        _profileCountLabel.Text = _profiles.Count == 1 ? "1 profile" : $"{_profiles.Count} profiles";
    }

    private void ShowSelectedProfile()
    {
        if (_savingMetadata)
        {
            return;
        }

        var selected = SelectedProfile();
        if (!ReferenceEquals(selected, _displayedProfile))
        {
            PersistDisplayedProfile(refreshGrid: true);
            _displayedProfile = selected;
        }

        var enabled = selected is not null;
        _nameBox.Enabled = enabled;
        _notesBox.Enabled = enabled;

        if (selected is null)
        {
            _powerCard.Value = "—";
            _powerCard.Detail = "No profile\nselected";
            _coreCard.Value = "—";
            _coreCard.Detail = "No profile\nselected";
            _voltageCard.Value = "—";
            _voltageCard.Detail = "No profile\nselected";
            _memoryCard.Value = "—";
            _memoryCard.Detail = "No profile\nselected";
            _fileValue.Text = "—";
            _gpuValue.Text = "—";
            _memoryValue.Text = "—";
            _timingValue.Text = "—";
            _fanValue.Text = "—";
            _zeroRpmValue.Text = "—";
            _nameBox.Text = string.Empty;
            _notesBox.Text = string.Empty;
            _rawXml.Text = string.Empty;
            _rawGrid.Rows.Clear();
            return;
        }

        _powerCard.Value = Format(selected.PowerLimitPercent, "%");
        _powerCard.Detail = "Board power\ntarget";
        _coreCard.Value = Format(selected.CoreClockOffsetMHz, " MHz");
        _coreCard.Detail = "Maximum GPU\noffset";
        _voltageCard.Value = Format(selected.VoltageOffsetMv, " mV");
        _voltageCard.Detail = "Global voltage\noffset";
        _memoryCard.Value = FormatPlain(selected.CalculatedEffectiveMemoryClockMHz, " MHz");
        _memoryCard.Detail = $"Stored XML\n{FormatPlain(selected.MemoryClockMHz, " MHz")}";
        _fileValue.Text = selected.FilePath;
        _gpuValue.Text = selected.GpuLabel;
        _memoryValue.Text = FormatPlain(selected.MemoryClockMHz, " MHz");
        _timingValue.Text = selected.MemoryTimings;
        _fanValue.Text = selected.FanMode;
        _zeroRpmValue.Text = selected.ZeroRpm switch { true => "On", false => "Off", null => "Unknown / not enabled" };
        _nameBox.Text = selected.DisplayName;
        _notesBox.Text = selected.Notes;
        _rawXml.Text = selected.RawXml;

        _rawGrid.Rows.Clear();
        foreach (var feature in selected.RawFeatures)
        {
            _rawGrid.Rows.Add(feature.Scope, feature.FeatureId, feature.FeatureEnabled, feature.StateId, feature.StateEnabled, feature.Value);
        }
        ApplySavedRowHeights(_rawGrid, RawGridKey);
    }

    private void SaveMetadata()
    {
        if (PersistDisplayedProfile(refreshGrid: true))
        {
            SetStatus($"Saved metadata for {_displayedProfile?.DisplayName}.");
        }
    }

    private bool PersistDisplayedProfile(bool refreshGrid)
    {
        var profile = _displayedProfile;
        if (profile is null || _savingMetadata)
        {
            return false;
        }

        _savingMetadata = true;
        try
        {
            var displayName = string.IsNullOrWhiteSpace(_nameBox.Text)
                ? Path.GetFileNameWithoutExtension(profile.FileName)
                : _nameBox.Text.Trim();
            var notes = _notesBox.Text;
            var changed = !string.Equals(profile.DisplayName, displayName, StringComparison.Ordinal) ||
                          !string.Equals(profile.Notes, notes, StringComparison.Ordinal);
            profile.DisplayName = displayName;
            profile.Notes = notes;
            _metadataStore.Save(profile.FilePath, new ProfileMetadata
            {
                DisplayName = profile.DisplayName,
                Notes = profile.Notes
            });

            if (changed && refreshGrid)
            {
                var index = _profiles.IndexOf(profile);
                if (index >= 0)
                {
                    _profiles.ResetItem(index);
                }
                RefreshCompareSources();
            }
            return true;
        }
        finally
        {
            _savingMetadata = false;
        }
    }

    private void RefreshCompareSources(AdrenalinProfile? preserve = null)
    {
        var left = preserve ?? _compareLeft.SelectedItem as AdrenalinProfile;
        var right = _compareRight.SelectedItem as AdrenalinProfile;
        _compareLeft.BeginUpdate();
        _compareRight.BeginUpdate();
        _compareLeft.Items.Clear();
        _compareRight.Items.Clear();
        foreach (var profile in _profiles)
        {
            _compareLeft.Items.Add(profile);
            _compareRight.Items.Add(profile);
        }
        _compareLeft.EndUpdate();
        _compareRight.EndUpdate();

        if (left is not null && _profiles.Contains(left))
        {
            _compareLeft.SelectedItem = left;
        }
        else if (_profiles.Count > 0)
        {
            _compareLeft.SelectedIndex = 0;
        }

        if (right is not null && _profiles.Contains(right))
        {
            _compareRight.SelectedItem = right;
        }
        else if (_profiles.Count > 1)
        {
            _compareRight.SelectedIndex = 1;
        }
        else if (_profiles.Count == 1)
        {
            _compareRight.SelectedIndex = 0;
        }
        UpdateComparison();
    }

    private void UpdateComparison()
    {
        _compareGrid.Rows.Clear();
        var left = _compareLeft.SelectedItem as AdrenalinProfile;
        var right = _compareRight.SelectedItem as AdrenalinProfile;
        if (left is null || right is null)
        {
            return;
        }

        AddComparison("GPU device", left.DeviceId, right.DeviceId);
        AddComparison("GPU revision", left.RevisionId, right.RevisionId);
        AddNumericComparison("Power limit", left.PowerLimitPercent, right.PowerLimitPercent, "%", signed: true);
        AddNumericComparison("Max frequency offset", left.CoreClockOffsetMHz, right.CoreClockOffsetMHz, " MHz", signed: true);
        AddNumericComparison("Voltage offset", left.VoltageOffsetMv, right.VoltageOffsetMv, " mV", signed: true);
        AddNumericComparison("Memory clock (stored XML)", left.MemoryClockMHz, right.MemoryClockMHz, " MHz", signed: false);
        AddNumericComparison("Calculated effective clock", left.CalculatedEffectiveMemoryClockMHz, right.CalculatedEffectiveMemoryClockMHz, " MHz", signed: false);
        AddComparison("Memory timings", left.MemoryTimings, right.MemoryTimings);
        AddComparison("Fan mode", left.FanMode, right.FanMode);
        AddComparison("Zero RPM", FormatBool(left.ZeroRpm), FormatBool(right.ZeroRpm));
        ApplySavedRowHeights(_compareGrid, CompareGridKey);
    }

    private void AddNumericComparison(
        string setting,
        int? leftValue,
        int? rightValue,
        string suffix,
        bool signed)
    {
        var leftText = signed ? Format(leftValue, suffix) : FormatPlain(leftValue, suffix);
        var rightText = signed ? Format(rightValue, suffix) : FormatPlain(rightValue, suffix);

        if (!leftValue.HasValue || !rightValue.HasValue || leftValue.Value == rightValue.Value)
        {
            AddComparison(setting, leftText, rightText);
            return;
        }

        // Lower numeric values are presented as the greener result and higher values as red.
        // This intentionally means that a more-negative offset, for example -250 versus -200,
        // is green because -250 is numerically lower.
        var leftIsLower = leftValue.Value < rightValue.Value;
        var rowIndex = _compareGrid.Rows.Add(
            setting,
            leftText,
            rightText,
            leftIsLower ? "A lower" : "B lower");
        var row = _compareGrid.Rows[rowIndex];

        ApplyComparisonTint(row.Cells[1], lower: leftIsLower);
        ApplyComparisonTint(row.Cells[2], lower: !leftIsLower);

        var resultCell = row.Cells[3];
        resultCell.Style.ForeColor = _palette.Positive;
        resultCell.Style.BackColor = Blend(_palette.Surface, _palette.Positive, _palette.IsDark ? 0.16f : 0.10f);
        resultCell.Style.SelectionBackColor = Blend(_palette.Selection, _palette.Positive, 0.22f);
        resultCell.Style.SelectionForeColor = Color.White;
        resultCell.Style.Font = new Font(_compareGrid.Font, FontStyle.Bold);
    }

    private void AddComparison(string setting, string? left, string? right)
    {
        var same = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        var rowIndex = _compareGrid.Rows.Add(setting, left ?? "—", right ?? "—", same ? "Same" : "Changed");
        var cell = _compareGrid.Rows[rowIndex].Cells[3];
        cell.Style.ForeColor = same ? _palette.Positive : _palette.Warning;
        cell.Style.Font = new Font(_compareGrid.Font, FontStyle.Bold);
    }

    private void ApplyComparisonTint(DataGridViewCell cell, bool lower)
    {
        var tone = lower ? _palette.Positive : _palette.Danger;
        cell.Style.ForeColor = tone;
        cell.Style.BackColor = Blend(_palette.Surface, tone, _palette.IsDark ? 0.18f : 0.11f);
        cell.Style.SelectionBackColor = Blend(_palette.Selection, tone, 0.25f);
        cell.Style.SelectionForeColor = Color.White;
        cell.Style.Font = new Font(_compareGrid.Font, FontStyle.Bold);
    }

    private static Color Blend(Color background, Color foreground, float foregroundAmount)
    {
        var amount = Math.Clamp(foregroundAmount, 0f, 1f);
        var inverse = 1f - amount;
        return Color.FromArgb(
            255,
            (int)Math.Round((background.R * inverse) + (foreground.R * amount)),
            (int)Math.Round((background.G * inverse) + (foreground.G * amount)),
            (int)Math.Round((background.B * inverse) + (foreground.B * amount)));
    }

    private void RestoreSessionSelection()
    {
        SelectProfileByPath(_session.SelectedProfilePath);
        SelectComboProfileByPath(_compareLeft, _session.CompareLeftPath);
        SelectComboProfileByPath(_compareRight, _session.CompareRightPath);
        UpdateComparison();
    }

    private void SelectProfileByPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        foreach (DataGridViewRow row in _profileGrid.Rows)
        {
            if (row.DataBoundItem is AdrenalinProfile profile && string.Equals(profile.FilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                _profileGrid.ClearSelection();
                row.Selected = true;
                _profileGrid.CurrentCell = row.Cells[0];
                return;
            }
        }
    }

    private static void SelectComboProfileByPath(ComboBox combo, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        foreach (var item in combo.Items)
        {
            if (item is AdrenalinProfile profile && string.Equals(profile.FilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = profile;
                return;
            }
        }
    }

    private void SaveSession()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _session.SchemaVersion = CurrentSessionSchema;
        _session.Theme = _themeKind.ToString();
        _session.Window = new WindowLayoutState
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            State = WindowState == FormWindowState.Minimized ? FormWindowState.Normal.ToString() : WindowState.ToString()
        };
        _session.MainSplitterDistance = _splitterInitialized ? _mainSplit.SplitterDistance : _session.MainSplitterDistance;
        CaptureAdjustedInspectorWidth();
        _session.MainRightPaneWidthLogical = _desiredInspectorWidthLogical;
        _session.SelectedTabIndex = _tabs.SelectedIndex;
        _session.OpenFiles = _profiles.Select(profile => profile.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _session.SelectedProfilePath = SelectedProfile()?.FilePath;
        _session.CompareLeftPath = (_compareLeft.SelectedItem as AdrenalinProfile)?.FilePath;
        _session.CompareRightPath = (_compareRight.SelectedItem as AdrenalinProfile)?.FilePath;
        SaveGridLayoutWhenReady(ProfileGridKey, _profileGrid, _initialProfileGridFitPending);
        SaveGridLayoutWhenReady(CompareGridKey, _compareGrid, _initialCompareGridFitPending);
        SaveGridLayoutWhenReady(RawGridKey, _rawGrid, _initialRawGridFitPending);
        _sessionStore.Save(_session);
    }

    private void SaveGridLayoutWhenReady(string key, DataGridView grid, bool initialFitPending)
    {
        if (initialFitPending)
        {
            _session.Grids.Remove(key);
            return;
        }

        _session.Grids[key] = CaptureGridLayout(grid);
    }

    private void RestoreGridLayouts()
    {
        ApplyGridLayout(_profileGrid, GetGridState(ProfileGridKey));
        ApplyGridLayout(_compareGrid, GetGridState(CompareGridKey));
        ApplyGridLayout(_rawGrid, GetGridState(RawGridKey));
    }

    private void ApplyInitialGridSizing()
    {
        if (_initialProfileGridFitPending)
        {
            _initialProfileGridFitPending = !FitGridColumns(_profileGrid, [26, 10, 15, 13, 14, 14, 12]);
        }

        if (_initialCompareGridFitPending)
        {
            _initialCompareGridFitPending = !FitGridColumnsWithPracticalMinimums(
                _compareGrid,
                [100, 78, 78, 66],
                [32, 24, 24, 20]);
        }

        if (_initialRawGridFitPending)
        {
            _initialRawGridFitPending = !FitGridColumnsWithPracticalMinimums(
                _rawGrid,
                [52, 60, 68, 48, 68, 62],
                [15, 17, 19, 14, 19, 16]);
        }
    }

    private static bool FitGridColumns(DataGridView grid, IReadOnlyList<int> weights)
    {
        var visibleColumns = grid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .OrderBy(column => column.DisplayIndex)
            .ToList();
        if (visibleColumns.Count == 0 || visibleColumns.Count != weights.Count)
        {
            return false;
        }

        var scrollbarAllowance = SystemInformation.VerticalScrollBarWidth + 4;
        var available = grid.ClientSize.Width - scrollbarAllowance;
        if (available <= 0)
        {
            return false;
        }

        var minimumTotal = visibleColumns.Sum(column => column.MinimumWidth);
        if (available <= minimumTotal)
        {
            foreach (var column in visibleColumns)
            {
                column.Width = column.MinimumWidth;
            }
            return true;
        }

        var distributable = available - minimumTotal;
        var totalWeight = Math.Max(1, weights.Sum());
        var assigned = 0;
        for (var index = 0; index < visibleColumns.Count; index++)
        {
            var column = visibleColumns[index];
            var width = index == visibleColumns.Count - 1
                ? available - assigned
                : column.MinimumWidth + (int)Math.Round(distributable * (weights[index] / (double)totalWeight));
            width = Math.Max(column.MinimumWidth, width);
            column.Width = width;
            assigned += width;
        }
        return true;
    }

    private static bool FitGridColumnsWithPracticalMinimums(
        DataGridView grid,
        IReadOnlyList<int> minimumLogicalWidths,
        IReadOnlyList<int> weights)
    {
        var visibleColumns = grid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .OrderBy(column => column.DisplayIndex)
            .ToList();
        if (visibleColumns.Count == 0
            || visibleColumns.Count != minimumLogicalWidths.Count
            || visibleColumns.Count != weights.Count)
        {
            return false;
        }

        var scrollbarAllowance = SystemInformation.VerticalScrollBarWidth + 4;
        var available = grid.ClientSize.Width - scrollbarAllowance;
        if (available <= 0)
        {
            return false;
        }

        var practicalWidths = visibleColumns
            .Select((column, index) => Math.Max(
                column.MinimumWidth,
                DpiMetrics.Scale(grid, minimumLogicalWidths[index])))
            .ToArray();
        var practicalTotal = practicalWidths.Sum();

        // Never squeeze Compare/Features into unreadable slivers. A horizontal scrollbar is
        // preferable at compact inspector widths. If more room exists, distribute it cleanly.
        if (available <= practicalTotal)
        {
            for (var index = 0; index < visibleColumns.Count; index++)
            {
                visibleColumns[index].Width = practicalWidths[index];
            }
            return true;
        }

        var distributable = available - practicalTotal;
        var totalWeight = Math.Max(1, weights.Sum());
        var assigned = 0;
        for (var index = 0; index < visibleColumns.Count; index++)
        {
            var extra = index == visibleColumns.Count - 1
                ? distributable - assigned
                : (int)Math.Round(distributable * (weights[index] / (double)totalWeight));
            extra = Math.Max(0, extra);
            visibleColumns[index].Width = practicalWidths[index] + extra;
            assigned += extra;
        }
        return true;
    }

    private GridLayoutState? GetGridState(string key)
    {
        return _session.Grids.TryGetValue(key, out var state) ? state : null;
    }

    private static GridLayoutState CaptureGridLayout(DataGridView grid)
    {
        return new GridLayoutState
        {
            RowHeight = grid.Rows.Count > 0 ? grid.Rows[0].Height : grid.RowTemplate.Height,
            HeaderHeight = grid.ColumnHeadersHeight,
            RowHeights = grid.Rows.Cast<DataGridViewRow>().Select(row => row.Height).ToList(),
            Columns = grid.Columns.Cast<DataGridViewColumn>()
                .Select(column => new GridColumnState
                {
                    Name = column.Name,
                    Width = column.Width,
                    DisplayIndex = column.DisplayIndex,
                    Visible = column.Visible
                })
                .ToList()
        };
    }

    private static void ApplyGridLayout(DataGridView grid, GridLayoutState? state)
    {
        if (state is null)
        {
            return;
        }

        grid.RowTemplate.Height = Math.Clamp(state.RowHeight, 18, 120);
        grid.ColumnHeadersHeight = Math.Clamp(state.HeaderHeight, 22, 120);
        for (var index = 0; index < grid.Rows.Count && index < state.RowHeights.Count; index++)
        {
            grid.Rows[index].Height = Math.Clamp(state.RowHeights[index], 18, 120);
        }
        foreach (var saved in state.Columns)
        {
            var column = FindColumn(grid, saved.Name);
            if (column is null)
            {
                continue;
            }
            column.Width = Math.Clamp(saved.Width, column.MinimumWidth, 900);
            column.Visible = saved.Visible;
        }

        var ordered = state.Columns
            .Where(saved => FindColumn(grid, saved.Name) is not null)
            .OrderBy(saved => saved.DisplayIndex)
            .ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var column = FindColumn(grid, ordered[index].Name);
            if (column is not null)
            {
                column.DisplayIndex = Math.Min(index, grid.Columns.Count - 1);
            }
        }
    }


    private static DataGridViewColumn? FindColumn(DataGridView grid, string name)
    {
        return grid.Columns.Cast<DataGridViewColumn>()
            .FirstOrDefault(column => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplySavedRowHeights(DataGridView grid, string key)
    {
        var state = GetGridState(key);
        if (state is null)
        {
            return;
        }

        for (var index = 0; index < grid.Rows.Count; index++)
        {
            var savedHeight = index < state.RowHeights.Count ? state.RowHeights[index] : state.RowHeight;
            grid.Rows[index].Height = Math.Clamp(savedHeight, 18, 120);
        }
    }

    private void OpenSelectedLocation()
    {
        var profile = SelectedProfile();
        if (profile is null)
        {
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{profile.FilePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private AdrenalinProfile? SelectedProfile() => _profileGrid.CurrentRow?.DataBoundItem as AdrenalinProfile;

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            var paths = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            e.Effect = paths?.Any(path => File.Exists(path) || Directory.Exists(path)) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        var paths = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? [];
        var files = new List<string>();
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                files.Add(path);
            }
            else if (Directory.Exists(path))
            {
                files.AddRange(Directory.EnumerateFiles(path, "*.xml", SearchOption.TopDirectoryOnly));
            }
        }
        LoadFiles(files, showErrors: true);
    }

    private void SetStatus(string text) => _status.Text = text;

    private static RoundedButton CreateButton(string text, RoundedButtonStyle style, EventHandler handler)
    {
        var button = new RoundedButton { Text = text, ButtonStyle = style };
        button.Click += handler;
        return button;
    }

    private static Label CreateLabel(string text, ThemeRole role, float size, FontStyle style = FontStyle.Regular)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", size, style),
            Tag = role,
            Margin = new Padding(0, 0, 0, 4)
        };
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            Text = "—",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold),
            Tag = ThemeRole.Value,
            MaximumSize = new Size(820, 0),
            Margin = new Padding(0, 0, 0, 4)
        };
    }

    private static void AddCompactSettingRow(TableLayoutPanel table, int row, string name, Label value)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var key = CreateLabel(name, ThemeRole.Muted, 8.6f);
        key.AutoSize = false;
        key.Dock = DockStyle.Fill;
        key.TextAlign = ContentAlignment.MiddleLeft;
        key.Margin = Padding.Empty;

        value.AutoSize = false;
        value.Dock = DockStyle.Fill;
        value.AutoEllipsis = true;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.Margin = Padding.Empty;

        table.Controls.Add(key, 0, row);
        table.Controls.Add(value, 1, row);
    }

    private static void AddSettingRow(TableLayoutPanel table, int row, string name, Label value)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var key = CreateLabel(name, ThemeRole.Muted, 9f);
        key.Anchor = AnchorStyles.Left;
        key.Padding = new Padding(0, 7, 0, 7);
        value.Anchor = AnchorStyles.Left;
        value.Padding = new Padding(0, 7, 0, 7);
        table.Controls.Add(key, 0, row);
        table.Controls.Add(value, 1, row);
    }

    private static void StyleTextBox(TextBox box, bool multiline)
    {
        box.Multiline = multiline;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Segoe UI", 9.5f);
    }

    private static void StyleComboBox(ComboBox box)
    {
        box.Dock = DockStyle.Fill;
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.FlatStyle = FlatStyle.Flat;
    }

    private static Panel NewTab(string title) => new()
    {
        AccessibleName = title,
        Padding = Padding.Empty,
        Margin = Padding.Empty,
        Tag = ThemeRole.Surface
    };

    private static DataGridViewTextBoxColumn TextColumn(string header, string property, int width, string? format = null)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = property,
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            MinimumWidth = 42,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = format is null ? new DataGridViewCellStyle() : new DataGridViewCellStyle { Format = format }
        };
    }

    private static DataGridViewTextBoxColumn GridColumn(string name, string header, int width, int minimumWidth = 50)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            Width = width,
            MinimumWidth = minimumWidth,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
    }

    private static string Format(int? value, string suffix) => value is null ? "—" : $"{value.Value:+0;-0;0}{suffix}";
    private static string FormatPlain(int? value, string suffix) => value is null ? "—" : $"{value.Value:0}{suffix}";
    private static string FormatBool(bool? value) => value switch { true => "On", false => "Off", null => "Unknown" };
}
