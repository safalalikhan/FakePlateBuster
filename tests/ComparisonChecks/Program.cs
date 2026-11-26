using FakePlateBuster.Api.Models;
using FakePlateBuster.Api.Services;
var registry = new Dictionary<string, VehicleRecord> { ["10000001"] = new() { Make="Honda", Model="CR-V", Color="Grey", Type="SUV" }, ["10000002"] = new() { Make="Honda", Model="Civic", Color="White", Type="sedan" } };
var vision = new VisionResult { Plate=new("10000001", .99), Make=new("Honda", .99), Model=new("CRV", .99), Colour=new("Gray", .99), Type=new("SUV", .99) };
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
VerificationResult Run(VisionResult v, string? manual=null) => Verification.Compare(v, manual, registry, "test", .7);
Check(Run(vision).Status == "Match", "aliases match");
foreach (var pair in new[] {("Mazda3","Mazda 3"),("HR-V","HRV"),("RAV4","RAV 4")}) Check(AttributeComparison.Normalize(pair.Item1)==AttributeComparison.Normalize(pair.Item2),pair.Item1);
Check(Run(vision with { Plate=new() }).Status == "Review Needed", "unreadable plate");
Check(Run(vision with { Plate=new("10000001", .2) }).Status == "Review Needed", "uncertain plate");
Check(Run(vision with { Plate=new("99999999", .99) }).Status == "Unknown Plate", "unknown plate");
Check(Run(vision with { Ambiguous=true },"10000001").Status=="Review Needed", "manual cannot resolve ambiguity");
Check(Run(vision with { Model=new("Civic",.2) }).Mismatches.Count==0, "uncertain not mismatch");
var mixed=Run(vision with { Model=new("Civic",.99), Colour=new("Red",.99), Make=new() });
Check(mixed.Status=="Suspicious" && mixed.Mismatches.Count==2,"confirmed mismatches survive missing attributes");
var corrected=Run(vision,"10000002");
Check(corrected.DetectedPlate=="10000001" && corrected.LookupPlate=="10000002" && corrected.PlateSource=="manual" && corrected.Detection==vision && corrected.Status=="Suspicious","manual correction preserves detection");
