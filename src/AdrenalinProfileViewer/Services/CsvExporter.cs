using System.Text;
using AdrenalinProfileViewer.Models;

namespace AdrenalinProfileViewer.Services;

public static class CsvExporter
{
    public static void ExportProfiles(string filePath, IEnumerable<AdrenalinProfile> profiles)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Profile,File,GPU Device,Revision,Power Limit %,Core Offset MHz,Voltage Offset mV,Memory Clock Stored XML MHz,Calculated Effective Memory MHz,Memory Timings,Fan Mode,Zero RPM,Notes");

        foreach (var profile in profiles)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Escape(profile.ToString()),
                Escape(profile.FileName),
                Escape(profile.DeviceId),
                Escape(profile.RevisionId),
                Escape(profile.PowerLimitPercent?.ToString()),
                Escape(profile.CoreClockOffsetMHz?.ToString()),
                Escape(profile.VoltageOffsetMv?.ToString()),
                Escape(profile.MemoryClockMHz?.ToString()),
                Escape(profile.CalculatedEffectiveMemoryClockMHz?.ToString()),
                Escape(profile.MemoryTimings),
                Escape(profile.FanMode),
                Escape(FormatNullableBool(profile.ZeroRpm)),
                Escape(profile.Notes)
            }));
        }

        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string FormatNullableBool(bool? value) => value switch
    {
        true => "On",
        false => "Off",
        null => "Unknown"
    };

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
