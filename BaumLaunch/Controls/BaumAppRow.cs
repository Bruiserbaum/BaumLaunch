using BaumLaunch.Models;

namespace BaumLaunch.Controls;

public sealed class BaumAppRow : UserControl
{
    private BaumAppEntry _entry;
    private int _rowIndex;

    private readonly Label  _lblName;
    private readonly Label  _lblInstalled;
    private readonly Label  _lblArrow;
    private readonly Label  _lblLatest;
    private readonly Label  _lblStatus;
    private readonly Button _btnAction;
    private readonly Panel  _progressBar;

    public event EventHandler<BaumAppEntry>? ActionClicked;

    public int RowIndex
    {
        get => _rowIndex;
        set
        {
            _rowIndex = value;
            BackColor = (_rowIndex % 2 == 0) ? AppTheme.BgCard : AppTheme.BgPanel;
        }
    }

    public BaumAppRow(BaumAppEntry entry)
    {
        _entry = entry;

        Height    = 60;
        Dock      = DockStyle.Top;
        BackColor = AppTheme.BgCard;

        _lblName = new Label
        {
            AutoSize  = false,
            Size      = new Size(250, 20),
            Location  = new Point(14, 10),
            Font      = AppTheme.FontBold,
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var _lblSub = new Label
        {
            AutoSize  = false,
            Size      = new Size(250, 16),
            Location  = new Point(14, 30),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "github.com/Bruiserbaum/" + entry.RepoName,
        };

        _lblInstalled = new Label
        {
            AutoSize  = false,
            Size      = new Size(120, 20),
            Location  = new Point(280, 20),
            Font      = AppTheme.FontMono,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _lblArrow = new Label
        {
            AutoSize  = false,
            Size      = new Size(18, 20),
            Location  = new Point(405, 20),
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = "→",
            Visible   = false,
        };

        _lblLatest = new Label
        {
            AutoSize  = false,
            Size      = new Size(120, 20),
            Location  = new Point(428, 20),
            Font      = AppTheme.FontMono,
            ForeColor = AppTheme.Accent,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Visible   = false,
        };

        _lblStatus = new Label
        {
            AutoSize  = false,
            Size      = new Size(120, 24),
            Location  = new Point(560, 18),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _lblStatus.Paint += OnStatusBadgePaint;

        _btnAction = new Button
        {
            AutoSize  = false,
            Size      = new Size(84, 28),
            Location  = new Point(698, 16),
            Font      = AppTheme.FontButton,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextPrimary,
            Cursor    = Cursors.Hand,
            Visible   = false,
        };
        _btnAction.FlatAppearance.BorderSize = 0;
        _btnAction.Click += (_, _) => ActionClicked?.Invoke(this, _entry);

        // Thin progress bar at the bottom of the row (shown while downloading)
        _progressBar = new Panel
        {
            Location  = new Point(0, Height - 3),
            Size      = new Size(0, 3),
            BackColor = AppTheme.Accent,
            Visible   = false,
        };

        Controls.AddRange(new Control[] {
            _lblName, _lblSub, _lblInstalled,
            _lblArrow, _lblLatest, _lblStatus, _btnAction, _progressBar,
        });

        foreach (Control c in Controls)
        {
            c.MouseEnter += (_, _) => BackColor = AppTheme.BgCardHover;
            c.MouseLeave += (_, _) =>
            {
                if (!ClientRectangle.Contains(PointToClient(MousePosition)))
                    BackColor = (_rowIndex % 2 == 0) ? AppTheme.BgCard : AppTheme.BgPanel;
            };
        }
        MouseEnter += (_, _) => BackColor = AppTheme.BgCardHover;
        MouseLeave += (_, _) =>
        {
            if (!ClientRectangle.Contains(PointToClient(MousePosition)))
                BackColor = (_rowIndex % 2 == 0) ? AppTheme.BgCard : AppTheme.BgPanel;
        };

        Refresh(entry);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var borderPen = new Pen(AppTheme.Border, 1);
        e.Graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
    }

    private void OnStatusBadgePaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        Color badgeColor = _entry.Status switch
        {
            BaumAppStatus.UpToDate     => AppTheme.Success,
            BaumAppStatus.UpdateAvailable => AppTheme.Warning,
            BaumAppStatus.NotInstalled => AppTheme.TextMuted,
            BaumAppStatus.Downloading  => AppTheme.Accent,
            BaumAppStatus.Installing   => AppTheme.Accent,
            BaumAppStatus.Updated      => AppTheme.Success,
            BaumAppStatus.Failed       => AppTheme.Danger,
            _                          => AppTheme.TextMuted,
        };

        string badgeText = _entry.Status switch
        {
            BaumAppStatus.UpToDate        => "Up to date",
            BaumAppStatus.UpdateAvailable => "Update avail.",
            BaumAppStatus.NotInstalled    => "Not installed",
            BaumAppStatus.Downloading     => _entry.DownloadProgress >= 0
                                             ? $"Downloading {_entry.DownloadProgress}%"
                                             : "Downloading...",
            BaumAppStatus.Installing      => "Installing...",
            BaumAppStatus.Updated         => "Updated \u2713",
            BaumAppStatus.Failed          => "Failed",
            _                             => "Unknown",
        };

        var ctrl = (Label)sender!;
        var rect = new Rectangle(0, 0, ctrl.Width - 1, ctrl.Height - 1);

        using var bgBrush   = new SolidBrush(Color.FromArgb(40, badgeColor));
        using var borderPen = new Pen(Color.FromArgb(120, badgeColor), 1);
        using var textBrush = new SolidBrush(badgeColor);

        const int radius = 8;
        var path = RoundedRect(rect, radius);
        g.FillPath(bgBrush, path);
        g.DrawPath(borderPen, path);

        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(badgeText, AppTheme.FontSmall, textBrush, rect, sf);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public void Refresh(BaumAppEntry entry)
    {
        _entry    = entry;
        _lblName.Text = entry.DisplayName;

        if (entry.InstalledVersion != null)
        {
            _lblInstalled.Text      = entry.InstalledVersion.ToString();
            _lblInstalled.ForeColor = AppTheme.TextSecondary;
        }
        else
        {
            _lblInstalled.Text      = "Not installed";
            _lblInstalled.ForeColor = AppTheme.TextMuted;
        }

        bool hasUpdate = entry.Status == BaumAppStatus.UpdateAvailable
                      && entry.LatestVersion != null;
        _lblArrow.Visible  = hasUpdate;
        _lblLatest.Visible = hasUpdate;
        if (hasUpdate) _lblLatest.Text = entry.LatestVersion!.ToString();

        _lblStatus.Invalidate();

        // Progress bar
        bool showProgress = entry.Status == BaumAppStatus.Downloading && entry.DownloadProgress >= 0;
        _progressBar.Visible = showProgress;
        if (showProgress)
            _progressBar.Width = (int)(Width * entry.DownloadProgress / 100.0);

        // Action button
        switch (entry.Status)
        {
            case BaumAppStatus.NotInstalled:
            case BaumAppStatus.Unknown when entry.InstalledVersion == null:
                _btnAction.Text      = "Install";
                _btnAction.BackColor = AppTheme.Accent;
                _btnAction.ForeColor = AppTheme.TextPrimary;
                _btnAction.Enabled   = true;
                _btnAction.Visible   = true;
                break;
            case BaumAppStatus.UpdateAvailable:
                _btnAction.Text      = "Update";
                _btnAction.BackColor = AppTheme.Warning;
                _btnAction.ForeColor = Color.FromArgb(30, 30, 30);
                _btnAction.Enabled   = true;
                _btnAction.Visible   = true;
                break;
            case BaumAppStatus.Downloading:
            case BaumAppStatus.Installing:
                _btnAction.Text      = "...";
                _btnAction.BackColor = AppTheme.Accent;
                _btnAction.ForeColor = AppTheme.TextPrimary;
                _btnAction.Enabled   = false;
                _btnAction.Visible   = true;
                break;
            default:
                _btnAction.Visible = false;
                _btnAction.Enabled = true;
                break;
        }

        Invalidate();
    }
}
