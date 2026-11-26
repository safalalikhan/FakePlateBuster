using System.Security.Cryptography;
using System.Text.Json;
using FakePlateBuster.Api.Models;

namespace FakePlateBuster.Api.Services;

public sealed class UnsupportedFixtureException : Exception { }

// Labels are used only by this explicitly mocked adapter, never by the real provider.
public sealed class Dataset
{
    public string Root { get; } = Path.Combine(AppContext.BaseDirectory, "dataset");
    public Dictionary<string, VehicleRecord> Registry { get; }
    public IReadOnlyList<Fixture> Fixtures { get; }
    private readonly Dictionary<string, VisionResult> byHash;
    public Dataset()
    {
        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "registry_by_plate.json")));
        Registry = registry.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => {
            var v = p.Value;
            return new VehicleRecord { Make = v.GetProperty("make").GetString(), Model = v.GetProperty("model").GetString(),
                Color = v.GetProperty("colour").GetString(), Type = v.GetProperty("type").GetString(), Flagged = v.GetProperty("flagged").GetBoolean() };
        }, StringComparer.OrdinalIgnoreCase);
        using var cases = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "test_cases.json")));
        Fixtures = cases.RootElement.EnumerateArray().Select(c => {
            var g = c.GetProperty("image_ground_truth");
            return new Fixture(c.GetProperty("id").GetString()!, c.GetProperty("input_plate").GetString()!,
                c.GetProperty("scenario").GetString()!, c.GetProperty("image").GetString()!, c.GetProperty("sha256").GetString()!,
                new VisionResult { Plate = new(c.GetProperty("visible_plate").GetString()), Make = new(g.GetProperty("make").GetString()), Model = new(g.GetProperty("model").GetString()),
                    Colour = new(g.GetProperty("colour").GetString()), Type = new(g.GetProperty("type").GetString()), Warnings = new() { "Deterministic mock fixture labels, not OCR. Confidence is not measured." } });
        }).ToArray();
        byHash = Fixtures.ToDictionary(f => f.Hash, f => f.Attributes, StringComparer.OrdinalIgnoreCase);
    }
    public VisionResult Detect(byte[] bytes) => byHash.TryGetValue(Convert.ToHexString(SHA256.HashData(bytes)), out var v)
        ? v : throw new UnsupportedFixtureException();
}
public record Fixture(string Id, string Plate, string Scenario, string Image, string Hash, VisionResult Attributes);
public sealed class MockAiVisionService(Dataset dataset) : IVisionService
{
    public string Mode => "Mock fixture";
    public Task<VisionResult> AnalyseAsync(byte[] image) => Task.FromResult(dataset.Detect(image));
}

public static class AttributeComparison
{
    public static string Normalize(string? value) {
        var normalized = System.Text.RegularExpressions.Regex.Replace(value?.Trim().ToLowerInvariant() ?? "", @"[\s-]+", "");
        return normalized == "gray" ? "grey" : normalized;
    }
    public static bool Missing(string? value) => string.IsNullOrWhiteSpace(value) || Normalize(value) is "unknown" or "n/a";
}
