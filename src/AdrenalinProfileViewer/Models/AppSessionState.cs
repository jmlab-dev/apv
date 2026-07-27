namespace AdrenalinProfileViewer.Models;

public sealed class AppSessionState
{
    public int SchemaVersion { get; set; } = 13;
    public string Theme { get; set; } = "DarkRed";
    public WindowLayoutState Window { get; set; } = new();
    // MainSplitterDistance is retained for backward compatibility with older session files.
    public int MainSplitterDistance { get; set; } = 730;
    public int MainRightPaneWidthLogical { get; set; } = 380;
    public int SelectedTabIndex { get; set; }
    public List<string> OpenFiles { get; set; } = [];
    public string? SelectedProfilePath { get; set; }
    public string? CompareLeftPath { get; set; }
    public string? CompareRightPath { get; set; }
    public Dictionary<string, GridLayoutState> Grids { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class WindowLayoutState
{
    public int X { get; set; } = 120;
    public int Y { get; set; } = 80;
    public int Width { get; set; } = 1080;
    public int Height { get; set; } = 720;
    public string State { get; set; } = "Normal";
}

public sealed class GridLayoutState
{
    public int RowHeight { get; set; } = 32;
    public int HeaderHeight { get; set; } = 36;
    public List<int> RowHeights { get; set; } = [];
    public List<GridColumnState> Columns { get; set; } = [];
}

public sealed class GridColumnState
{
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int DisplayIndex { get; set; }
    public bool Visible { get; set; } = true;
}
