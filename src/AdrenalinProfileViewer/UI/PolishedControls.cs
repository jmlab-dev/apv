using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace AdrenalinProfileViewer.UI;

internal enum RoundedButtonStyle
{
    Primary,
    Secondary,
    Danger
}

internal enum UiIconKind
{
    Details,
    Compare,
    Features,
    Xml,
    File
}

internal static class DpiMetrics
{
    public static float Scale(Control control) => Math.Max(96, control.DeviceDpi) / 96f;

    public static int Scale(Control control, int logicalPixels) =>
        Math.Max(1, (int)Math.Round(logicalPixels * Scale(control)));

    public static Size Scale(Control control, Size logicalSize) =>
        new(Scale(control, logicalSize.Width), Scale(control, logicalSize.Height));
}

internal static class UiFonts
{
    public static Font Code(float size, FontStyle style = FontStyle.Regular)
    {
        foreach (var family in new[] { "Cascadia Mono", "Cascadia Code", "Consolas" })
        {
            try
            {
                var font = new Font(family, size, style, GraphicsUnit.Point);
                if (string.Equals(font.FontFamily.Name, family, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }
                font.Dispose();
            }
            catch
            {
                // Try the next installed monospace font.
            }
        }

        return new Font(FontFamily.GenericMonospace, size, style, GraphicsUnit.Point);
    }
}

internal static class UiIconPainter
{
    public static void Draw(Graphics graphics, Rectangle bounds, UiIconKind kind, Color color)
    {
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return;
        }

        var previous = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var inset = Math.Max(1.5f, Math.Min(bounds.Width, bounds.Height) * 0.10f);
        var rect = new RectangleF(
            bounds.Left + inset,
            bounds.Top + inset,
            Math.Max(2f, bounds.Width - (inset * 2f)),
            Math.Max(2f, bounds.Height - (inset * 2f)));
        var stroke = Math.Max(1.25f, Math.Min(bounds.Width, bounds.Height) / 10f);
        using var pen = new Pen(color, stroke)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(color);

        switch (kind)
        {
            case UiIconKind.Details:
                for (var row = 0; row < 3; row++)
                {
                    var y = rect.Top + (rect.Height * (0.18f + row * 0.32f));
                    graphics.FillEllipse(brush, rect.Left, y - stroke, stroke * 1.65f, stroke * 1.65f);
                    graphics.DrawLine(pen, rect.Left + (stroke * 3f), y, rect.Right, y);
                }
                break;

            case UiIconKind.Compare:
            {
                var upperY = rect.Top + rect.Height * 0.32f;
                var lowerY = rect.Top + rect.Height * 0.70f;
                graphics.DrawLine(pen, rect.Left, upperY, rect.Right - stroke * 1.5f, upperY);
                graphics.DrawLine(pen, rect.Right - stroke * 3.2f, upperY - stroke * 1.7f, rect.Right - stroke * 1.2f, upperY);
                graphics.DrawLine(pen, rect.Right - stroke * 3.2f, upperY + stroke * 1.7f, rect.Right - stroke * 1.2f, upperY);
                graphics.DrawLine(pen, rect.Right, lowerY, rect.Left + stroke * 1.5f, lowerY);
                graphics.DrawLine(pen, rect.Left + stroke * 3.2f, lowerY - stroke * 1.7f, rect.Left + stroke * 1.2f, lowerY);
                graphics.DrawLine(pen, rect.Left + stroke * 3.2f, lowerY + stroke * 1.7f, rect.Left + stroke * 1.2f, lowerY);
                break;
            }

            case UiIconKind.Features:
                for (var row = 0; row < 3; row++)
                {
                    var y = rect.Top + rect.Height * (0.18f + row * 0.32f);
                    graphics.DrawLine(pen, rect.Left, y, rect.Right, y);
                    var knobX = row switch
                    {
                        0 => rect.Left + rect.Width * 0.28f,
                        1 => rect.Left + rect.Width * 0.70f,
                        _ => rect.Left + rect.Width * 0.45f
                    };
                    graphics.FillEllipse(brush, knobX - stroke * 1.15f, y - stroke * 1.15f, stroke * 2.3f, stroke * 2.3f);
                }
                break;

            case UiIconKind.Xml:
            {
                var middleY = rect.Top + rect.Height * 0.5f;
                var quarter = rect.Width * 0.24f;
                graphics.DrawLine(pen, rect.Left + quarter, rect.Top + rect.Height * 0.20f, rect.Left, middleY);
                graphics.DrawLine(pen, rect.Left, middleY, rect.Left + quarter, rect.Bottom - rect.Height * 0.20f);
                graphics.DrawLine(pen, rect.Right - quarter, rect.Top + rect.Height * 0.20f, rect.Right, middleY);
                graphics.DrawLine(pen, rect.Right, middleY, rect.Right - quarter, rect.Bottom - rect.Height * 0.20f);
                graphics.DrawLine(pen, rect.Left + rect.Width * 0.58f, rect.Top + rect.Height * 0.12f, rect.Left + rect.Width * 0.42f, rect.Bottom - rect.Height * 0.12f);
                break;
            }

            case UiIconKind.File:
            {
                var fold = rect.Width * 0.28f;
                using var path = new GraphicsPath();
                path.StartFigure();
                path.AddLine(rect.Left, rect.Top, rect.Right - fold, rect.Top);
                path.AddLine(rect.Right - fold, rect.Top, rect.Right, rect.Top + fold);
                path.AddLine(rect.Right, rect.Top + fold, rect.Right, rect.Bottom);
                path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
                path.CloseFigure();
                graphics.DrawPath(pen, path);
                graphics.DrawLine(pen, rect.Right - fold, rect.Top, rect.Right - fold, rect.Top + fold);
                graphics.DrawLine(pen, rect.Right - fold, rect.Top + fold, rect.Right, rect.Top + fold);
                graphics.DrawLine(pen, rect.Left + rect.Width * 0.20f, rect.Top + rect.Height * 0.58f, rect.Right - rect.Width * 0.18f, rect.Top + rect.Height * 0.58f);
                graphics.DrawLine(pen, rect.Left + rect.Width * 0.20f, rect.Top + rect.Height * 0.76f, rect.Right - rect.Width * 0.30f, rect.Top + rect.Height * 0.76f);
                break;
            }
        }

