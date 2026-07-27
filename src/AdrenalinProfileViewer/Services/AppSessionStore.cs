using System.Text;
using System.Text.Json;
using AdrenalinProfileViewer.Models;

namespace AdrenalinProfileViewer.Services;

public sealed class AppSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSessionState Load()
    {
        if (!File.Exists(PortablePaths.SessionFilePath))
        {
            return new AppSessionState();
        }

        try
        {
            var state = JsonSerializer.Deserialize<AppSessionState>(
                            File.ReadAllText(PortablePaths.SessionFilePath, Encoding.UTF8),
                            JsonOptions)
                        ?? new AppSessionState();
            state.OpenFiles ??= [];
            state.Grids ??= new Dictionary<string, GridLayoutState>(StringComparer.OrdinalIgnoreCase);
            foreach (var grid in state.Grids.Values)
            {
                grid.Columns ??= [];
                grid.RowHeights ??= [];
            }
            state.Window ??= new WindowLayoutState();
            return state;
        }
        catch (Exception ex)
        {
            TryLogSessionError("load", ex);
            return new AppSessionState();
        }
    }

    public void Save(AppSessionState state)
    {
        Directory.CreateDirectory(PortablePaths.SettingsDirectory);
        var temporaryPath = PortablePaths.SessionFilePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions), Encoding.UTF8);
            File.Move(temporaryPath, PortablePaths.SessionFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            TryLogSessionError("save", ex);
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Best effort cleanup inside the portable data directory.
            }
        }
    }

    private static void TryLogSessionError(string operation, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(PortablePaths.LogsDirectory);
            File.AppendAllText(
                PortablePaths.SessionLogPath,
                $"[{DateTimeOffset.Now:O}] Session {operation} failed.{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // Never mask the original session error.
        }
    }
}
