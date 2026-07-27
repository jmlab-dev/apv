using System.Drawing.Drawing2D;

namespace AdrenalinProfileViewer.UI;

public enum AppThemeKind
{
    DarkRed,
    DarkOrange,
    White
}

public enum SurfaceLevel
{
    Background,
    Surface,
    Raised
}

internal enum ThemeRole
{
    Background,
    Surface,
    Raised,
    Heading,
    Muted,
    Value,
    Accent,
    Footer
}

public sealed record ThemePalette(
    string DisplayName,
    bool IsDark,
    Color Background,
    Color Surface,
    Color SurfaceRaised,
    Color Border,
    Color Text,
    Color Muted,
    Color Accent,
    Color AccentHover,
    Color Selection,
    Color Positive,
    Color Warning,
    Color Danger);

public static class ThemeCatalog
{
    public static readonly List<AppThemeKind> All =
        [AppThemeKind.DarkRed, AppThemeKind.DarkOrange, AppThemeKind.White];

    public static ThemePalette Get(AppThemeKind kind) => kind switch
    {
        AppThemeKind.DarkOrange => new ThemePalette(
            "Dark orange",
            true,
            Color.FromArgb(17, 18, 21),
            Color.FromArgb(27, 29, 34),
            Color.FromArgb(38, 41, 48),
            Color.FromArgb(62, 66, 76),
            Color.FromArgb(244, 245, 248),
            Color.FromArgb(164, 169, 180),
            Color.FromArgb(255, 106, 0),
            Color.FromArgb(255, 132, 42),
            Color.FromArgb(167, 73, 10),
            Color.FromArgb(74, 202, 132),
            Color.FromArgb(255, 187, 78),
            Color.FromArgb(235, 82, 82)),
        AppThemeKind.White => new ThemePalette(
            "White",
            false,
            Color.FromArgb(243, 245, 248),
            Color.White,
            Color.FromArgb(234, 237, 242),
            Color.FromArgb(204, 209, 218),
            Color.FromArgb(30, 33, 39),
            Color.FromArgb(102, 109, 121),
            Color.FromArgb(210, 42, 34),
            Color.FromArgb(232, 64, 55),
            Color.FromArgb(220, 68, 59),
            Color.FromArgb(37, 145, 89),
            Color.FromArgb(178, 112, 10),
            Color.FromArgb(196, 48, 48)),
        _ => new ThemePalette(
            "Dark red (AMD)",
            true,
            Color.FromArgb(17, 18, 21),
            Color.FromArgb(27, 29, 34),
            Color.FromArgb(38, 41, 48),
            Color.FromArgb(62, 66, 76),
            Color.FromArgb(244, 245, 248),
            Color.FromArgb(164, 169, 180),
            Color.FromArgb(226, 35, 26),
            Color.FromArgb(244, 62, 53),
            Color.FromArgb(151, 29, 24),
            Color.FromArgb(74, 202, 132),
            Color.FromArgb(255, 187, 78),
            Color.FromArgb(235, 82, 82))
    };

    public static AppThemeKind Parse(string? value)
    {
        return Enum.TryParse<AppThemeKind>(value, ignoreCase: true, out var parsed)
            ? parsed
            : AppThemeKind.DarkRed;
    }

    public static string DisplayName(AppThemeKind kind) => Get(kind).DisplayName;
}

internal interface IThemeAware
{
    void ApplyTheme(ThemePalette palette);
}

internal static class RoundedGeometry
{
    public static GraphicsPath Create(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        var diameter = Math.Max(2, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal static class ThemeStyler
{
    public static void Apply(Control root, ThemePalette palette)
    {
        ApplyOne(root, palette);
        foreach (Control child in root.Controls)
        {
            Apply(child, palette);
        }
    }

    public static void StyleGrid(DataGridView grid, ThemePalette palette)
    {
        grid.BackgroundColor = palette.Surface;
        grid.GridColor = palette.Border;
        grid.DefaultCellStyle.BackColor = palette.Surface;
        grid.DefaultCellStyle.ForeColor = palette.Text;
        grid.DefaultCellStyle.SelectionBackColor = palette.Selection;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.NullValue = "—";
        grid.AlternatingRowsDefaultCellStyle.BackColor = palette.SurfaceRaised;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = palette.Text;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = palette.Selection;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.BackColor = palette.SurfaceRaised;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = palette.SurfaceRaised;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = palette.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.RowHeadersDefaultCellStyle.BackColor = palette.SurfaceRaised;
        grid.RowHeadersDefaultCellStyle.ForeColor = palette.Muted;
        grid.RowHeadersDefaultCellStyle.SelectionBackColor = palette.Selection;
        grid.RowHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.EnableHeadersVisualStyles = false;
        grid.Invalidate();
    }

    private static void ApplyOne(Control control, ThemePalette palette)
    {
        if (control is IThemeAware themed)
        {
            themed.ApplyTheme(palette);
            return;
        }

        switch (control)
        {
            case Form form:
                form.BackColor = palette.Background;
                form.ForeColor = palette.Text;
                break;
            case DataGridView grid:
                StyleGrid(grid, palette);
                break;
            case RichTextBox rich:
                rich.BackColor = palette.Background;
                rich.ForeColor = palette.Text;
                break;
            case TextBox text:
                text.BackColor = palette.Background;
                text.ForeColor = palette.Text;
                break;
            case ComboBox combo:
                combo.BackColor = palette.Background;
                combo.ForeColor = palette.Text;
                break;
            case TabPage page:
                page.BackColor = palette.Surface;
                page.ForeColor = palette.Text;
                break;
            case StatusStrip strip:
                strip.BackColor = palette.SurfaceRaised;
                strip.ForeColor = palette.Muted;
                foreach (ToolStripItem item in strip.Items)
                {
                    item.ForeColor = item.Tag is ThemeRole.Footer ? palette.Accent : palette.Muted;
                }
                break;
            case Label label:
                label.ForeColor = label.Tag switch
                {
                    ThemeRole.Muted => palette.Muted,
                    ThemeRole.Accent => palette.Accent,
                    ThemeRole.Footer => palette.Accent,
                    _ => palette.Text
                };
                break;
            case Panel panel:
                panel.BackColor = panel.Tag switch
                {
                    ThemeRole.Surface => palette.Surface,
                    ThemeRole.Raised => palette.SurfaceRaised,
                    _ => palette.Background
                };
                break;
        }
    }
}