        graphics.SmoothingMode = previous;
    }
}

internal sealed class IconGlyph : Control, IThemeAware
{
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private UiIconKind _iconKind = UiIconKind.File;

    public UiIconKind IconKind
    {
        get => _iconKind;
        set
        {
            _iconKind = value;
            Invalidate();
        }
    }

    public IconGlyph()
    {
        DoubleBuffered = true;
        MinimumSize = new Size(16, 16);
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var side = Math.Max(8, Math.Min(Width, Height) - DpiMetrics.Scale(this, 2));
        var bounds = new Rectangle((Width - side) / 2, (Height - side) / 2, side, side);
        UiIconPainter.Draw(e.Graphics, bounds, IconKind, _palette.Accent);
    }
}

internal sealed class RoundedButton : Button, IThemeAware
{
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private bool _hovered;
    private bool _pressed;

    public RoundedButtonStyle ButtonStyle { get; set; } = RoundedButtonStyle.Secondary;
    public int CornerRadius { get; set; } = 8;

    public RoundedButton()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Margin = new Padding(0, 0, 8, 0);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
        Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(
            Text,
            Font,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        var horizontal = DpiMetrics.Scale(this, 30);
        var vertical = DpiMetrics.Scale(this, 18);
        var minimumHeight = DpiMetrics.Scale(this, 42);
        return new Size(text.Width + horizontal, Math.Max(minimumHeight, text.Height + vertical));
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        ForeColor = ButtonStyle == RoundedButtonStyle.Secondary && !palette.IsDark
            ? palette.Text
            : Color.White;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        PerformLayout();
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = RoundedGeometry.Create(
            new Rectangle(0, 0, Width, Height),
            DpiMetrics.Scale(this, CornerRadius));
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = RoundedGeometry.Create(rect, DpiMetrics.Scale(this, CornerRadius));

        var baseColor = ButtonStyle switch
        {
            RoundedButtonStyle.Primary => _palette.Accent,
            RoundedButtonStyle.Danger => _palette.Danger,
            _ => _palette.SurfaceRaised
        };

        var hoverColor = ButtonStyle switch
        {
            RoundedButtonStyle.Primary => _palette.AccentHover,
            RoundedButtonStyle.Danger => ControlPaint.Light(_palette.Danger, 0.10f),
            _ => ControlPaint.Light(baseColor, 0.08f)
        };
        var fill = _pressed
            ? ControlPaint.Dark(baseColor, 0.12f)
            : _hovered ? hoverColor : baseColor;

        using var brush = new SolidBrush(fill);
        pevent.Graphics.FillPath(brush, path);

        if (ButtonStyle == RoundedButtonStyle.Secondary)
        {
            using var pen = new Pen(_palette.Border);
            pevent.Graphics.DrawPath(pen, path);
        }

        var textColor = ButtonStyle == RoundedButtonStyle.Secondary && !_palette.IsDark
            ? _palette.Text
            : Color.White;
        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            ClientRectangle,
            textColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(
                ClientRectangle,
                -DpiMetrics.Scale(this, 4),
                -DpiMetrics.Scale(this, 4));
            ControlPaint.DrawFocusRectangle(pevent.Graphics, focus, textColor, fill);
        }
    }
}

