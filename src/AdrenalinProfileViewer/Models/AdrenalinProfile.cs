namespace AdrenalinProfileViewer.Models;

public sealed class AdrenalinProfile
{
    // Observed RDNA 4 Adrenalin export conversion: XML 2728 -> applied/displayed 2714 MHz,
    // and XML 2714 -> applied/displayed 2700 MHz. This is not the GDDR6 data-rate multiplier.
    public const int MemoryClockXmlOffsetMHz = 14;

    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public string? DeviceId { get; init; }
    public string? RevisionId { get; init; }
    public int? Ppw { get; init; }
    public int? PowerLimitPercent { get; init; }
    public int? CoreClockOffsetMHz { get; init; }
    public int? VoltageOffsetMv { get; init; }
    public int? MemoryClockMHz { get; init; }
    public int? CalculatedEffectiveMemoryClockMHz => MemoryClockMHz is null
        ? null
        : MemoryClockMHz.Value - MemoryClockXmlOffsetMHz;
    public string MemoryTimings { get; init; } = "Unknown";
    public string FanMode { get; init; } = "Automatic / not decoded";
    public bool? ZeroRpm { get; init; }
    public DateTime LoadedAt { get; init; }
    public required string RawXml { get; init; }
    public IReadOnlyList<RawFeature> RawFeatures { get; init; } = [];
    public string Notes { get; set; } = string.Empty;

    public string GpuLabel => string.IsNullOrWhiteSpace(RevisionId)
        ? $"Device {DeviceId ?? "unknown"}"
        : $"Device {DeviceId ?? "unknown"}, rev. {RevisionId}";

    public override string ToString() => string.IsNullOrWhiteSpace(DisplayName)
        ? Path.GetFileNameWithoutExtension(FileName)
        : DisplayName;
}

public sealed class RawFeature
{
    public required string Scope { get; init; }
    public required string FeatureId { get; init; }
    public required string FeatureEnabled { get; init; }
    public required string StateId { get; init; }
    public required string StateEnabled { get; init; }
    public required string Value { get; init; }
}

public sealed class ProfileMetadata
{
    public string DisplayName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
