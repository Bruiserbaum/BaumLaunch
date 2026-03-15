using BaumLaunch.Controls;
using BaumLaunch.Models;
using BaumLaunch.Services;

namespace BaumLaunch;

public sealed class MainForm : Form
{
    // ── Data ────────────────────────────────────────────────────────────────
    private List<AppEntry> _entries = AppCatalog.GetAll();
    private string _activeCategory  = "All";
    private bool   _isChecking      = false;
    private DateTime _lastChecked   = DateTime.MinValue;
    private bool   _exitRequested   = false;
    private AppSettings _settings   = AppSettings.Load();

    // ── Timers ───────────────────────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _checkTimer;
    private readonly System.Windows.Forms.Timer _scheduleTimer;
    private DateTime _lastScheduledRun = DateTime.MinValue;
    private bool _appUpdateCheckInProgress = false;

    // ── Tray ─────────────────────────────────────────────────────────────────
    private readonly NotifyIcon _trayIcon;

    // ── UI panels ────────────────────────────────────────────────────────────
    private readonly Panel      _titleBar;
    private readonly Panel      _filterBar;
    private readonly Panel      _searchBar;
    private readonly Panel      _statusBar;
    private readonly Panel      _scrollPanel;
    private readonly Panel      _bottomBar;

    // ── Status bar labels ────────────────────────────────────────────────────
    private readonly Label _lblStatusInfo;
    private readonly Label _lblChecking;

    // ── Bottom bar controls ───────────────────────────────────────────────────
    private readonly Button _btnSelectAll;
    private readonly Button _btnDeselectAll;
    private readonly Button _btnInstallSelected;
    private readonly Button _btnUpdateSelected;
    private readonly Button _btnUpdateAll;
    private readonly Button _btnExport;
    private readonly Button _btnImport;
    private readonly Button _btnCheckNow;
    private readonly Button _btnSettings;

    // ── Filter buttons & search ───────────────────────────────────────────────
    private readonly List<Button> _filterButtons = new();
    private TextBox? _searchBox;
    private string   _searchFilter = "";

    // ── Window drag state ────────────────────────────────────────────────────
    private Point _dragStart;
    private bool  _dragging;

    // ── Log overlay panel ────────────────────────────────────────────────────
    private Panel?       _logOverlay;
    private RichTextBox? _logBox;

    // ── Startup mode ─────────────────────────────────────────────────────────
    private readonly bool _startMinimized;

    public MainForm(bool startMinimized = false)
    {
        _startMinimized = startMinimized;

        // ── Form setup ──────────────────────────────────────────────────────
        Text            = "BaumLaunch";
        Size            = new Size(980, 680);
        MinimumSize     = new Size(860, 520);
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = AppTheme.BgMain;
        StartPosition   = FormStartPosition.CenterScreen;
        DoubleBuffered  = true;

        // ── Title bar ───────────────────────────────────────────────────────
        _titleBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 40,
            BackColor = AppTheme.BgDeep,
        };
        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp   += TitleBar_MouseUp;
        _titleBar.Paint     += TitleBar_Paint;

        var btnClose = MakeTitleBarButton("✕", Color.FromArgb(220, 60, 60));
        btnClose.Click += (_, _) => { _exitRequested = false; Close(); };

        var btnMin = MakeTitleBarButton("–", Color.FromArgb(60, 60, 90));
        btnMin.Click += (_, _) => WindowState = FormWindowState.Minimized;

        var btnTray = MakeTitleBarButton("⬓", Color.FromArgb(60, 60, 90));
        btnTray.Click += (_, _) =>
        {
            Hide();
            _trayIcon?.ShowBalloonTip(1500, "BaumLaunch", "Minimized to tray.", ToolTipIcon.None);
        };

        _titleBar.Controls.Add(btnClose);
        _titleBar.Controls.Add(btnMin);
        _titleBar.Controls.Add(btnTray);

        // Position buttons flush-right; recalculate whenever the title bar is resized
        // (Anchor cannot be used here because the panel has no width yet at construction time)
        void PositionTitleBarButtons()
        {
            int w = _titleBar.Width;
            btnClose.Location = new Point(w - 44,  0);
            btnMin.Location   = new Point(w - 88,  0);
            btnTray.Location  = new Point(w - 132, 0);
        }
        _titleBar.SizeChanged += (_, _) => PositionTitleBarButtons();
        // Fallback: also reposition when the form itself resizes
        SizeChanged += (_, _) => PositionTitleBarButtons();

