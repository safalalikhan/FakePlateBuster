# FakePlateBuster

FakePlateBuster is a vehicle verification prototype for petrol stations, parking teams and security staff. It compares a vehicle photo with the record linked to its number plate.

**Status: Building phase — this is not a production-ready system.**

## How it works

1. Take or upload a photo of one vehicle.
2. The vision service returns the plate, make, model, colour and vehicle type.
3. The app uses the detected plate to look up the mock registry.
4. It compares the detected vehicle details with the registry record.
5. It shows the result, differences and a suggested next step.

If the plate is unreadable or incorrect, you can enter a correction. This changes the registry lookup while keeping the original image detection unchanged.

## Verification logic

Before comparing, the app normalizes case, spacing and variations such as `CR-V` / `CRV` and `Grey` / `Gray`.

| Condition | Result |
| --- | --- |
| All required vehicle details match | Match |
| One confirmed difference | Review Needed |
| Two or more confirmed differences | Suspicious |
| Missing or uncertain details, without two confirmed differences | Review Needed |
| Plate is not in the mock registry | Unknown Plate |
| Registry record is explicitly flagged | Flagged |
| Vision service fails or does not support the photo | Analysis unavailable |

Unknown or low-confidence details are not counted as differences. Confirmed differences are retained even when other details are uncertain. The confidence threshold defaults to 70% and can be configured.

An unreadable plate asks for a clearer photo or manual entry. An ambiguous photo asks for a clearer image of one vehicle. A plate-only check never returns Match.

**Suspicious means the vehicle details differ from the record. It is not proof of a cloned plate or wrongdoing.**

## Current demo

- Uses a fictional registry and 40 synthetic vehicle photos.
- The default mock vision service matches image bytes using SHA-256, then returns the fixture's labelled plate and vehicle details. This is not real image recognition.
- Registry lookup and image detection remain separate. Results are calculated from the comparison, not copied from test labels.
- Choose **Try a sample vehicle**, load a sample, then select **Analyse vehicle**.
- Unrecognised photos are unsupported in mock mode.

All 40 mock cases pass: **10 Match, 20 Review Needed and 10 Suspicious**. This validates the workflow, not real AI accuracy. The synthetic image labels have not been independently validated.

An optional AWS adapter supports OCR and vehicle labels. Exact model detection is limited, and colour detection is not yet implemented. There is no official vehicle registry connection.

## Run locally

Requires .NET 8 and Node.js/npm.

Start the API from the project root:

```sh
dotnet run --project backend/FakePlateBuster.Api
```

In a second terminal, start the frontend:

```sh
cd frontend
npm install
npm run dev
```

Open the frontend URL printed in the terminal. The API normally runs at `http://localhost:5088`.

To use AWS, configure server-side AWS credentials, set `VISION_PROVIDER=AWS` and `AWS_REGION`, then restart the API. Credentials must not be placed in the frontend. AWS mode sends image content to AWS and never silently falls back to mock results.

## Tests

With the API running:

```sh
python3 tests/verify_dataset.py
dotnet run --project tests/ComparisonChecks
```

## Still being built

Real vehicle recognition, real-world accuracy testing, phone-browser testing and production security need further work. Uploaded photos are processed without being saved by the app; bundled demo images remain in the project.

Built with **React + ASP.NET Core**. See [frontend setup](frontend/README.md) and [API details](backend/README.md) for additional configuration.
