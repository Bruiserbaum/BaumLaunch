using BaumLaunch.Models;
using BaumLaunch.Services;

namespace BaumLaunch;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly CheckBox    _chkStartup;
    private readonly CheckBox    _chkCheckOnStartup;
    private readonly ComboBox    _cboFrequency;
    private readonly Label       _lblVersion;
    private readonly Label       _lblUpdateStatus;
    private readonly Button      _btnCheckUpdate;

    /// <summary>Raised after the user saves so MainForm can apply changed settings.</summary>
    public event EventHandler? SettingsSaved;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        // ── Form ──────────────────────────────────────────────────────────────
        Text            = "Settings";
        Size            = new Size(460, 480);
        MinimumSize     = new Size(420, 460);
        FormBorderStyle = FormBorderStyle.None;
        BackColor       = AppTheme.BgMain;
        StartPosition   = FormStartPosition.CenterParent;
        DoubleBuffered  = true;

        // ── Title bar ─────────────────────────────────────────────────────────
        var titleBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 40,
            BackColor = AppTheme.BgDeep,
        };
        titleBar.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var b = new SolidBrush(AppTheme.TextPrimary);
            e.Graphics.DrawString("⚙  Settings", AppTheme.FontHeader, b, 14, 12);
        };

        var btnClose = new Button
        {
            Text      = "✕",
            Size      = new Size(44, 40),
            Font      = AppTheme.FontBody,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            Cursor    = Cursors.Hand,
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 60, 60);
        btnClose.Click += (_, _) => Close();

        void PositionClose() => btnClose.Location = new Point(titleBar.Width - 44, 0);
        titleBar.SizeChanged += (_, _) => PositionClose();
        SizeChanged          += (_, _) => PositionClose();
        titleBar.Controls.Add(btnClose);

        // ── Scroll body ───────────────────────────────────────────────────────
        var body = new Panel
        {
            Dock       = DockStyle.Fill,
            AutoScroll = true,
            Padding    = new Padding(24, 16, 24, 16),
            BackColor  = AppTheme.BgMain,
        };

        int y = 16;

        // ── ABOUT section ─────────────────────────────────────────────────────
        body.Controls.Add(SectionLabel("ABOUT", ref y));

        var card = new Panel
        {
            Location  = new Point(0, y),
            Size      = new Size(380, 84),
            BackColor = AppTheme.BgPanel,
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        _lblVersion = new Label
        {
            AutoSize  = false,
            Size      = new Size(260, 22),
            Location  = new Point(14, 12),
            Font      = AppTheme.FontBold,
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent,
            Text      = $"BaumLaunch  v{UpdateService.CurrentVersion.ToString(3)}",
        };

        _lblUpdateStatus = new Label
        {
            AutoSize  = false,
            Size      = new Size(340, 18),
            Location  = new Point(14, 36),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            Text      = "Click below to check for a newer version.",
        };

        _btnCheckUpdate = new Button
        {
            Text      = "Check for App Update",
            Size      = new Size(160, 26),
            Location  = new Point(14, 56),
            Font      = AppTheme.FontButton,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Accent,
            Cursor    = Cursors.Hand,
        };
        _btnCheckUpdate.FlatAppearance.BorderSize = 0;
        _btnCheckUpdate.Click += BtnCheckUpdate_Click;

        card.Controls.AddRange(new Control[] { _lblVersion, _lblUpdateStatus, _btnCheckUpdate });
        body.Controls.Add(card);
        y += card.Height + 20;

        // ── STARTUP section ───────────────────────────────────────────────────
        body.Controls.Add(SectionLabel("STARTUP", ref y));

        _chkStartup = DarkCheckBox("Start BaumLaunch with Windows", y);
        _chkStartup.Checked = AppSettings.GetStartWithWindows();
        body.Controls.Add(_chkStartup);
        y += 30;

        _chkCheckOnStartup = DarkCheckBox("Check for package updates on startup", y);
        _chkCheckOnStartup.Checked = _settings.CheckOnStartup;
        body.Controls.Add(_chkCheckOnStartup);
        y += 36;

        // ── UPDATE CHECKS section ─────────────────────────────────────────────
        body.Controls.Add(SectionLabel("PACKAGE UPDATE CHECKS", ref y));

        body.Controls.Add(new Label
        {
            AutoSize  = false,
            Size      = new Size(200, 20),
            Location  = new Point(0, y),
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            Text      = "Automatic check interval:",
        });
        y += 24;

        _cboFrequency = new ComboBox
        {
            Location      = new Point(0, y),
            Size          = new Size(200, 26),
            Font          = AppTheme.FontBody,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = AppTheme.BgCard,
            ForeColor     = AppTheme.TextPrimary,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cboFrequency.Items.AddRange(new object[]
        {
            "Manual only",
            "Every 1 hour",
            "Every 3 hours",
            "Every 6 hours",
            "Every 12 hours",
            "Every 24 hours",
        });
        _cboFrequency.SelectedIndex = HoursToIndex(_settings.UpdateCheckHours);
        body.Controls.Add(_cboFrequency);
        y += 40;

        // ── Bottom bar ────────────────────────────────────────────────────────
        var bottomBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 52,
            BackColor = AppTheme.BgDeep,
        };
        bottomBar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawLine(pen, 0, 0, bottomBar.Width, 0);
        };

        var btnSave = new Button
        {
            Text      = "Save & Close",
            Size      = new Size(120, 30),
            Font      = AppTheme.FontButton,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Accent,
            Cursor    = Cursors.Hand,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;
        bottomBar.SizeChanged += (_, _) =>
            btnSave.Location = new Point(bottomBar.Width - 132, 11);
        SizeChanged += (_, _) =>
            btnSave.Location = new Point(bottomBar.Width - 132, 11);
        bottomBar.Controls.Add(btnSave);

        // ── Assemble ──────────────────────────────────────────────────────────
        Controls.Add(body);
        Controls.Add(bottomBar);
        Controls.Add(titleBar);

        // Allow dragging via title bar
        Point dragStart = default;
        bool  dragging  = false;
        titleBar.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { dragging = true; dragStart = e.Location; } };
        titleBar.MouseMove += (_, e) => { if (dragging) Location = new Point(Left + e.X - dragStart.X, Top + e.Y - dragStart.Y); };
        titleBar.MouseUp   += (_, _) => dragging = false;

        // Resize handle bottom-right
        Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        };
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private async void BtnCheckUpdate_Click(object? sender, EventArgs e)
    {
        _btnCheckUpdate.Enabled = false;
        _lblUpdateStatus.ForeColor = AppTheme.TextMuted;
        _lblUpdateStatus.Text = "Checking…";

        var result = await UpdateService.CheckAsync();
        if (result == null)
        {
            _lblUpdateStatus.ForeColor = AppTheme.Success;
            _lblUpdateStatus.Text      = $"Already up to date (v{UpdateService.CurrentVersion.ToString(3)})";
        }
        else
        {
            var (version, url) = result.Value;
            _lblUpdateStatus.ForeColor = AppTheme.Warning;
            _lblUpdateStatus.Text      = $"v{version} available — downloading…";

            await Task.Delay(800);
            await UpdateService.DownloadAndInstallAsync(version, url);
        }

        _btnCheckUpdate.Enabled = true;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        AppSettings.SetStartWithWindows(_chkStartup.Checked);
        _settings.StartWithWindows = _chkStartup.Checked;
        _settings.CheckOnStartup   = _chkCheckOnStartup.Checked;
        _settings.UpdateCheckHours = IndexToHours(_cboFrequency.SelectedIndex);
        _settings.Save();
        SettingsSaved?.Invoke(this, EventArgs.Empty);
        Close();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Label SectionLabel(string text, ref int y)
    {
        var lbl = new Label
        {
            AutoSize  = false,
            Size      = new Size(380, 18),
            Location  = new Point(0, y),
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            Text      = text,
        };
        y += 22;
        return lbl;
    }

    private static CheckBox DarkCheckBox(string text, int y) => new()
    {
        AutoSize  = false,
        Size      = new Size(360, 24),
        Location  = new Point(0, y),
        Font      = AppTheme.FontBody,
        ForeColor = AppTheme.TextSecondary,
        BackColor = Color.Transparent,
        Text      = text,
        Cursor    = Cursors.Hand,
    };

    private static int HoursToIndex(int hours) => hours switch
    {
        0  => 0,
        1  => 1,
        3  => 2,
        6  => 3,
        12 => 4,
        24 => 5,
        _  => 3, // default 6h
    };

    private static int IndexToHours(int index) => index switch
    {
        0 => 0,
        1 => 1,
        2 => 3,
        3 => 6,
        4 => 12,
        5 => 24,
        _ => 6,
    };
}