internal sealed class RoundedPanel : Panel, IThemeAware
{
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);

    public int CornerRadius { get; set; } = 10;
    public SurfaceLevel SurfaceLevel { get; set; } = SurfaceLevel.Surface;
    public bool DrawBorder { get; set; } = true;

    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = SurfaceLevel switch
        {
            SurfaceLevel.Background => palette.Background,
            SurfaceLevel.Raised => palette.SurfaceRaised,
            _ => palette.Surface
        };
        Invalidate();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UpdateRegion();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = RoundedGeometry.Create(
            new Rectangle(0, 0, Width, Height),
            DpiMetrics.Scale(this, CornerRadius));
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = RoundedGeometry.Create(rect, DpiMetrics.Scale(this, CornerRadius));
        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillPath(fill, path);
        if (DrawBorder)
        {
            using var border = new Pen(_palette.Border);
            e.Graphics.DrawPath(border, path);
        }
        base.OnPaint(e);
    }
}

internal sealed class BrandHeader : Control, IThemeAware
{
    private const string LogoResourceName = "AdrenalinProfileViewer.Assets.RadeonGraphicsLogo.png";
    private const string MatrixCharacters = "01ABCDEF<>[]{}:+-*/RX9070XT";
    private static readonly Lazy<Image?> LogoImage = new(LoadLogo);
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 85 };
    private readonly Random _random = new(9070);
    private readonly List<MatrixDrop> _drops = [];
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);

    public BrandHeader()
    {
        DoubleBuffered = true;
        MinimumSize = new Size(430, 78);
        Dock = DockStyle.Fill;
        AccessibleName = "AMD Radeon Graphics logo and application title";
        _animationTimer.Tick += (_, _) => AdvanceAnimation();
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ResetAnimation();
        _animationTimer.Start();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _animationTimer.Stop();
        base.OnHandleDestroyed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ResetAnimation();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // A custom-painted WinForms control is replaced by a red-X placeholder when an
        // exception escapes OnPaint. Keep the decorative animation strictly best effort:
        // branding and the rest of the application must remain usable even if a display
        // driver, DPI transition or unusual graphics state rejects one animation frame.
        try
        {
            DrawMatrixBackground(e.Graphics);
        }
        catch
        {
            _drops.Clear();
            _animationTimer.Stop();
        }

        try
        {
            DrawBranding(e.Graphics);
        }
        catch
        {
            try
            {
                DrawFallbackBranding(e.Graphics);
            }
            catch
            {
                // Never let decorative header painting replace the control with WinForms' red-X placeholder.
            }
        }
    }

    private void DrawBranding(Graphics graphics)
    {
        var scale = DpiMetrics.Scale(this);
        var outerPadding = Math.Max(8, (int)Math.Round(8 * scale));
        var availableHeight = Math.Max(28, Height - (outerPadding * 2));
        var logoBoxSide = Math.Min(availableHeight, Math.Max(48, (int)Math.Round(64 * scale)));
        var logoBox = new Rectangle(
            outerPadding,
            Math.Max(outerPadding, (Height - logoBoxSide) / 2),
            logoBoxSide,
            logoBoxSide);

        var logo = LogoImage.Value;
        if (logo is not null)
        {
            var logoRect = FitInside(logoBox, logo.Size, Math.Max(2, (int)Math.Round(3 * scale)));
            using var attributes = new ImageAttributes();
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(
                logo,
                logoRect,
                0,
                0,
                logo.Width,
                logo.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        var textLeft = logoBox.Right + Math.Max(14, (int)Math.Round(16 * scale));
        var textWidth = Math.Max(30, Width - textLeft - outerPadding);
        var titleHeight = Math.Max(28, (int)Math.Round(31 * scale));
        var subtitleHeight = Math.Max(17, (int)Math.Round(19 * scale));
        var combinedHeight = titleHeight + subtitleHeight;
        var textTop = Math.Max(outerPadding, (Height - combinedHeight) / 2);

        using var titleFont = UiFonts.Code(15.5f, FontStyle.Bold);
        using var subtitleFont = UiFonts.Code(8.25f, FontStyle.Regular);

        TextRenderer.DrawText(
            graphics,
            "AMD ADRENALIN PROFILE VIEWER",
            titleFont,
            new Rectangle(textLeft, textTop, textWidth, titleHeight),
            _palette.Text,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            graphics,
            "[ tuning_profile_library :: compare :: inspect ]",
            subtitleFont,
            new Rectangle(textLeft, textTop + titleHeight, textWidth, subtitleHeight),
            _palette.Muted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    private void DrawFallbackBranding(Graphics graphics)
    {
        using var font = UiFonts.Code(13f, FontStyle.Bold);
        var bounds = Rectangle.Inflate(ClientRectangle, -DpiMetrics.Scale(this, 12), -DpiMetrics.Scale(this, 8));
        TextRenderer.DrawText(
            graphics,
            "AMD ADRENALIN PROFILE VIEWER",
            font,
            bounds,
            _palette.Text,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    private void DrawMatrixBackground(Graphics graphics)
    {
        if (_drops.Count == 0 || Width < 160 || Height < 40)
        {
            return;
        }

        using var font = UiFonts.Code(7.25f, FontStyle.Bold);
        var darkAlpha = _palette.IsDark ? 42 : 22;
        var headAlpha = _palette.IsDark ? 74 : 36;
        var cellHeight = Math.Max(9, TextRenderer.MeasureText("0", font).Height - 1);

        foreach (var drop in _drops)
        {
            for (var trail = 5; trail >= 0; trail--)
            {
                var y = (int)Math.Round(drop.Y - (trail * cellHeight));
                if (y < -cellHeight || y > Height)
                {
                    continue;
                }

                var alpha = trail == 0
                    ? headAlpha
                    : Math.Max(7, darkAlpha - (trail * 6));
                var baseColor = _palette.IsDark ? _palette.Accent : Color.FromArgb(180, 32, 24);
                var color = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
                // C# keeps the sign on the remainder. Drops begin above the visible header,
                // so a negative Y position can produce a negative array/string index unless the
                // value is normalized into the 0..Length-1 range.
                var rowOffset = (long)Math.Floor(drop.Y / Math.Max(1d, cellHeight));
                var rawCharacterIndex = (long)drop.CharacterOffset + trail + rowOffset;
                var characterIndex = PositiveModulo(rawCharacterIndex, MatrixCharacters.Length);
                var text = MatrixCharacters[characterIndex].ToString();
                TextRenderer.DrawText(
                    graphics,
                    text,
                    font,
                    new Point(drop.X, y),
                    color,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
        }
    }

    private static int PositiveModulo(long value, int modulus)
    {
        if (modulus <= 0)
        {
            return 0;
        }

        var remainder = value % modulus;
        return (int)(remainder < 0 ? remainder + modulus : remainder);
    }

    private void AdvanceAnimation()
    {
        if (!Visible || _drops.Count == 0)
        {
            return;
        }

        foreach (var drop in _drops)
        {
            drop.Y += drop.Speed;
            if (drop.Y > Height + 52)
            {
                drop.Y = -_random.Next(10, Math.Max(20, Height));
                drop.Speed = 1.4f + (float)_random.NextDouble() * 2.5f;
                drop.CharacterOffset = _random.Next(MatrixCharacters.Length);
            }
        }

        Invalidate();
    }

    private void ResetAnimation()
    {
        _drops.Clear();
        if (Width <= 0)
        {
            return;
        }

        var spacing = Math.Max(18, DpiMetrics.Scale(this, 18));
        var columns = Math.Max(10, Width / spacing);
        for (var index = 0; index < columns; index++)
        {
            _drops.Add(new MatrixDrop
            {
                X = index * spacing + _random.Next(0, Math.Max(1, spacing / 3)),
                Y = _random.Next(-Math.Max(20, Height), Math.Max(21, Height + 1)),
                Speed = 1.4f + (float)_random.NextDouble() * 2.5f,
                CharacterOffset = _random.Next(MatrixCharacters.Length)
            });
        }
    }

    private static Rectangle FitInside(Rectangle bounds, Size imageSize, int inset)
    {
        var inner = Rectangle.Inflate(bounds, -inset, -inset);
        if (imageSize.Width <= 0 || imageSize.Height <= 0 || inner.Width <= 0 || inner.Height <= 0)
        {
            return inner;
        }

        var ratio = Math.Min(inner.Width / (double)imageSize.Width, inner.Height / (double)imageSize.Height);
        var width = Math.Max(1, (int)Math.Floor(imageSize.Width * ratio));
        var height = Math.Max(1, (int)Math.Floor(imageSize.Height * ratio));
        return new Rectangle(
            inner.X + ((inner.Width - width) / 2),
            inner.Y + ((inner.Height - height) / 2),
            width,
            height);
    }

    private static Image? LoadLogo()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResourceName);
            if (stream is null)
            {
                return null;
            }

            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private sealed class MatrixDrop
    {
        public int X { get; init; }
        public float Y { get; set; }
        public float Speed { get; set; }
        public int CharacterOffset { get; set; }
    }
}

internal sealed class MetricCard : Control, IThemeAware
{
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private string _caption = string.Empty;
    private string _value = "—";
    private string _detail = string.Empty;

    public string Caption
    {
        get => _caption;
        set { _caption = value; Invalidate(); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    public string Detail
    {
        get => _detail;
        set { _detail = value; Invalidate(); }
    }

    public MetricCard()
    {
        Margin = new Padding(0, 0, 10, 0);
        DoubleBuffered = true;
        RefreshDpiMetrics();
    }

    public void RefreshDpiMetrics()
    {
        var preferredHeight = Math.Max(116, DpiMetrics.Scale(this, 108));
        MinimumSize = new Size(DpiMetrics.Scale(this, 48), preferredHeight);
        Height = preferredHeight;
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize) =>
        new(Math.Max(MinimumSize.Width, proposedSize.Width), MinimumSize.Height);

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.SurfaceRaised;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        RefreshDpiMetrics();
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = RoundedGeometry.Create(
            new Rectangle(0, 0, Width, Height),
            DpiMetrics.Scale(this, 10));
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = RoundedGeometry.Create(rect, DpiMetrics.Scale(this, 10));
        using var fill = new SolidBrush(_palette.SurfaceRaised);
        using var border = new Pen(_palette.Border);
        using var accent = new SolidBrush(_palette.Accent);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        e.Graphics.FillRectangle(accent, 0, 0, Math.Max(4, DpiMetrics.Scale(this, 4)), Height);

        using var captionFont = new Font("Segoe UI Semibold", 7.9f, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 7.9f);

        var left = Math.Max(13, DpiMetrics.Scale(this, 13));
        var rightPadding = Math.Max(7, DpiMetrics.Scale(this, 7));
        var width = Math.Max(10, Width - left - rightPadding);
        var singleCaptionLine = Math.Max(16, TextRenderer.MeasureText("Ag", captionFont).Height);
        var captionHeight = singleCaptionLine * 2;
        var detailLineHeight = Math.Max(15, TextRenderer.MeasureText("Ag", detailFont).Height);
        var detailHeight = detailLineHeight * 2;
        var top = Math.Max(7, DpiMetrics.Scale(this, 7));
        var valueHeight = Math.Max(26, DpiMetrics.Scale(this, 25));
        var detailTop = Math.Max(
            top + captionHeight + valueHeight + DpiMetrics.Scale(this, 5),
            Height - detailHeight - Math.Max(8, DpiMetrics.Scale(this, 8)));
        var valueTop = Math.Min(
            detailTop - valueHeight - DpiMetrics.Scale(this, 3),
            top + captionHeight + DpiMetrics.Scale(this, 1));
        var valueBounds = new Rectangle(left, valueTop, width, valueHeight);

        TextRenderer.DrawText(
            e.Graphics,
            Caption.ToUpperInvariant(),
            captionFont,
            new Rectangle(left, top, width, captionHeight),
            _palette.Muted,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        // Plain theme-colored reading with automatic font fitting. This keeps the original
        // clean text appearance while preventing MHz and mV values from being truncated.
        using var valueFont = CreateFittingValueFont(Value, valueBounds.Width);
        TextRenderer.DrawText(
            e.Graphics,
            Value,
            valueFont,
            valueBounds,
            _palette.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            e.Graphics,
            Detail,
            detailFont,
            new Rectangle(left, detailTop, width, detailHeight),
            _palette.Muted,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static Font CreateFittingValueFont(string text, int availableWidth)
    {
        const float preferredSize = 11.25f;
        const float minimumSize = 8.25f;
        const float decrement = 0.25f;
        var safeText = string.IsNullOrWhiteSpace(text) ? "—" : text;
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

        for (var size = preferredSize; size >= minimumSize; size -= decrement)
        {
            var candidate = new Font("Segoe UI Semibold", size, FontStyle.Bold);
            var measured = TextRenderer.MeasureText(
                safeText,
                candidate,
                new Size(int.MaxValue, int.MaxValue),
                flags).Width;
            if (measured <= Math.Max(1, availableWidth))
            {
                return candidate;
            }

            candidate.Dispose();
        }

        return new Font("Segoe UI Semibold", minimumSize, FontStyle.Bold);
    }
}

internal sealed class PolishedTabHost : Panel, IThemeAware
{
    private readonly TableLayoutPanel _navigation = new();
    private readonly Panel _contentHost = new();
    private readonly List<Control> _pages = [];
    private readonly List<TabNavigationButton> _buttons = [];
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private int _selectedIndex = -1;

    public event EventHandler? SelectedIndexChanged;

    public int PageCount => _pages.Count;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_pages.Count == 0)
            {
                _selectedIndex = -1;
                return;
            }

            var target = Math.Clamp(value, 0, _pages.Count - 1);
            if (_selectedIndex == target)
            {
                return;
            }

            _selectedIndex = target;
            for (var index = 0; index < _pages.Count; index++)
            {
                _pages[index].Visible = index == target;
                _buttons[index].Selected = index == target;
            }

            _pages[target].BringToFront();
            Invalidate(true);
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public PolishedTabHost()
    {
        DoubleBuffered = true;
        Padding = Padding.Empty;

        _navigation.Dock = DockStyle.Top;
        _navigation.RowCount = 1;
        _navigation.ColumnCount = 0;
        _navigation.Padding = new Padding(0, 0, 0, 6);
        _navigation.Margin = Padding.Empty;
        _navigation.Tag = ThemeRole.Surface;

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.Padding = Padding.Empty;
        _contentHost.Margin = Padding.Empty;
        _contentHost.Tag = ThemeRole.Surface;

        Controls.Add(_contentHost);
        Controls.Add(_navigation);
        RefreshDpiMetrics();
    }

    public void AddPage(Control page, string title, UiIconKind iconKind)
    {
        var index = _pages.Count;
        page.Dock = DockStyle.Fill;
        page.Margin = Padding.Empty;
        page.Visible = false;
        page.Tag ??= ThemeRole.Surface;
        _pages.Add(page);
        _contentHost.Controls.Add(page);

        _navigation.ColumnCount = index + 1;
        _navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        var button = new TabNavigationButton
        {
            Text = title,
            IconKind = iconKind,
            Dock = DockStyle.Fill,
            Margin = new Padding(index == 0 ? 0 : 3, 0, index == 0 ? 3 : 0, 0)
        };
        button.Click += (_, _) => SelectedIndex = index;
        _buttons.Add(button);
        _navigation.Controls.Add(button, index, 0);
        button.ApplyTheme(_palette);

        if (_selectedIndex < 0)
        {
            SelectedIndex = 0;
        }
    }

    public void RefreshDpiMetrics()
    {
        _navigation.Height = Math.Max(44, DpiMetrics.Scale(this, 42));
        foreach (var button in _buttons)
        {
            button.Invalidate();
        }
        PerformLayout();
        Invalidate(true);
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        _navigation.BackColor = palette.Surface;
        _contentHost.BackColor = palette.Surface;
        foreach (var button in _buttons)
        {
            button.ApplyTheme(palette);
        }
        foreach (var page in _pages)
        {
            page.BackColor = palette.Surface;
            page.ForeColor = palette.Text;
        }
        RefreshDpiMetrics();
    }
}

internal sealed class TabNavigationButton : Control, IThemeAware
{
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private bool _hovered;
    private bool _selected;

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    public UiIconKind IconKind { get; set; } = UiIconKind.Details;

    public TabNavigationButton()
    {
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        TabStop = true;
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        ForeColor = palette.Text;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        Focus();
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = RoundedGeometry.Create(rect, DpiMetrics.Scale(this, 8));
        var fillColor = Selected
            ? _palette.SurfaceRaised
            : _hovered ? ControlPaint.Light(_palette.Surface, _palette.IsDark ? 0.06f : 0.02f) : _palette.Surface;
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(Selected ? _palette.Accent : _palette.Border, Selected ? 1.5f : 1f);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        if (Selected)
        {
            using var accent = new SolidBrush(_palette.Accent);
            var lineHeight = Math.Max(2, DpiMetrics.Scale(this, 2));
            e.Graphics.FillRectangle(accent, DpiMetrics.Scale(this, 8), Height - lineHeight - 1, Math.Max(1, Width - DpiMetrics.Scale(this, 16)), lineHeight);
        }

        var contentColor = Selected ? _palette.Text : _palette.Muted;
        var iconSize = Math.Max(12, DpiMetrics.Scale(this, 14));
        var gap = DpiMetrics.Scale(this, 5);
        var horizontalPadding = DpiMetrics.Scale(this, 8);
        var textSize = TextRenderer.MeasureText(
            Text,
            Font,
            new Size(Math.Max(1, rect.Width - iconSize - gap - horizontalPadding * 2), rect.Height),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        var availableTextWidth = Math.Max(8, rect.Width - iconSize - gap - horizontalPadding * 2);
        var renderedTextWidth = Math.Min(textSize.Width, availableTextWidth);
        var groupWidth = iconSize + gap + renderedTextWidth;
        var startX = rect.Left + Math.Max(horizontalPadding, (rect.Width - groupWidth) / 2);
        var iconBounds = new Rectangle(
            startX,
            rect.Top + Math.Max(0, (rect.Height - iconSize) / 2),
            iconSize,
            iconSize);
        UiIconPainter.Draw(e.Graphics, iconBounds, IconKind, Selected ? _palette.Accent : contentColor);
        var textBounds = new Rectangle(
            iconBounds.Right + gap,
            rect.Top,
            Math.Max(8, rect.Right - (iconBounds.Right + gap) - horizontalPadding),
            rect.Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            contentColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(rect, -4, -4);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, _palette.Text, fillColor);
        }
    }
}

internal sealed class DarkScrollHost : Panel, IThemeAware
{
    private readonly Panel _viewport = new();
    private readonly DarkVerticalScrollBar _scrollBar = new();
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private Control? _content;
    private int _contentPadding = 12;
    private int _maximumContentWidth;

    public int ContentPadding
    {
        get => _contentPadding;
        set
        {
            _contentPadding = Math.Max(0, value);
            UpdateContentLayout();
        }
    }

    /// <summary>
    /// Optional maximum width in logical (96-DPI) pixels. Zero keeps the legacy fill-width
    /// behavior. This is useful for compact inspector panes where cards should not stretch just
    /// because the main window is maximized.
    /// </summary>
    public int MaximumContentWidth
    {
        get => _maximumContentWidth;
        set
        {
            _maximumContentWidth = Math.Max(0, value);
            UpdateContentLayout();
        }
    }

    public DarkScrollHost()
    {
        DoubleBuffered = true;
        TabStop = true;
        _viewport.Dock = DockStyle.Fill;
        _viewport.Margin = Padding.Empty;
        _viewport.Padding = Padding.Empty;
        _viewport.Tag = ThemeRole.Surface;
        _viewport.Resize += (_, _) => UpdateContentLayout();
        _viewport.MouseWheel += OnViewportMouseWheel;
        _viewport.MouseEnter += (_, _) => Focus();

        _scrollBar.Dock = DockStyle.Right;
        _scrollBar.Width = 15;
        _scrollBar.Margin = Padding.Empty;
        _scrollBar.ValueChanged += (_, _) => PositionContent();

        Controls.Add(_viewport);
        Controls.Add(_scrollBar);
    }

    public void SetContent(Control content)
    {
        if (_content is not null)
        {
            _content.SizeChanged -= OnContentSizeChanged;
            _viewport.Controls.Remove(_content);
        }

        _content = content;
        _content.Dock = DockStyle.None;
        _content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _content.Margin = Padding.Empty;
        _content.SizeChanged += OnContentSizeChanged;
        _viewport.Controls.Add(_content);
        _content.BringToFront();
        UpdateContentLayout();
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        _viewport.BackColor = palette.Surface;
        _scrollBar.ApplyTheme(palette);
        Invalidate(true);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        _scrollBar.Width = Math.Max(14, DpiMetrics.Scale(this, 14));
        UpdateContentLayout();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        ScrollByWheel(e.Delta);
        base.OnMouseWheel(e);
    }

    private void OnViewportMouseWheel(object? sender, MouseEventArgs e) => ScrollByWheel(e.Delta);

    private void ScrollByWheel(int delta)
    {
        if (_scrollBar.Maximum <= 0)
        {
            return;
        }

        var lines = Math.Max(1, SystemInformation.MouseWheelScrollLines);
        var step = Math.Max(24, DpiMetrics.Scale(this, 24));
        _scrollBar.Value -= Math.Sign(delta) * step * lines;
    }

    private void OnContentSizeChanged(object? sender, EventArgs e) => UpdateContentLayout();

    private void UpdateContentLayout()
    {
        if (_content is null || _viewport.ClientSize.Width <= 0)
        {
            return;
        }

        var padding = Math.Max(8, DpiMetrics.Scale(this, _contentPadding));
        var availableWidth = Math.Max(10, _viewport.ClientSize.Width - (padding * 2));
        var maximumWidth = _maximumContentWidth > 0
            ? DpiMetrics.Scale(this, _maximumContentWidth)
            : int.MaxValue;
        _content.Width = Math.Min(availableWidth, maximumWidth);
        _content.PerformLayout();

        var fullHeight = _content.Height + (padding * 2);
        var maximum = Math.Max(0, fullHeight - _viewport.ClientSize.Height);
        _scrollBar.LargeChange = Math.Max(1, _viewport.ClientSize.Height);
        _scrollBar.Maximum = maximum;
        _scrollBar.Visible = maximum > 0;
        if (maximum == 0)
        {
            _scrollBar.Value = 0;
        }
        PositionContent();
    }

    private void PositionContent()
    {
        if (_content is null)
        {
            return;
        }

        var padding = Math.Max(8, DpiMetrics.Scale(this, _contentPadding));
        _content.Location = new Point(padding, padding - _scrollBar.Value);
    }
}

internal sealed class DarkVerticalScrollBar : Control, IThemeAware
{
    private ThemePalette _palette = ThemeCatalog.Get(AppThemeKind.DarkRed);
    private int _maximum;
    private int _value;
    private int _largeChange = 100;
    private bool _dragging;
    private int _dragOffset;
    private bool _hovered;

    public event EventHandler? ValueChanged;

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(0, value);
            Value = Math.Min(_value, _maximum);
            Invalidate();
        }
    }

    public int LargeChange
    {
        get => _largeChange;
        set
        {
            _largeChange = Math.Max(1, value);
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, 0, Maximum);
            if (_value == next)
            {
                return;
            }

            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public DarkVerticalScrollBar()
    {
        Cursor = Cursors.Hand;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
    }

    public void ApplyTheme(ThemePalette palette)
    {
        _palette = palette;
        BackColor = palette.Surface;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (!_dragging)
        {
            _hovered = false;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || Maximum <= 0)
        {
            base.OnMouseDown(e);
            return;
        }

        var thumb = GetThumbRectangle();
        if (thumb.Contains(e.Location))
        {
            _dragging = true;
            _dragOffset = e.Y - thumb.Y;
            Capture = true;
        }
        else
        {
            Value += e.Y < thumb.Y ? -LargeChange : LargeChange;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            var track = GetTrackRectangle();
            var thumb = GetThumbRectangle();
            var travel = Math.Max(1, track.Height - thumb.Height);
            var y = Math.Clamp(e.Y - _dragOffset - track.Y, 0, travel);
            Value = (int)Math.Round(Maximum * (y / (double)travel));
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            Capture = false;
            _hovered = ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(_palette.Surface);
        if (Maximum <= 0)
        {
            return;
        }

        var track = GetTrackRectangle();
        using var trackPath = RoundedGeometry.Create(track, Math.Max(3, track.Width / 2));
        using var trackBrush = new SolidBrush(_palette.IsDark
            ? ControlPaint.Light(_palette.Background, 0.04f)
            : ControlPaint.Dark(_palette.SurfaceRaised, 0.03f));
        e.Graphics.FillPath(trackBrush, trackPath);

        var thumb = GetThumbRectangle();
        using var thumbPath = RoundedGeometry.Create(thumb, Math.Max(3, thumb.Width / 2));
        var thumbColor = _dragging
            ? _palette.Accent
            : _hovered ? _palette.AccentHover : _palette.Border;
        using var thumbBrush = new SolidBrush(thumbColor);
        e.Graphics.FillPath(thumbBrush, thumbPath);
    }

    private Rectangle GetTrackRectangle()
    {
        var inset = Math.Max(3, Width / 4);
        return new Rectangle(
            inset,
            Math.Max(4, DpiMetrics.Scale(this, 4)),
            Math.Max(4, Width - (inset * 2)),
            Math.Max(10, Height - Math.Max(8, DpiMetrics.Scale(this, 8))));
    }

    private Rectangle GetThumbRectangle()
    {
        var track = GetTrackRectangle();
        var extent = Maximum + LargeChange;
        var thumbHeight = extent <= 0
            ? track.Height
            : Math.Max(Math.Min(track.Height, DpiMetrics.Scale(this, 28)), (int)Math.Round(track.Height * (LargeChange / (double)extent)));
        var travel = Math.Max(0, track.Height - thumbHeight);
        var y = Maximum <= 0 ? track.Y : track.Y + (int)Math.Round(travel * (Value / (double)Maximum));
        return new Rectangle(track.X, y, track.Width, thumbHeight);
    }
}
