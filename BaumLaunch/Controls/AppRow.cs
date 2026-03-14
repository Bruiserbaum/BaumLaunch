using BaumLaunch.Models;

namespace BaumLaunch.Controls;

public sealed class AppRow : UserControl
{
    private AppEntry _entry;
    private int _rowIndex;

    private readonly CheckBox _chk;
    private readonly Label _lblName;
    private readonly Label _lblCategory;
    private readonly Label _lblInstalled;
    private readonly Label _lblArrow;
    private readonly Label _lblAvailable;
    private readonly Label _lblStatus;
    private readonly Button _btnAction;

    public event EventHandler<AppEntry>? ActionClicked;

    public int RowIndex
    {
        get => _rowIndex;
        set
        {
            _rowIndex = value;
            BackColor = (_rowIndex % 2 == 0) ? AppTheme.BgCard : AppTheme.BgPanel;
        }
    }

    public AppRow(AppEntry entry)
    {
        _entry = entry;

        Height = 56;
        Dock = DockStyle.Top;
        BackColor = AppTheme.BgCard;
        Cursor = Cursors.Default;

        // Checkbox
        _chk = new CheckBox
        {
            Size      = new Size(20, 20),
            Location  = new Point(10, 18),
            Checked   = entry.IsSelected,
            BackColor = Color.Transparent,
            ForeColor = AppTheme.TextPrimary,
        };
        _chk.CheckedChanged += (_, _) =>
        {
            _entry.IsSelected = _chk.Checked;
        };

        // Name label
        _lblName = new Label
        {
            AutoSize  = false,
            Size      = new Size(290, 20),
            Location  = new Point(40, 10),
            Font      = AppTheme.FontBold,
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = entry.DisplayName,
        };

        // Category label
        _lblCategory = new Label
        {
            AutoSize  = false,
            Size      = new Size(290, 16),
            Location  = new Point(40, 30),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = entry.Category,
        };

        // Installed version
        _lblInstalled = new Label
        {
            AutoSize  = false,
            Size      = new Size(120, 20),
            Location  = new Point(340, 18),
            Font      = AppTheme.FontMono,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        // Arrow
        _lblArrow = new Label
        {
            AutoSize  = false,
            Size      = new Size(18, 20),
            Location  = new Point(468, 18),
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = "→",
            Visible   = false,
        };

        // Available version
        _lblAvailable = new Label
        {
            AutoSize  = false,
            Size      = new Size(120, 20),
            Location  = new Point(490, 18),
            Font      = AppTheme.FontMono,
            ForeColor = AppTheme.Accent,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Visible   = false,
        };

        // Status badge (owner-drawn)
        _lblStatus = new Label
        {
            AutoSize  = false,
            Size      = new Size(110, 22),
            Location  = new Point(620, 17),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _lblStatus.Paint += OnStatusBadgePaint;

        // Action button
        _btnAction = new Button
        {
            AutoSize  = false,
            Size      = new Size(74, 26),
            Location  = new Point(750, 15),
            Font      = AppTheme.FontButton,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.TextPrimary,
            Cursor    = Cursors.Hand,
            Visible   = false,
        };
        _btnAction.FlatAppearance.BorderSize = 0;
        _btnAction.Click += (_, _) => ActionClicked?.Invoke(this, _entry);

        Controls.AddRange(new Control[] {
            _chk, _lblName, _lblCategory, _lblInstalled,
            _lblArrow, _lblAvailable, _lblStatus, _btnAction
        });

        // Hover effects on all child controls
        foreach (Control c in Controls)
        {
            c.MouseEnter += OnMouseEnterRow;
            c.MouseLeave += OnMouseLeaveRow;
        }
        MouseEnter += OnMouseEnterRow;
        MouseLeave += OnMouseLeaveRow;

        // Selection indicator repaint
        _chk.CheckedChanged += (_, _) => Invalidate();

        Refresh(entry);
    }

    private void OnMouseEnterRow(object? sender, EventArgs e)
    {
        BackColor = AppTheme.BgCardHover;
    }

    private void OnMouseLeaveRow(object? sender, EventArgs e)
    {
        // Only restore if mouse truly left the whole row
        if (!ClientRectangle.Contains(PointToClient(MousePosition)))
            BackColor = (_rowIndex % 2 == 0) ? AppTheme.BgCard : AppTheme.BgPanel;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Bottom border
        using var borderPen = new Pen(AppTheme.Border, 1);
        e.Graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);

        // Left accent bar when selected
        if (_entry.IsSelected)
        {
            using var accentBrush = new SolidBrush(AppTheme.Accent);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 3, Height);
        }
    }

