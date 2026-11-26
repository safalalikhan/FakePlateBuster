using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using FakePlateBuster.Api.Services;
using FakePlateBuster.Api.Models;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 6 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 8 * 1024 * 1024);
builder.Services.AddSingleton<Dataset>();
builder.Services.AddSingleton<MockAiVisionService>();
builder.Services.AddSingleton<AwsRekognitionVisionService>();
builder.Services.AddSingleton<IVisionService>(sp => {
    var provider = Environment.GetEnvironmentVariable("VISION_PROVIDER") ?? builder.Configuration["Vision:Provider"] ?? "Mock";
    return provider.ToUpperInvariant() switch {
        "MOCK" or "STUB" => sp.GetRequiredService<MockAiVisionService>(),
        "AWS" or "REKOGNITION" => sp.GetRequiredService<AwsRekognitionVisionService>(),
        _ => throw new InvalidOperationException("Unsupported configured vision provider.")
    };
});
var app = builder.Build();
var protector = new EphemeralDataProtectionProvider().CreateProtector("vehicle-analysis");
app.UseCors();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
Dictionary<string, VehicleRecord> Registry() {
    var path = Path.Combine(app.Environment.ContentRootPath, "data", "registry.json");
    var records = File.Exists(path) ? JsonSerializer.Deserialize<Dictionary<string, VehicleRecord>>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })! : new();
    foreach (var pair in app.Services.GetRequiredService<Dataset>().Registry) records[pair.Key] = pair.Value;
    return records;
}
async Task<IResult> Verify(string? manual, byte[]? image, IVisionService vision, string? analysisToken = null) {
    if (!string.IsNullOrWhiteSpace(manual) && !Verification.ValidPlate(Verification.NormalizePlate(manual))) return Results.BadRequest(new { message = "Enter up to 12 plate letters or numbers." });
    VisionResult? detection = null;
    if (image is not null) {
        if (image.Length == 0 || image.Length > 5 * 1024 * 1024) return Results.BadRequest(new { message = "Choose an image up to 5 MB." });
        try {
            if (!string.IsNullOrWhiteSpace(analysisToken)) {
                var cached = JsonSerializer.Deserialize<CachedAnalysis>(protector.Unprotect(analysisToken))!;
                if (cached.Expires < DateTimeOffset.UtcNow || cached.Mode != vision.Mode || cached.Hash != Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image)))
                    return Results.BadRequest(new { message = "Previous analysis expired or does not match this photo. Analyse the photo again." });
                detection = cached.Detection;
            } else detection = await vision.AnalyseAsync(image);
        }
        catch (UnsupportedFixtureException) { return Results.Ok(new VerificationResult { Status = "Analysis unavailable", VisionMode = vision.Mode, Message = "This photo is not a recognised demo fixture. Mock vision only supports the bundled sample photos." }); }
        catch (VisionConfigurationException) { return Results.Ok(new VerificationResult { Status = "Analysis unavailable", VisionMode = vision.Mode, Message = "Real vision is not configured. Set server-side AWS credentials and region." }); }
        catch (Exception) { return Results.Ok(new VerificationResult { Status = "Analysis unavailable", VisionMode = vision.Mode, Message = "The vision provider could not analyse this image. Try again or take a clearer photo." }); }
    }
    var threshold = app.Configuration.GetValue<double?>("Vision:ConfidenceThreshold") ?? 0.7;
    var result = Verification.Compare(detection, manual, Registry(), vision.Mode, Math.Clamp(threshold, 0, 1));
    if (detection is not null && image is not null) result.AnalysisToken = protector.Protect(JsonSerializer.Serialize(new CachedAnalysis(detection, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image)), vision.Mode, DateTimeOffset.UtcNow.AddMinutes(15))));
    return Results.Ok(result);
}
app.MapGet("/api/health", (IVisionService vision) => Results.Ok(new { status = "ok", visionMode = vision.Mode }));
app.MapGet("/api/plates/{plate}", (string plate) => Registry().TryGetValue(Verification.NormalizePlate(plate), out var record) ? Results.Ok(record) : Results.NotFound());
app.MapPost("/api/verify", async (VerifyRequest request, IVisionService vision) => {
    byte[]? bytes;
    try { bytes = string.IsNullOrEmpty(request.Image) ? null : Convert.FromBase64String(request.Image); }
    catch (FormatException) { return Results.BadRequest(new { message = "Image must be base64 image content." }); }
    return await Verify(request.PlateNumber, bytes, vision, request.AnalysisToken);
});
app.MapPost("/api/verify-multipart", async (HttpRequest request, IVisionService vision) => {
    if (!request.HasFormContentType) return Results.BadRequest(new { message = "Use multipart form data." });
    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("image");
    byte[]? bytes = null;
    if (file is not null) {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024 || !new[] { "image/jpeg", "image/png", "image/webp", "image/gif" }.Contains(file.ContentType)) return Results.BadRequest(new { message = "Choose a JPG, PNG, WebP or GIF image up to 5 MB." });
        using var buffer = new MemoryStream(); await file.CopyToAsync(buffer); bytes = buffer.ToArray();
    }
    return await Verify(form["plateNumber"].ToString(), bytes, vision, form["analysisToken"].ToString());
});
app.MapGet("/api/demo/samples", (Dataset dataset) => Results.Ok(dataset.Fixtures.Select(f => new { f.Id, f.Plate, f.Scenario, imageUrl = $"/api/demo/images/{f.Id}" })));
app.MapGet("/api/demo/images/{id}", (string id, Dataset dataset) => {
    var fixture = dataset.Fixtures.FirstOrDefault(f => f.Id == id);
    return fixture is null ? Results.NotFound() : Results.File(Path.Combine(dataset.Root, fixture.Image), "image/png");
});
app.Run();
