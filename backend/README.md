# FakePlateBuster API

Run from the repository root with `dotnet run --project backend/FakePlateBuster.Api` (.NET 8). Default development port: 5088. Swagger is available at `/swagger` in Development.

Endpoints:
- `GET /api/health`
- `GET /api/plates/{plate}` — independent registry lookup.
- `POST /api/verify` — JSON `{ "plateNumber": "10000001", "image": "<base64 image bytes>" }`; image may be omitted.
- `POST /api/verify-multipart` — `plateNumber` string and optional `image` file (5 MB maximum).
- `GET /api/demo/samples` — 40 sample identifiers, plates, scenarios and image URLs.
- `GET /api/demo/images/{id}` — allowlisted bundled synthetic image only.

The default `Stub` provider is now `MockAiVisionService`: SHA-256 matches uploaded image bytes to `test_cases.json`, then returns intended ground-truth attributes. Text tokens and unrecognised images cannot generate fake detections. Original dataset files are copied during build/publish. Registry records come from the dataset plus separate legacy fixtures.

Configure `VISION_PROVIDER=AWS` or `Vision:Provider=AWS` for real AWS image analysis, with server-side credentials and AWS_REGION. Optional per-request override: `X-Vision-Provider: Mock|Stub|AWS` (or `?provider=...`). AWS receives only image bytes. No expected registry record or dataset labels are passed to it. Errors require review and do not fall back to mocks.

Responses include `status`, `message`, `mismatches` (field names), `reasonDescriptions`, `visionMode` when analysis occurs, and `details.expected` / `details.detected`. API colour is named `color`; dataset `colour` is mapped explicitly. Mock fixtures do not have a measured confidence score.

Run `python3 tests/verify_dataset.py` from the repository root while the API is running. See the root README for rules, limitations and full setup.

## Image-first API update

`plateNumber` is optional on both verification endpoints. `detection` includes plate/make/model/colour/type readings with value and nullable confidence. Results expose detectedPlate, lookupPlate, plateSource and a protected analysisToken. Submit the same image and token with a manual plate correction to reuse independent detections. Tokens expire after 15 minutes or server restart. Providers are now selected only through server configuration; per-request overrides described above are retired.

See the root README for current AWS limitations, setup and image-first tests.