    private void OnStatusBadgePaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        Color badgeColor = _entry.Status switch
        {
            AppStatus.UpToDate        => AppTheme.Success,
            AppStatus.UpdateAvailable => AppTheme.Warning,
            AppStatus.NotInstalled    => AppTheme.TextMuted,
            AppStatus.Installing      => AppTheme.Accent,
            AppStatus.Updated         => AppTheme.Success,
            AppStatus.Failed          => AppTheme.Danger,
            AppStatus.NotManaged      => Color.FromArgb(180, 120, 220),  // purple
            _                         => AppTheme.TextMuted,
        };

        string badgeText = _entry.Status switch
        {
            AppStatus.UpToDate        => "Up to date",
            AppStatus.UpdateAvailable => "Update avail.",
            AppStatus.NotInstalled    => "Not installed",
            AppStatus.Installing      => "Installing...",
            AppStatus.Updated         => "Updated \u2713",
            AppStatus.Failed          => "Failed",
            AppStatus.NotManaged      => "Not via WinGet",
            _                         => "Unknown",
        };

        var ctrl = (Label)sender!;
        var rect = new Rectangle(0, 0, ctrl.Width - 1, ctrl.Height - 1);

        using var bgBrush = new SolidBrush(Color.FromArgb(40, badgeColor));
        using var borderPen = new Pen(Color.FromArgb(120, badgeColor), 1);
        using var textBrush = new SolidBrush(badgeColor);

        int radius = 8;
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

    public void Refresh(AppEntry entry)
    {
        _entry = entry;
        _chk.Checked = entry.IsSelected;
        _lblName.Text = entry.DisplayName;
        _lblCategory.Text = entry.Category;

        if (entry.InstalledVersion != null)
        {
            _lblInstalled.Text      = entry.InstalledVersion;
            _lblInstalled.ForeColor = AppTheme.TextSecondary;
        }
        else
        {
            _lblInstalled.Text      = "Not installed";
            _lblInstalled.ForeColor = AppTheme.TextMuted;
        }

        bool hasUpdate = entry.HasUpdate && !string.IsNullOrWhiteSpace(entry.AvailableVersion);
        _lblArrow.Visible     = hasUpdate;
        _lblAvailable.Visible = hasUpdate;
        if (hasUpdate)
            _lblAvailable.Text = entry.AvailableVersion ?? "";

        // Status badge
        _lblStatus.Invalidate();

        // Action button
        switch (entry.Status)
        {
            case AppStatus.NotInstalled:
            case AppStatus.Unknown when !entry.IsInstalled:
                _btnAction.Text      = "Install";
                _btnAction.BackColor = AppTheme.Accent;
                _btnAction.ForeColor = AppTheme.TextPrimary;
                _btnAction.Visible   = true;
                break;
            case AppStatus.UpdateAvailable:
                _btnAction.Text      = "Update";
                _btnAction.BackColor = AppTheme.Warning;
                _btnAction.ForeColor = Color.FromArgb(30, 30, 30);
                _btnAction.Visible   = true;
                break;
            case AppStatus.NotManaged:
                _btnAction.Text      = "Switch";
                _btnAction.BackColor = Color.FromArgb(100, 60, 160);
                _btnAction.ForeColor = AppTheme.TextPrimary;
                _btnAction.Visible   = true;
                _btnAction.Enabled   = true;
                break;
            case AppStatus.Installing:
                _btnAction.Text      = "...";
                _btnAction.BackColor = AppTheme.Accent;
                _btnAction.ForeColor = AppTheme.TextPrimary;
                _btnAction.Visible   = true;
                _btnAction.Enabled   = false;
                break;
            default:
                _btnAction.Visible  = false;
                _btnAction.Enabled  = true;
                break;
        }

        Invalidate();
    }
}
