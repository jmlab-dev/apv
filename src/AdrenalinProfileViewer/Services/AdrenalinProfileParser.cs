using System.Globalization;
using System.Xml.Linq;
using AdrenalinProfileViewer.Models;

namespace AdrenalinProfileViewer.Services;

public sealed class AdrenalinProfileParser
{
    public AdrenalinProfile Parse(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);
        var rawXml = File.ReadAllText(fullPath);
        var document = XDocument.Parse(rawXml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var root = document.Root ?? throw new InvalidDataException("The XML document has no root element.");
        var gpu = root.Element("GPU") ?? throw new InvalidDataException("No GPU element was found in the profile.");

        var rawFeatures = ReadRawFeatures(root).ToList();

        return new AdrenalinProfile
        {
            FilePath = fullPath,
            FileName = Path.GetFileName(fullPath),
            DisplayName = Path.GetFileNameWithoutExtension(fullPath),
            DeviceId = Attribute(gpu, "DevID"),
            RevisionId = Attribute(gpu, "RevID"),
            Ppw = ParseInt(Attribute(gpu.Element("PPW"), "Value")),
            PowerLimitPercent = GetStateValue(gpu, featureId: "3", stateId: "0"),
            CoreClockOffsetMHz = GetStateValue(gpu, featureId: "26", stateId: "4"),
            VoltageOffsetMv = GetStateValue(gpu, featureId: "12", stateId: "0"),
            MemoryClockMHz = GetStateValue(gpu, featureId: "5", stateId: "0"),
            MemoryTimings = DecodeMemoryTimings(gpu),
            FanMode = DecodeFanMode(gpu),
            ZeroRpm = DecodeZeroRpm(gpu),
            LoadedAt = DateTime.Now,
            RawXml = rawXml,
            RawFeatures = rawFeatures
        };
    }

    private static IEnumerable<RawFeature> ReadRawFeatures(XElement root)
    {
        foreach (var scope in new[] { "CPU", "GPU" })
        {
            var scopeElement = root.Element(scope);
            if (scopeElement is null)
            {
                continue;
            }

            foreach (var feature in scopeElement.Elements("FEATURE"))
            {
                var states = feature.Element("STATES")?.Elements("STATE").ToList() ?? [];
                if (states.Count == 0)
                {
                    yield return new RawFeature
                    {
                        Scope = scope,
                        FeatureId = Attribute(feature, "ID") ?? string.Empty,
                        FeatureEnabled = Attribute(feature, "Enabled") ?? string.Empty,
                        StateId = string.Empty,
                        StateEnabled = string.Empty,
                        Value = string.Empty
                    };
                    continue;
                }

                foreach (var state in states)
                {
                    yield return new RawFeature
                    {
                        Scope = scope,
                        FeatureId = Attribute(feature, "ID") ?? string.Empty,
                        FeatureEnabled = Attribute(feature, "Enabled") ?? string.Empty,
                        StateId = Attribute(state, "ID") ?? string.Empty,
                        StateEnabled = Attribute(state, "Enabled") ?? string.Empty,
                        Value = Attribute(state, "Value") ?? string.Empty
                    };
                }
            }
        }
    }

    private static int? GetStateValue(XElement gpu, string featureId, string stateId)
    {
        var state = GetState(gpu, featureId, stateId);
        return ParseInt(Attribute(state, "Value"));
    }

    private static XElement? GetState(XElement gpu, string featureId, string stateId)
    {
        var feature = gpu.Elements("FEATURE")
            .FirstOrDefault(x => string.Equals(Attribute(x, "ID"), featureId, StringComparison.OrdinalIgnoreCase));

        return feature?.Element("STATES")?.Elements("STATE")
            .FirstOrDefault(x => string.Equals(Attribute(x, "ID"), stateId, StringComparison.OrdinalIgnoreCase));
    }

    private static string DecodeMemoryTimings(XElement gpu)
    {
        // In the supplied RDNA 4 export, FEATURE 17 / STATE 0 / Value 1 represents Fast Timings.
        // The state flag is used because the parent feature's Enabled attribute is not consistent across exports.
        var state = GetState(gpu, featureId: "17", stateId: "0");
        var value = ParseInt(Attribute(state, "Value"));
        var stateEnabled = ParseBool(Attribute(state, "Enabled"));

        if (stateEnabled == true && value == 1)
        {
            return "Fast";
        }

        if (value == 0)
        {
            return "Default";
        }

        return value is null ? "Unknown" : $"Mode {value}";
    }

    private static string DecodeFanMode(XElement gpu)
    {
        var fanCurveFeature = gpu.Elements("FEATURE")
            .FirstOrDefault(x => string.Equals(Attribute(x, "ID"), "22", StringComparison.OrdinalIgnoreCase));

        return ParseBool(Attribute(fanCurveFeature, "Enabled")) == true
            ? "Custom curve"
            : "Automatic / default";
    }

    private static bool? DecodeZeroRpm(XElement gpu)
    {
        // FEATURE 18 is retained as a best-effort decode and is also visible in Raw Features.
        var state = GetState(gpu, featureId: "18", stateId: "0");
        if (ParseBool(Attribute(state, "Enabled")) != true)
        {
            return null;
        }

        return ParseInt(Attribute(state, "Value")) switch
        {
            0 => false,
            1 => true,
            _ => null
        };
    }

    private static string? Attribute(XElement? element, string name) => element?.Attribute(name)?.Value;

    private static int? ParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };
    }
}
