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

    // ── Timers ───────────────────────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _checkTimer;

    // ── Tray ─────────────────────────────────────────────────────────────────
    private readonly NotifyIcon _trayIcon;

    // ── UI panels ────────────────────────────────────────────────────────────
    private readonly Panel      _titleBar;
    private readonly Panel      _filterBar;
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

    // ── Filter buttons ────────────────────────────────────────────────────────
    private readonly List<Button> _filterButtons = new();

    // ── Window drag state ────────────────────────────────────────────────────
    private Point _dragStart;
    private bool  _dragging;

    // ── Log overlay panel ────────────────────────────────────────────────────
    private Panel?       _logOverlay;
    private RichTextBox? _logBox;

    public MainForm()
    {
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
        btnClose.Location = new Point(Width - 44, 0);
        btnClose.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.Click   += (_, _) => { _exitRequested = false; Close(); };

        var btnMin = MakeTitleBarButton("–", Color.FromArgb(60, 60, 90));
        btnMin.Location = new Point(Width - 88, 0);
        btnMin.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
        btnMin.Click   += (_, _) => WindowState = FormWindowState.Minimized;

        _titleBar.Controls.Add(btnClose);
        _titleBar.Controls.Add(btnMin);

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

        string[] categories = { "All", "Browsers", "Runtimes", "Dev Tools", "Media & Tools", "Game Launchers", "Communication", "System Tools" };
        int bx = 6;
        foreach (var cat in categories)
        {
            var btn = new Button
            {
                Text      = cat,
                AutoSize  = false,
                Size      = new Size(TextRenderer.MeasureText(cat, AppTheme.FontSmall).Width + 22, 28),
                Location  = new Point(bx, 5),
                Font      = AppTheme.FontSmall,
                FlatStyle = FlatStyle.Flat,
                ForeColor = AppTheme.TextSecondary,
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
        _btnUpdateSelected = MakeToolbarButton("Update Selected", AppTheme.Warning, Color.FromArgb(30, 30, 30));
        _btnUpdateAll      = MakeToolbarButton("Update All (0)", AppTheme.Warning, Color.FromArgb(30, 30, 30));
        _btnExport         = MakeToolbarButton("Export Profile", AppTheme.BgCard,  AppTheme.TextSecondary);
        _btnImport         = MakeToolbarButton("Import Profile", AppTheme.BgCard,  AppTheme.TextSecondary);
        _btnCheckNow       = MakeToolbarButton("Check Now",      AppTheme.Accent,  AppTheme.TextPrimary);

        _btnSelectAll.Click       += (_, _) => { foreach (var e in _entries) e.IsSelected = true;  RebuildRows(); };
        _btnDeselectAll.Click     += (_, _) => { foreach (var e in _entries) e.IsSelected = false; RebuildRows(); };
        _btnInstallSelected.Click += async (_, _) => await RunBatchAsync(false);
        _btnUpdateSelected.Click  += async (_, _) => await RunBatchAsync(true, selectedOnly: true);
        _btnUpdateAll.Click       += async (_, _) => await RunBatchAsync(true, selectedOnly: false);
        _btnExport.Click          += ExportProfile_Click;
        _btnImport.Click          += ImportProfile_Click;
        _btnCheckNow.Click        += async (_, _) => await RefreshStatusAsync();

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
        Controls.Add(_filterBar);
        Controls.Add(_titleBar);

        // ── Tray icon ────────────────────────────────────────────────────────
        var ctxMenu = new ContextMenuStrip();
        ctxMenu.BackColor = AppTheme.BgPanel;
        ctxMenu.ForeColor = AppTheme.TextPrimary;
        ctxMenu.Font      = AppTheme.FontBody;
        ctxMenu.Items.Add("Open BaumLaunch",    null, (_, _) => ShowMainWindow());
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("Check for Updates",  null, async (_, _) => await RefreshStatusAsync());
        ctxMenu.Items.Add("Update All",         null, async (_, _) => await RunBatchAsync(true, selectedOnly: false));
        ctxMenu.Items.Add(new ToolStripSeparator());
        ctxMenu.Items.Add("Exit",               null, (_, _) => { _exitRequested = true; Application.Exit(); });

        _trayIcon = new NotifyIcon
        {
            Text        = "BaumLaunch",
            Icon        = GenerateTrayIcon(false),
            ContextMenuStrip = ctxMenu,
            Visible     = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();

        // ── Timer ────────────────────────────────────────────────────────────
        _checkTimer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 }; // 6 hours
        _checkTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _checkTimer.Start();

        // Activate correct filter button
        UpdateFilterButtons();

        Load += async (_, _) =>
        {
            RebuildRows();
            await RefreshStatusAsync();
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
        // Small icon — package box
        int ix = 12, iy = 10, sz = 20;
        using var iconBrush = new SolidBrush(AppTheme.Accent);
        using var iconPen   = new Pen(AppTheme.Accent, 1.5f);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        // Box body
        g.DrawRectangle(iconPen, ix, iy + 6, sz, sz - 6);
        // Box lid
        g.DrawPolygon(iconPen, new Point[] {
            new(ix - 2, iy + 7), new(ix + sz / 2, iy), new(ix + sz + 2, iy + 7)
        });
        // Center stripe
        g.DrawLine(iconPen, ix + sz / 2, iy, ix + sz / 2, iy + 6);

        // Title text
        using var titleBrush = new SolidBrush(AppTheme.TextPrimary);
        g.DrawString("BaumLaunch", AppTheme.FontHeader, titleBrush, ix + sz + 10, 12);
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

        var filtered = _activeCategory == "All"
            ? _entries
            : _entries.Where(e => e.Category == _activeCategory).ToList();

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
        entry.Status = AppStatus.Installing;
        RefreshRow(entry);

        var lines = new List<string>();
        bool ok = await WinGetService.InstallOrUpgradeAsync(
            entry.WinGetId, entry.IsInstalled,
            line => lines.Add(line));

        entry.Status = ok ? AppStatus.Updated : AppStatus.Failed;
        if (ok) entry.InstalledVersion = entry.AvailableVersion ?? entry.InstalledVersion;
        RefreshRow(entry);

        // Trigger a full re-check in background
        _ = Task.Delay(2000).ContinueWith(async _ => await SafeInvoke(RefreshStatusAsync));
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

            var installed  = await WinGetService.GetInstalledAsync(cts.Token);
            var upgradable = await WinGetService.GetUpgradableAsync(cts.Token);

            // Build lookup dictionaries
            var installedMap  = installed.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
            var upgradableMap = upgradable.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var entry in _entries)
            {
                if (installedMap.TryGetValue(entry.WinGetId, out var inst))
                {
                    entry.InstalledVersion = inst.InstalledVersion;
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
                else
                {
                    // Check upgradable list too (some may show even if not in 'list')
                    if (upgradableMap.TryGetValue(entry.WinGetId, out var upg2))
                    {
                        entry.InstalledVersion = upg2.InstalledVersion;
                        entry.AvailableVersion = upg2.AvailableVersion;
                        entry.Status           = AppStatus.UpdateAvailable;
                    }
                    else
                    {
                        entry.InstalledVersion = null;
                        entry.AvailableVersion = null;
                        entry.Status           = AppStatus.NotInstalled;
                    }
                }
            }

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
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var boxColor = hasUpdates ? AppTheme.Warning : AppTheme.Success;
            using var brush = new SolidBrush(boxColor);
            using var pen   = new Pen(boxColor, 1f);

            // Package box icon
            // Body
            g.FillRectangle(brush, 2, 7, 12, 8);
            // Lid triangle
            var lid = new Point[] { new(1, 8), new(8, 2), new(15, 8) };
            g.FillPolygon(brush, lid);
            // Belt stripe (darker)
            using var darkBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            g.FillRectangle(darkBrush, 6, 7, 4, 8);
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

        // Check Now on right side
        _btnCheckNow.Size     = new Size(94, 30);
        _btnCheckNow.Location = new Point(_bottomBar.Width - 102, 10);
        _btnCheckNow.Anchor   = AnchorStyles.Top | AnchorStyles.Right;

        if (!_bottomBar.Controls.Contains(_btnSelectAll))
        {
            _bottomBar.Controls.AddRange(new Control[] {
                _btnSelectAll, _btnDeselectAll,
                _btnInstallSelected, _btnUpdateSelected, _btnUpdateAll,
                _btnExport, _btnImport, _btnCheckNow
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
            _trayIcon.ShowBalloonTip(2000, "BaumLaunch", "Running in the system tray.", ToolTipIcon.Info);
        }
        else
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _checkTimer.Stop();
            _checkTimer.Dispose();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────
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
