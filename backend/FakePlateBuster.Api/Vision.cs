using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using FakePlateBuster.Api.Models;
namespace FakePlateBuster.Api.Services;
public interface IVisionService { string Mode { get; } Task<VisionResult> AnalyseAsync(byte[] image); }
public sealed class VisionConfigurationException : Exception { }
public sealed class AwsRekognitionVisionService : IVisionService
{
    public string Mode => "AWS";
    public async Task<VisionResult> AnalyseAsync(byte[] bytes)
    {
        AWSCredentials credentials;
        try { credentials = FallbackCredentialsFactory.GetCredentials(); await credentials.GetCredentialsAsync(); }
        catch { throw new VisionConfigurationException(); }
        using var client = new AmazonRekognitionClient(credentials, RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-southeast-2"));
        using var imageStream = new MemoryStream(bytes);
        var image = new Amazon.Rekognition.Model.Image { Bytes = imageStream };
        var labels = await client.DetectLabelsAsync(new DetectLabelsRequest { Image = image, MaxLabels = 100, MinConfidence = 60 });
        imageStream.Position = 0;
        var text = await client.DetectTextAsync(new DetectTextRequest { Image = image });
        var vehicles = labels.Labels.Where(l => l.Name is "Car" or "Truck" or "Motorcycle" or "Bus").ToArray();
        // Require a single localized subject; never combine attributes across vehicles.
        var ambiguous = vehicles.Any(l => l.Instances.Count > 1) || vehicles.Count(l => l.Instances.Count > 0) > 1;
        var candidates = text.TextDetections.Where(t => t.Type == TextTypes.LINE &&
            Verification.ValidPlate(Verification.NormalizePlate(t.DetectedText)) && t.DetectedText.Any(char.IsDigit)).ToArray();
        ambiguous |= candidates.Length > 1;
        if (ambiguous) return new VisionResult { Ambiguous = true, Warnings = new() { "Multiple possible subjects or plate texts. Photograph one vehicle." } };
        if (!vehicles.Any(l => l.Instances.Count == 1)) return new VisionResult { Ambiguous = true, Warnings = new() { "A single vehicle could not be located reliably." } };
        Reading Find(params string[] names) {
            var hits = labels.Labels.Where(l => names.Contains(l.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
            return hits.Length == 1 ? new Reading(hits[0].Name, (double)hits[0].Confidence / 100) : new Reading();
        }
        // General labels often cannot identify exact models/colour. Return null rather than infer from the registry.
        return new VisionResult {
            Plate = candidates.Length == 1 ? new Reading(candidates[0].DetectedText, (double)candidates[0].Confidence / 100) : new(),
            Make = Find("Suzuki", "Toyota", "Mazda", "Honda", "Nissan", "Ford", "BMW", "Audi", "Hyundai", "Kia"),
            Model = Find("Swift", "Vitara", "Corolla", "RAV4", "Mazda3", "CX-5", "Civic", "CR-V", "Leaf", "X-Trail"),
            Colour = new(), Type = Find("Sedan", "Hatchback", "SUV", "Pickup Truck"),
            Warnings = new() { "AWS confidence values are estimates. General labels cannot reliably identify vehicle colour or every exact model; missing details require review. OCR text should be checked against the photo." }
        };
    }
}