        // ── Filter bar ──────────────────────────────────────────────────────
        _filterBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 38,
            BackColor = AppTheme.BgPanel,
            Padding   = new Padding(4, 0, 4, 0),
        };
        _filterBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawLine(pen, 0, _filterBar.Height - 1, _filterBar.Width, _filterBar.Height - 1);
        };

        string[] categories = { "All", "AI Tools", "Browsers", "Runtimes", "Dev Tools", "Media & Tools", "Game Launchers", "Communication", "System Tools", "WinGet Updates" };
        int bx = 6;
        foreach (var cat in categories)
        {
            bool isUpdatesTab = cat == "WinGet Updates";
            string btnText = isUpdatesTab ? "⬆ Updates" : cat;
            // Measure with the bold (active) font — it's wider, so switching fonts never clips the text
            int btnW = TextRenderer.MeasureText(btnText, AppTheme.FontBold).Width + 22;
            var btn = new Button
            {
                Text      = btnText,
                AutoSize  = false,
                Size      = new Size(btnW, 28),
                Location  = new Point(bx, 5),
                Font      = AppTheme.FontSmall,
                FlatStyle = FlatStyle.Flat,
                ForeColor = isUpdatesTab ? AppTheme.Accent : AppTheme.TextSecondary,
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                Tag       = cat,
            };
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor = AppTheme.BgCard;
            btn.Click += FilterButton_Click;
            _filterButtons.Add(btn);
            _filterBar.Controls.Add(btn);
            bx += btn.Width + 2;
        }

        // Ensure the window is wide enough to show every tab without clipping
        int tabsMinWidth = bx + 16; // right margin
        if (MinimumSize.Width < tabsMinWidth)
            MinimumSize = new Size(tabsMinWidth, MinimumSize.Height);
        if (Width < tabsMinWidth)
            Size = new Size(tabsMinWidth + 80, Height); // extra breathing room

        // ── Search bar (dedicated row below filter tabs) ─────────────────────
        _searchBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 34,
            BackColor = AppTheme.BgPanel,
        };
        _searchBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawLine(pen, 0, _searchBar.Height - 1, _searchBar.Width, _searchBar.Height - 1);
        };

        _searchBox = new TextBox
        {
            Size        = new Size(240, 22),
            Font        = AppTheme.FontSmall,
            BackColor   = AppTheme.BgCard,
            ForeColor   = AppTheme.TextSecondary,
            BorderStyle = BorderStyle.None,
            Text        = "Search apps...",
        };
        var searchPanel = new Panel
        {
            Size      = new Size(246, 26),
            BackColor = AppTheme.BgCard,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
        };
        searchPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
        };
        _searchBox.Location = new Point(6, 2);
        searchPanel.Controls.Add(_searchBox);
        _searchBar.Controls.Add(searchPanel);
        void PositionSearchBox() =>
            searchPanel.Location = new Point(_searchBar.Width - searchPanel.Width - 8,
                                             (_searchBar.Height - searchPanel.Height) / 2);
        PositionSearchBox();
        _searchBar.SizeChanged += (_, _) => PositionSearchBox();

        // Placeholder text behaviour
        _searchBox.GotFocus += (_, _) =>
        {
            if (_searchBox.Text == "Search apps...") { _searchBox.Text = ""; _searchBox.ForeColor = AppTheme.TextPrimary; }
        };
        _searchBox.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_searchBox.Text)) { _searchBox.Text = "Search apps..."; _searchBox.ForeColor = AppTheme.TextSecondary; }
        };
        _searchBox.TextChanged += (_, _) =>
        {
            _searchFilter = _searchBox.Text == "Search apps..." ? "" : _searchBox.Text.Trim();
            RebuildRows();
        };

        // ── Status bar ──────────────────────────────────────────────────────
        _statusBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 24,
            BackColor = AppTheme.BgDeep,
            Padding   = new Padding(10, 0, 10, 0),
        };

        _lblStatusInfo = new Label
        {
            AutoSize  = false,
            Size      = new Size(600, 24),
            Location  = new Point(10, 0),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "Ready",
        };

        _lblChecking = new Label
        {
            AutoSize  = false,
            Size      = new Size(200, 24),
            Location  = new Point(760, 0),
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.Accent,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight,
            Text      = "",
            Visible   = false,
        };

        _statusBar.Controls.Add(_lblStatusInfo);
        _statusBar.Controls.Add(_lblChecking);

        // ── Bottom toolbar ───────────────────────────────────────────────────
        _bottomBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 50,
            BackColor = AppTheme.BgDeep,
            Padding   = new Padding(8, 0, 8, 0),
        };
        _bottomBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawLine(pen, 0, 0, _bottomBar.Width, 0);
        };

        _btnSelectAll      = MakeToolbarButton("Select All",     AppTheme.BgCard,  AppTheme.TextSecondary);
        _btnDeselectAll    = MakeToolbarButton("Deselect All",   AppTheme.BgCard,  AppTheme.TextSecondary);
        _btnInstallSelected = MakeToolbarButton("Install Selected", AppTheme.Accent, AppTheme.TextPrimary);
        _btnUpdateSelected  = MakeToolbarButton("Update Selected",  AppTheme.Accent, AppTheme.TextPrimary);
        _btnUpdateAll       = MakeToolbarButton("Update All (0)",   AppTheme.Accent, AppTheme.TextPrimary);
        _btnExport         = MakeToolbarButton("Export Profile", AppTheme.BgCard,  AppTheme.TextSecondary);
        _btnImport         = MakeToolbarButton("Import Profile", AppTheme.BgCard,  AppTheme.TextSecondary);
        _btnCheckNow       = MakeToolbarButton("Check Now",      AppTheme.Accent,  AppTheme.TextPrimary);
        _btnSettings       = MakeToolbarButton("⚙ Settings",    AppTheme.BgCard,  AppTheme.TextSecondary);

        _btnSelectAll.Click       += (_, _) => { foreach (var e in _entries) e.IsSelected = true;  RebuildRows(); };
        _btnDeselectAll.Click     += (_, _) => { foreach (var e in _entries) e.IsSelected = false; RebuildRows(); };
        _btnInstallSelected.Click += async (_, _) => await RunBatchAsync(false);
        _btnUpdateSelected.Click  += async (_, _) => await RunBatchAsync(true, selectedOnly: true);
        _btnUpdateAll.Click       += async (_, _) => await RunBatchAsync(true, selectedOnly: false);
        _btnExport.Click          += ExportProfile_Click;
        _btnImport.Click          += ImportProfile_Click;
        _btnCheckNow.Click        += async (_, _) => await RefreshStatusAsync();
        _btnSettings.Click        += (_, _) => OpenSettings();

        // Layout bottom bar left-to-right
        LayoutBottomBar();
        _bottomBar.SizeChanged += (_, _) => LayoutBottomBar();

        // ── Scroll panel (content area) ─────────────────────────────────────
        _scrollPanel = new Panel
        {
            Dock        = DockStyle.Fill,
            AutoScroll  = true,
            BackColor   = AppTheme.BgMain,
        };

        // Order matters for DockStyle.Top stacking (bottom → top order of adding)
        Controls.Add(_scrollPanel);
        Controls.Add(_bottomBar);
        Controls.Add(_statusBar);
        Controls.Add(_searchBar);
        Controls.Add(_filterBar);
        Controls.Add(_titleBar);

        // ── Tray icon ────────────────────────────────────────────────────────
        var ctxMenu = new ContextMenuStrip();
        ctxMenu.BackColor = AppTheme.BgPanel;
        ctxMenu.ForeColor = AppTheme.TextPrimary;
        ctxMenu.Font      = AppTheme.FontBody;
        ctxMenu.Items.Add("Open BaumLaunch",      null, (_, _) => ShowMainWindow());
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("Check for Updates",   null, async (_, _) => await RefreshStatusAsync());
        ctxMenu.Items.Add("Update All",          null, async (_, _) => await RunBatchAsync(true, selectedOnly: false));
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("Check for App Update",null, async (_, _) => await CheckForAppUpdateAsync(force: true));
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("Settings",            null, (_, _) => { ShowMainWindow(); OpenSettings(); });
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("Exit",                null, (_, _) => { _exitRequested = true; Application.Exit(); });

        // Use the same generated icon everywhere: tray, taskbar, alt-tab, title bar
        var appIcon = GenerateTrayIcon(false);
        this.Icon   = appIcon;

        _trayIcon = new NotifyIcon
        {
            Text        = "BaumLaunch",
            Icon        = appIcon,
            ContextMenuStrip = ctxMenu,
            Visible     = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();

        // ── Timer ────────────────────────────────────────────────────────────
        _checkTimer = new System.Windows.Forms.Timer { Interval = SettingsToTimerInterval(_settings) };
        _checkTimer.Tick += async (_, _) =>
        {
            await RefreshStatusAsync();
            _ = CheckForAppUpdateAsync();
        };
        if (_settings.UpdateCheckHours > 0) _checkTimer.Start();

        _scheduleTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _scheduleTimer.Tick += ScheduleTimer_Tick;
        if (_settings.AutoUpdateEnabled) _scheduleTimer.Start();

        // Activate correct filter button
        UpdateFilterButtons();

        Load += async (_, _) =>
        {
            // If started via Windows startup, hide to tray immediately without showing the window
            if (_startMinimized)
            {
                this.WindowState  = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }

            RebuildRows();
            if (_settings.CheckOnStartup) await RefreshStatusAsync();
            // Delay app-update check slightly so the main window finishes loading first
            _ = Task.Delay(3000).ContinueWith(async _ => await SafeInvoke(() => CheckForAppUpdateAsync()));
        };

        FormClosing += MainForm_FormClosing;

        // Resize grip — bottom-right corner panel acting as drag handle
        var grip = new Panel
        {
            Size      = new Size(16, 16),
            Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = AppTheme.BgDeep,
            Cursor    = Cursors.SizeNWSE,
        };
        grip.Location = new Point(ClientSize.Width - 16, ClientSize.Height - 16);
        SizeChanged += (_, _) => grip.Location = new Point(ClientSize.Width - 16, ClientSize.Height - 16);
        grip.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawLine(pen, 4, 14, 14, 4);
            e.Graphics.DrawLine(pen, 8, 14, 14, 8);
            e.Graphics.DrawLine(pen, 12, 14, 14, 12);
        };
        Point gripDragStart = default;
        Size  gripOrigSize   = default;
        grip.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { gripDragStart = PointToScreen(grip.Location); gripOrigSize = Size; } };
        grip.MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                var cur = PointToScreen(e.Location);
                int dw  = cur.X - gripDragStart.X;
                int dh  = cur.Y - gripDragStart.Y;
                Size = new Size(Math.Max(MinimumSize.Width, gripOrigSize.Width + dw),
                                Math.Max(MinimumSize.Height, gripOrigSize.Height + dh));
            }
        };
        Controls.Add(grip);
        grip.BringToFront();
    }

    // ── Title bar ──────────────────────────────────────────────────────────────
    private static Button MakeTitleBarButton(string text, Color hoverColor)
    {
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(44, 40),
            Font      = AppTheme.FontBody,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = hoverColor;
        return btn;
    }

    private void TitleBar_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // ── Icon badge (dark blue rounded rect) ──────────────────────────────
        int bx = 8, by = 6, bw = 28, bh = 28;
        using var badgePath = RoundedRect(bx, by, bw, bh, 6);
        using var badgeBrush = new SolidBrush(Color.FromArgb(18, 40, 90));
        using var badgePen   = new Pen(Color.FromArgb(55, 90, 180), 1f);
        g.FillPath(badgeBrush, badgePath);
        g.DrawPath(badgePen,   badgePath);

        // ── Tree inside badge ─────────────────────────────────────────────────
        int ox = bx + 5, oy = by + 2;
        using var treeBrush  = new SolidBrush(Color.White);
        using var trunkBrush = new SolidBrush(Color.FromArgb(160, 130, 80));
        using var flameBrush = new SolidBrush(AppTheme.Accent);

        // Top pine layer
        g.FillPolygon(treeBrush, new PointF[]
        {
            new(ox + 9, oy),
            new(ox + 5, oy + 8),
            new(ox + 13, oy + 8),
        });
        // Bottom pine layer (wider)
        g.FillPolygon(treeBrush, new PointF[]
        {
            new(ox + 9, oy + 5),
            new(ox + 2, oy + 14),
            new(ox + 16, oy + 14),
        });
        // Trunk
        g.FillRectangle(trunkBrush, ox + 7, oy + 14, 4, 4);
        // Flames (accent blue)
        g.FillEllipse(flameBrush, ox + 5,  oy + 17, 3, 5);
        g.FillEllipse(flameBrush, ox + 7,  oy + 17, 4, 6);
        g.FillEllipse(flameBrush, ox + 10, oy + 17, 3, 5);

        // ── "Baum" + "Launch" title text ──────────────────────────────────────
        int tx = bx + bw + 8;
        using var baumBrush   = new SolidBrush(AppTheme.Accent);
        using var launchBrush = new SolidBrush(AppTheme.TextSecondary);
        var baumFont   = new Font("Segoe UI", 12f, FontStyle.Bold);
        var launchFont = new Font("Segoe UI", 12f, FontStyle.Regular);

        g.DrawString("Baum",   baumFont,   baumBrush,   tx, 10);
        float baumW = g.MeasureString("Baum", baumFont).Width - 3f;
        g.DrawString("Launch", launchFont, launchBrush, tx + baumW, 10);

        baumFont.Dispose();
        launchFont.Dispose();
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragging)
            Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y);
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _dragging = false;
    }

    // ── Filter ─────────────────────────────────────────────────────────────────
    private void FilterButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cat)
        {
            _activeCategory = cat;
            UpdateFilterButtons();
            RebuildRows();
        }
    }

    private void UpdateFilterButtons()
    {
        foreach (var btn in _filterButtons)
        {
            bool active = btn.Tag is string t && t == _activeCategory;
            btn.ForeColor = active ? AppTheme.Accent : AppTheme.TextSecondary;
            btn.Font      = active ? AppTheme.FontBold : AppTheme.FontSmall;
        }
    }

    // ── Row management ─────────────────────────────────────────────────────────
    private void RebuildRows()
    {
        _scrollPanel.SuspendLayout();

        // Remove old rows
        var oldRows = _scrollPanel.Controls.OfType<AppRow>().ToList();
        foreach (var r in oldRows)
        {
            r.ActionClicked -= Row_ActionClicked;
            _scrollPanel.Controls.Remove(r);
            r.Dispose();
        }

        var filtered = _activeCategory switch
        {
            "All"            => _entries,
            // All WinGet-managed apps: updates at top, then up-to-date, then alphabetical within each group
            "WinGet Updates" => _entries
                .Where(e => e.IsWinGetManaged)
                .OrderByDescending(e => e.Status == AppStatus.UpdateAvailable)
                .ThenBy(e => e.DisplayName)
                .ToList(),
            _                => _entries.Where(e => e.Category == _activeCategory).ToList(),
        };

        // Apply search filter (works across all tabs)
        if (!string.IsNullOrWhiteSpace(_searchFilter))
            filtered = filtered
                .Where(e => e.DisplayName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
                         || e.Category.Contains(_searchFilter,    StringComparison.OrdinalIgnoreCase))
                .ToList();

        // Add rows in reverse order (DockStyle.Top stacks in reverse)
        var rows = filtered.Select((e, i) =>
        {
            var row = new AppRow(e) { RowIndex = i };
            row.ActionClicked += Row_ActionClicked;
            return row;
        }).ToList();

        for (int i = rows.Count - 1; i >= 0; i--)
            _scrollPanel.Controls.Add(rows[i]);

        _scrollPanel.ResumeLayout(true);
        UpdateStatusBar();
        UpdateUpdateAllButton();
    }

    private async void Row_ActionClicked(object? sender, AppEntry entry)
    {
        await RunSingleAsync(entry);
    }

    // ── Install / Update ────────────────────────────────────────────────────────
    private async Task RunSingleAsync(AppEntry entry)
    {
        bool isSwitching = entry.Status == AppStatus.NotManaged;

        entry.Status = AppStatus.Installing;
        RefreshRow(entry);

        bool ok = false;

        if (isSwitching)
        {
            // Uninstall the non-WinGet copy first, then reinstall via WinGet
            AppendLog($"Switching {entry.DisplayName} to WinGet...\n▶ Uninstalling existing copy");
            ShowLogOverlay();
            bool uninstalled = await WinGetService.UninstallAsync(entry.WinGetId, line => AppendLog("  " + line));
            if (uninstalled)
                AppendLog("  [Uninstalled]");
            else
                AppendLog("  [Uninstall failed — will use --force to let WinGet take ownership]");
            await Task.Delay(1500);
            AppendLog($"▶ Installing via WinGet{(uninstalled ? "" : " (forced)")}");
            // If uninstall failed the app is still present; pass force=true so winget
            // reinstalls over it and registers ownership rather than skipping as "already installed".
            ok = await WinGetService.InstallOrUpgradeAsync(
                entry.WinGetId, false, line => AppendLog("  " + line), force: !uninstalled);
            AppendLog(ok ? "  [OK]" : "  [FAILED]");
        }
        else
        {
            string verb = entry.IsInstalled ? "Updating" : "Installing";
            AppendLog($"{verb} {entry.DisplayName}...");
            ShowLogOverlay();
            ok = await WinGetService.InstallOrUpgradeAsync(entry.WinGetId, entry.IsInstalled, line => AppendLog("  " + line));
            AppendLog(ok ? "\n[Done]" : "\n[FAILED]");
        }

        entry.Status = ok ? AppStatus.Updated : AppStatus.Failed;
        if (ok)
        {
            entry.IsWinGetManaged  = true;
            // For fresh installs AvailableVersion is null; use a non-null placeholder so
            // IsInstalled flips to true immediately while the background re-check runs.
            entry.InstalledVersion = entry.AvailableVersion ?? entry.InstalledVersion ?? "installed";
            // Persist so future checks can recognise this as WinGet-managed even when
            // "winget list" is inconsistent (e.g. portable apps like Rufus).
            if (!_settings.KnownWinGetIds.Contains(entry.WinGetId, StringComparer.OrdinalIgnoreCase))
            {
                _settings.KnownWinGetIds.Add(entry.WinGetId);
                _settings.Save();
            }
        }
        RefreshRow(entry);

        // Trigger a full re-check after a short delay to let winget register the install
        _ = Task.Run(async () =>
        {
            await Task.Delay(4000);
            await SafeInvoke(RefreshStatusAsync);
        });
    }

    private async Task RunBatchAsync(bool updatesOnly, bool selectedOnly = false)
    {
        IEnumerable<AppEntry> targets;
        if (updatesOnly)
        {
            targets = selectedOnly
                ? _entries.Where(e => e.HasUpdate && e.IsSelected)
                : _entries.Where(e => e.HasUpdate);
        }
        else
        {
            targets = _entries.Where(e => !e.IsInstalled && e.IsSelected);
        }

        var list = targets.ToList();
        if (list.Count == 0)
        {
            MessageBox.Show("Nothing to do.", "BaumLaunch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ShowLogOverlay();
        AppendLog($"Starting batch operation — {list.Count} package(s)...\n");

        foreach (var entry in list)
        {
            AppendLog($"\n▶ {entry.DisplayName} ({entry.WinGetId})");
            entry.Status = AppStatus.Installing;
            SafeRefreshRow(entry);

            bool ok = await WinGetService.InstallOrUpgradeAsync(
                entry.WinGetId, entry.IsInstalled,
                line => AppendLog("  " + line));

            entry.Status = ok ? AppStatus.Updated : AppStatus.Failed;
            if (ok) entry.InstalledVersion = entry.AvailableVersion ?? entry.InstalledVersion;
            SafeRefreshRow(entry);
            AppendLog(ok ? "  [OK]" : "  [FAILED]");
        }

        AppendLog("\n\nDone.");
        await RefreshStatusAsync();
    }

    private void SafeRefreshRow(AppEntry entry)
    {
        if (InvokeRequired) Invoke(() => RefreshRow(entry));
        else RefreshRow(entry);
    }

    private void RefreshRow(AppEntry entry)
    {
        var row = _scrollPanel.Controls.OfType<AppRow>()
            .FirstOrDefault(r => r.Tag is AppEntry e && e.WinGetId == entry.WinGetId);
        row?.Refresh(entry);
    }

    // ── Status check ────────────────────────────────────────────────────────────
    private async Task RefreshStatusAsync()
    {
        if (_isChecking) return;
        _isChecking = true;

        SetCheckingUI(true);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            // Pass 1: winget-managed list, then upgradable list — run SEQUENTIALLY.
            // Both "winget list --source winget" and "winget upgrade --source winget" write
            // to the same SQLite source-index file.  Running them in parallel causes a
            // database lock where one silently returns empty, wiping out all managed state.
            // Sequential: first call downloads the source; second call hits the cache (fast).
            var managed    = await WinGetService.GetWinGetManagedAsync(cts.Token);
            var upgradable = await WinGetService.GetUpgradableAsync(cts.Token);

            var wingetMap = managed
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var upgradableMap = upgradable
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // Pass 2 (instant): scan Windows Add/Remove Programs registry directly.
            // No child processes — finds apps installed by any means (WinGet, browser, installer).
            var arpApps = await Task.Run(() => RegistryService.GetInstalledApps(), cts.Token);

            foreach (var entry in _entries)
            {
                if (wingetMap.TryGetValue(entry.WinGetId, out var inst))
                {
                    // Installed and managed by WinGet
                    entry.InstalledVersion = inst.InstalledVersion;
                    entry.IsWinGetManaged  = true;
                    // Persist so we can survive future winget list inconsistencies
                    if (!_settings.KnownWinGetIds.Contains(entry.WinGetId, StringComparer.OrdinalIgnoreCase))
                        _settings.KnownWinGetIds.Add(entry.WinGetId);

                    if (upgradableMap.TryGetValue(entry.WinGetId, out var upg) && !string.IsNullOrWhiteSpace(upg.AvailableVersion))
                    {
                        entry.AvailableVersion = upg.AvailableVersion;
                        entry.Status           = AppStatus.UpdateAvailable;
                    }
                    else
                    {
                        entry.AvailableVersion = null;
                        entry.Status           = AppStatus.UpToDate;
                    }
                }
                else if (upgradableMap.TryGetValue(entry.WinGetId, out var upg2))
                {
                    // In upgrade list but not in managed list — treat as WinGet-managed with update
                    entry.InstalledVersion = upg2.InstalledVersion;
                    entry.AvailableVersion = upg2.AvailableVersion;
                    entry.IsWinGetManaged  = true;
                    entry.Status           = AppStatus.UpdateAvailable;
                }
                else
                {
                    // Pass 2: check ARP registry — matches by DisplayName or ArpNameHint substring
                    var arpMatch = FindInArp(arpApps, entry);
                    bool knownManaged = _settings.KnownWinGetIds
                        .Contains(entry.WinGetId, StringComparer.OrdinalIgnoreCase);
                    if (arpMatch.HasValue)
                    {
                        entry.InstalledVersion = arpMatch.Value.Version;
                        entry.AvailableVersion = null;
                        if (knownManaged)
                        {
                            // winget list was inconsistent — app still installed, keep as WinGet-managed
                            entry.IsWinGetManaged = true;
                            entry.Status          = AppStatus.UpToDate;
                        }
                        else
                        {
                            entry.IsWinGetManaged = false;
                            entry.Status          = AppStatus.NotManaged;
                        }
                    }
                    else
                    {
                        // Not in ARP either — app was uninstalled; remove from known set
                        if (knownManaged)
                        {
                            _settings.KnownWinGetIds.Remove(entry.WinGetId);
                        }
                        entry.IsWinGetManaged  = false;
                        entry.InstalledVersion = null;
                        entry.AvailableVersion = null;
                        entry.Status           = AppStatus.NotInstalled;
                    }
                }
            }

            _settings.Save(); // persist any KnownWinGetIds changes accumulated in the loop

            _lastChecked = DateTime.Now;
            SafeInvoke(() =>
            {
                RebuildRows();
                UpdateTrayIcon();
                UpdateStatusBar();
            });
        }
        catch (Exception ex)
        {
            SafeInvoke(() => _lblStatusInfo.Text = $"Check failed: {ex.Message}");
        }
        finally
        {
            _isChecking = false;
            SafeInvoke(() => SetCheckingUI(false));
        }
    }

    private void SetCheckingUI(bool checking)
    {
        _lblChecking.Visible = checking;
        _lblChecking.Text    = checking ? "Checking for updates..." : "";
        _btnCheckNow.Enabled = !checking;
    }

    private void UpdateStatusBar()
    {
        int total   = _entries.Count;
        int updates = _entries.Count(e => e.HasUpdate);
        string lastCheckedStr = _lastChecked == DateTime.MinValue
            ? "Never"
            : _lastChecked.ToString("HH:mm");
        _lblStatusInfo.Text = $"{total} apps  •  {updates} update{(updates != 1 ? "s" : "")} available  •  Last checked: {lastCheckedStr}";
        UpdateUpdateAllButton();
    }

    private void UpdateUpdateAllButton()
    {
        int updates = _entries.Count(e => e.HasUpdate);
        _btnUpdateAll.Text = $"Update All ({updates})";

        // Keep the Updates tab label in sync with the current count
        var updatesTab = _filterButtons.FirstOrDefault(b => b.Tag is string t && t == "WinGet Updates");
        if (updatesTab != null)
        {
            string tabText = updates > 0 ? $"⬆ Updates ({updates})" : "⬆ Updates";
            updatesTab.Text  = tabText;
            // Resize so the longer "⬆ Updates (N)" text never gets clipped
            updatesTab.Width = TextRenderer.MeasureText(tabText, AppTheme.FontBold).Width + 22;
        }
    }

    private void UpdateTrayIcon()
    {
        bool hasUpdates = _entries.Any(e => e.HasUpdate);
        _trayIcon.Icon = GenerateTrayIcon(hasUpdates);
        _trayIcon.Text = hasUpdates
            ? $"BaumLaunch — {_entries.Count(e => e.HasUpdate)} update(s) available"
            : "BaumLaunch — Up to date";
    }

    // ── Tray icon generation ────────────────────────────────────────────────────
    private static Icon GenerateTrayIcon(bool hasUpdates)
    {
        // Use 32×32 for crisp rendering; Windows scales it to tray size
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Dark blue-black background badge
            using var bgBrush  = new SolidBrush(Color.FromArgb(12, 25, 65));
            using var bgPen    = new Pen(Color.FromArgb(50, 90, 180), 1.5f);
            using var bgPath   = new System.Drawing.Drawing2D.GraphicsPath();
            bgPath.AddArc(1, 1, 10, 10, 180, 90);
            bgPath.AddArc(21, 1, 10, 10, 270, 90);
            bgPath.AddArc(21, 21, 10, 10, 0, 90);
            bgPath.AddArc(1, 21, 10, 10, 90, 90);
            bgPath.CloseFigure();
            g.FillPath(bgBrush, bgPath);
            g.DrawPath(bgPen,   bgPath);

            // White tree
            using var treeBrush  = new SolidBrush(Color.White);
            using var trunkBrush = new SolidBrush(Color.FromArgb(180, 150, 90));
            g.FillPolygon(treeBrush, new PointF[] { new(16, 3), new(10, 12), new(22, 12) });
            g.FillPolygon(treeBrush, new PointF[] { new(16, 8), new(6, 20),  new(26, 20) });
            g.FillRectangle(trunkBrush, 13, 20, 6, 5);

            // Accent-blue flames
            var flameColor = hasUpdates ? AppTheme.Warning : AppTheme.Accent;
            using var flameBrush = new SolidBrush(flameColor);
            g.FillEllipse(flameBrush, 10, 24, 4, 6);
            g.FillEllipse(flameBrush, 14, 24, 4, 7);
            g.FillEllipse(flameBrush, 18, 24, 4, 6);

            // Orange dot indicator when updates are available
            if (hasUpdates)
            {
                using var dotBrush = new SolidBrush(AppTheme.Warning);
                using var dotPen   = new Pen(Color.FromArgb(12, 25, 65), 1.5f);
                g.FillEllipse(dotBrush, 22, 1, 9, 9);
                g.DrawEllipse(dotPen,   22, 1, 9, 9);
            }
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ── Profile export/import ──────────────────────────────────────────────────
    private void ExportProfile_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title      = "Export Profile",
            Filter     = "BaumLaunch Profile (*.json)|*.json",
            DefaultExt = "json",
            FileName   = "BaumLaunch-Profile.json",
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var profile = UserProfile.FromEntries(_entries);
            File.WriteAllText(dlg.FileName, profile.ToJson());
            MessageBox.Show($"Profile saved to:\n{dlg.FileName}", "BaumLaunch", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ImportProfile_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Import Profile",
            Filter = "BaumLaunch Profile (*.json)|*.json|All Files (*.*)|*.*",
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            string json = File.ReadAllText(dlg.FileName);
            var profile = UserProfile.FromJson(json);
            if (profile == null)
            {
                MessageBox.Show("Invalid profile file.", "BaumLaunch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            profile.ApplyTo(_entries);
            RebuildRows();
            MessageBox.Show($"Profile '{profile.Name}' applied ({profile.SelectedIds.Count} apps selected).",
                "BaumLaunch", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ── Log overlay ─────────────────────────────────────────────────────────────
    private void ShowLogOverlay()
    {
        if (_logOverlay != null) return;

        _logOverlay = new Panel
        {
            Size      = new Size(ClientSize.Width - 40, ClientSize.Height - 80),
            Location  = new Point(20, 50),
            BackColor = AppTheme.BgDeep,
            BorderStyle = BorderStyle.None,
        };
        _logOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;

        _logBox = new RichTextBox
        {
            Dock      = DockStyle.Fill,
            BackColor = AppTheme.BgDeep,
            ForeColor = AppTheme.TextPrimary,
            Font      = AppTheme.FontMono,
            ReadOnly  = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical,
        };

        var closeBtn = new Button
        {
            Text      = "Close",
            Dock      = DockStyle.Bottom,
            Height    = 34,
            Font      = AppTheme.FontButton,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.BgCard,
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.Click += (_, _) => HideLogOverlay();

        var hdr = new Label
        {
            Text      = "Operation Log",
            Dock      = DockStyle.Top,
            Height    = 34,
            Font      = AppTheme.FontHeader,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.BgPanel,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
        };

        _logOverlay.Controls.Add(_logBox);
        _logOverlay.Controls.Add(closeBtn);
        _logOverlay.Controls.Add(hdr);

        Controls.Add(_logOverlay);
        _logOverlay.BringToFront();
    }

    private void HideLogOverlay()
    {
        if (_logOverlay != null)
        {
            Controls.Remove(_logOverlay);
            _logOverlay.Dispose();
            _logOverlay = null;
            _logBox     = null;
        }
    }

    private void AppendLog(string text)
    {
        if (_logBox == null) return;
        if (_logBox.InvokeRequired)
            _logBox.Invoke(() => AppendLog(text));
        else
        {
            _logBox.AppendText(text + "\n");
            _logBox.ScrollToCaret();
        }
    }

    // ── Bottom bar layout ────────────────────────────────────────────────────────
    private void LayoutBottomBar()
    {
        int bx = 8, by = 10, gap = 6;
        PlaceBtn(_btnSelectAll,       ref bx, by, gap);
        PlaceBtn(_btnDeselectAll,     ref bx, by, gap);
        bx += 8; // spacer
        PlaceBtn(_btnInstallSelected, ref bx, by, gap);
        PlaceBtn(_btnUpdateSelected,  ref bx, by, gap);
        PlaceBtn(_btnUpdateAll,       ref bx, by, gap);
        bx += 8;
        PlaceBtn(_btnExport,          ref bx, by, gap);
        PlaceBtn(_btnImport,          ref bx, by, gap);

        // Settings + Check Now pinned to the right
        int rbx = _bottomBar.Width - 8;
        _btnSettings.Size     = new Size(94, 30);
        _btnSettings.Location = new Point(rbx - _btnSettings.Width, 10);

        _btnCheckNow.Size     = new Size(94, 30);
        _btnCheckNow.Location = new Point(_btnSettings.Left - 6 - _btnCheckNow.Width, 10);

        if (!_bottomBar.Controls.Contains(_btnSelectAll))
        {
            _bottomBar.Controls.AddRange(new Control[] {
                _btnSelectAll, _btnDeselectAll,
                _btnInstallSelected, _btnUpdateSelected, _btnUpdateAll,
                _btnExport, _btnImport, _btnCheckNow, _btnSettings
            });
        }
    }

    private static void PlaceBtn(Button btn, ref int x, int y, int gap)
    {
        btn.Location = new Point(x, y);
        x += btn.Width + gap;
    }

    private static Button MakeToolbarButton(string text, Color bgColor, Color fgColor)
    {
        int w = TextRenderer.MeasureText(text, AppTheme.FontButton).Width + 20;
        var btn = new Button
        {
            Text      = text,
            Size      = new Size(Math.Max(w, 80), 30),
            Font      = AppTheme.FontButton,
            FlatStyle = FlatStyle.Flat,
            ForeColor = fgColor,
            BackColor = bgColor,
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bgColor, 0.15f);
        return btn;
    }

    // ── Tray / window ────────────────────────────────────────────────────────────
    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(2000, "BaumLaunch", "Running in the system tray.", ToolTipIcon.None);
        }
        else
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _checkTimer.Stop();
            _checkTimer.Dispose();
            _scheduleTimer.Stop();
            _scheduleTimer.Dispose();
        }
    }

    // ── Settings ─────────────────────────────────────────────────────────────────
    private void OpenSettings()
    {
        using var dlg = new SettingsForm(_settings);
        dlg.SettingsSaved += (_, _) => ApplySettings();
        dlg.ShowDialog(this);
    }

    private void ApplySettings()
    {
        // Re-configure the update check timer
        _checkTimer.Stop();
        if (_settings.UpdateCheckHours > 0)
        {
            _checkTimer.Interval = SettingsToTimerInterval(_settings);
            _checkTimer.Start();
        }

        // Re-configure the schedule timer
        _scheduleTimer.Stop();
        if (_settings.AutoUpdateEnabled) _scheduleTimer.Start();
    }

    private static int SettingsToTimerInterval(AppSettings s) =>
        Math.Max(s.UpdateCheckHours, 1) * 60 * 60 * 1000;

    // ── Scheduled auto-update ───────────────────────────────────────────────────
    private void ScheduleTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.AutoUpdateEnabled) return;

        var now = DateTime.Now;
        // Don't fire more than once per day
        if (_lastScheduledRun.Date == now.Date) return;

        bool dayMatches = _settings.AutoUpdateSchedule == "Monthly"
            ? now.Day == _settings.AutoUpdateDayOfMonth
            : (int)now.DayOfWeek == _settings.AutoUpdateDayOfWeek;

        if (!dayMatches) return;
        if (now.Hour != _settings.AutoUpdateHour || now.Minute != _settings.AutoUpdateMinute) return;

        _lastScheduledRun = now;
        _ = RunScheduledUpdateAsync();
    }

    private async Task RunScheduledUpdateAsync()
    {
        SafeInvoke(() => _trayIcon?.ShowBalloonTip(2000, "BaumLaunch",
            "Starting scheduled silent update…", ToolTipIcon.None));
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("winget",
                "upgrade --all --silent --accept-package-agreements --accept-source-agreements")
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null) await proc.WaitForExitAsync();
        }
        catch { }

        await SafeInvoke(RefreshStatusAsync);
        SafeInvoke(() => _trayIcon?.ShowBalloonTip(3000, "BaumLaunch",
            "Scheduled update complete.", ToolTipIcon.None));
    }

    // ── App self-update ──────────────────────────────────────────────────────────
    private async Task CheckForAppUpdateAsync(bool force = false)
    {
        if (_appUpdateCheckInProgress) return;
        _appUpdateCheckInProgress = true;

        try
        {
            var result = await UpdateService.CheckAsync();
            if (result == null)
            {
                if (force)
                    SafeInvoke(() => _trayIcon.ShowBalloonTip(2000, "BaumLaunch", "Already up to date.", ToolTipIcon.None));
                return;
            }

            var (version, url) = result.Value;
            SafeInvoke(() =>
                _trayIcon.ShowBalloonTip(4000, "BaumLaunch Update",
                    $"Updating to v{version} — the app will restart automatically.", ToolTipIcon.None));

            // Small pause so the user sees the balloon before the window disappears
            await Task.Delay(2000);
            await UpdateService.DownloadAndInstallAsync(version, url);
        }
        finally
        {
            _appUpdateCheckInProgress = false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private readonly record struct ArpMatch(string Version);

    /// <summary>
    /// Looks up a catalog entry in the ARP registry dictionary.
    /// Checks (in order): exact DisplayName match, DisplayName prefix + space/version, ArpNameHint substring.
    /// </summary>
    private static ArpMatch? FindInArp(Dictionary<string, string> arpApps, AppEntry entry)
    {
        var display = entry.DisplayName;
        var hint    = entry.ArpNameHint;

        foreach (var (arpName, version) in arpApps)
        {
            // Exact match (case-insensitive)
            if (arpName.Equals(display, StringComparison.OrdinalIgnoreCase))
                return new ArpMatch(version);

            // ARP name starts with catalog DisplayName followed by a space or digit (version suffix)
            // e.g. "7-Zip 24.08 (x64 edition)" matches catalog "7-Zip"
            if (arpName.StartsWith(display, StringComparison.OrdinalIgnoreCase))
            {
                int next = display.Length;
                if (next < arpName.Length && (arpName[next] == ' ' || char.IsDigit(arpName[next])))
                    return new ArpMatch(version);
            }

            // ArpNameHint substring — for packages where the ARP name has no obvious relation
            // e.g. hint "Visual Studio Code" matches "Microsoft Visual Studio Code"
            // e.g. hint "LGHUB" matches "LGHUB"
            if (!string.IsNullOrEmpty(hint) &&
                arpName.Contains(hint, StringComparison.OrdinalIgnoreCase))
                return new ArpMatch(version);
        }
        return null;
    }

    private void SafeInvoke(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) Invoke(action);
        else action();
    }

    private async Task SafeInvoke(Func<Task> action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) await (Task)Invoke(action)!;
        else await action();
    }
}
