using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdrenalinProfileViewer.Models;

namespace AdrenalinProfileViewer.Services;

public sealed class ProfileMetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _directory;

    public ProfileMetadataStore()
    {
        _directory = PortablePaths.ProfileMetadataDirectory;
        Directory.CreateDirectory(_directory);
    }

    public ProfileMetadata Load(string profilePath)
    {
        var path = GetMetadataPath(profilePath);
        if (!File.Exists(path))
        {
            return new ProfileMetadata();
        }

        try
        {
            return JsonSerializer.Deserialize<ProfileMetadata>(File.ReadAllText(path), JsonOptions)
                   ?? new ProfileMetadata();
        }
        catch
        {
            return new ProfileMetadata();
        }
    }

    public void Save(string profilePath, ProfileMetadata metadata)
    {
        Directory.CreateDirectory(_directory);
        var path = GetMetadataPath(profilePath);
        File.WriteAllText(path, JsonSerializer.Serialize(metadata, JsonOptions), Encoding.UTF8);
    }

    private string GetMetadataPath(string profilePath)
    {
        var normalized = Path.GetFullPath(profilePath).Trim().ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(_directory, $"{hash}.json");
    }
}
