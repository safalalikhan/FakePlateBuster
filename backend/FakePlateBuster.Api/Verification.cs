using FakePlateBuster.Api.Services;
namespace FakePlateBuster.Api.Models;
public record Reading(string? Value = null, double? Confidence = null);
public record VisionResult
{
    public Reading Plate { get; init; } = new();
    public Reading Make { get; init; } = new();
    public Reading Model { get; init; } = new();
    public Reading Colour { get; init; } = new();
    public Reading Type { get; init; } = new();
    public bool Ambiguous { get; init; }
    public List<string> Warnings { get; init; } = new();
}
public record VehicleSnapshot
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public string? Color { get; init; }
    public string? Type { get; init; }
}
public record VehicleRecord : VehicleSnapshot
{
    public bool Flagged { get; init; }
    public string? FlagReason { get; init; }
}
public record CachedAnalysis(VisionResult Detection, string Hash, string Mode, DateTimeOffset Expires);
public record VerifyRequest(string? PlateNumber, string? Image, string? AnalysisToken = null);
public record VerificationResult
{
    public string? AnalysisToken { get; set; }
    public string Status { get; set; } = "Review Needed";
    public string Message { get; set; } = "Add a clear photo or enter a plate manually.";
    public string? DetectedPlate { get; init; }
    public string? LookupPlate { get; set; }
    public string PlateSource { get; init; } = "image";
    public string VisionMode { get; init; } = "Mock fixture";
    public VisionResult? Detection { get; init; }
    public object? Details { get; set; }
    public List<string> Mismatches { get; } = new();
    public List<string> ReasonDescriptions { get; } = new();
}
public static class Verification
{
    public static string NormalizePlate(string? plate) => System.Text.RegularExpressions.Regex.Replace(plate?.Trim().ToUpperInvariant() ?? "", @"[\s-]+", "");
    public static bool ValidPlate(string plate) => System.Text.RegularExpressions.Regex.IsMatch(plate, "^[A-Z0-9]{1,12}$");
    public static VerificationResult Compare(VisionResult? vision, string? manual, IReadOnlyDictionary<string, VehicleRecord> registry, string mode, double threshold)
    {
        var corrected = !string.IsNullOrWhiteSpace(manual);
        var lookup = NormalizePlate(corrected ? manual : vision?.Plate.Value);
        var result = new VerificationResult { DetectedPlate = vision?.Plate.Value, LookupPlate = lookup.Length > 0 ? lookup : null,
            PlateSource = corrected ? "manual" : "image", Detection = vision, VisionMode = mode };
        var detected = vision is null ? null : new VehicleSnapshot { Make = vision.Make.Value, Model = vision.Model.Value, Color = vision.Colour.Value, Type = vision.Type.Value };
        result.Details = new { expected = (VehicleRecord?)null, detected };
        if (vision?.Ambiguous == true) { result.Message = "More than one vehicle or plate may be present. Take a clear photo of one vehicle; a plate correction cannot resolve the vehicle ambiguity."; return result; }
        if (!ValidPlate(lookup) || (!corrected && (AttributeComparison.Missing(vision?.Plate.Value) || vision?.Plate.Confidence < threshold))) {
            result.Message = "The plate could not be read reliably. Take a clearer photo or enter the plate manually."; return result;
        }
        if (!registry.TryGetValue(lookup, out var record)) { result.Status = "Unknown Plate"; result.Message = "This plate was not found in the mock registry."; return result; }
        result.Details = new { expected = record, detected };
        if (record.Flagged) { result.Status = "Flagged"; result.Message = record.FlagReason ?? "This separate demo record is flagged for staff review."; return result; }
        if (vision is null) { result.Message = "Registry record found. Add a photo to compare the vehicle."; return result; }
        var incomplete = false;
        void Check(string key, string? expected, Reading observed) {
            if (AttributeComparison.Missing(expected) || AttributeComparison.Missing(observed.Value) || observed.Confidence < threshold) { incomplete = true; return; }
            if (AttributeComparison.Normalize(expected) != AttributeComparison.Normalize(observed.Value)) {
                result.Mismatches.Add(key); result.ReasonDescriptions.Add($"{key}: expected {expected}; detected {observed.Value}.");
            }
        }
        Check("make", record.Make, vision.Make); Check("model", record.Model, vision.Model);
        Check("colour", record.Color, vision.Colour); Check("type", record.Type, vision.Type);
        // Confirmed differences survive uncertainty in other attributes.
        result.Status = result.Mismatches.Count >= 2 ? "Suspicious" : result.Mismatches.Count == 1 || incomplete ? "Review Needed" : "Match";
        result.Message = result.Status == "Match" ? "The detected vehicle attributes match the mock registry record."
            : result.Mismatches.Count > 0 ? $"The vehicle's {string.Join(" and ", result.Mismatches)} differ from the mock registry record. Staff review recommended."
            : "Some vehicle details could not be identified reliably. Please review or take a clearer photo.";
        if (incomplete) result.ReasonDescriptions.Add("Some attributes are missing or uncertain and were not counted as mismatches.");
        return result;
    }
}
