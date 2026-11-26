# FakePlateBuster synthetic test dataset

10 fictional registry vehicles, two baseline models each from Suzuki, Toyota, Mazda, Honda and Nissan. Plates 10000001–10000010 are synthetic eight-digit numeric identifiers, not real NZ registry lookups. Plate values are strings.

## Contents
- registry.json: array of ten registry records.
- registry_by_plate.json: same records keyed by plate for backend lookup.
- registry.csv: importable registry table.
- test_cases.json / test_cases.csv: forty image cases and expected results.
- images/correct: ten correct vehicle images.
- images/wrong_model: ten images with another model, same make, colour and broad type.
- images/wrong_colour: ten images with original model and different colour.
- images/wrong_model_and_colour: ten images with another model and colour.
- gallery.html: open after extracting the ZIP to browse all images.

## Use with the app
1. Load registry_by_plate.json into the mock registry service. Preserve plates as strings and permit eight digits in demo mode.
2. Enter input_plate from a test case and upload its image.
3. Compare detected make/model/type/colour with the registry record. Normalize spelling, punctuation and case, including Mazda3/Mazda 3, CR-V/CRV, HR-V/HRV, RAV4/RAV 4 and grey/gray.
4. Zero mismatches => Match; one => Review Needed; two or more => Suspicious. Missing/low-confidence attributes require Review Needed instead of forced mismatches.
5. All records are unflagged. Flagged and Unknown Plate cases are outside these forty image tests. For Unknown Plate use 99999999; for a flag test temporarily override a record's flagged value in a separate fixture.

## Ground truth and limitations
The labels describe the intended image generation specification. Images are AI-generated, not actual vehicle evidence. They have not been independently validated by an OCR or vehicle-recognition model. Model generations, badges and details can vary; year and trim are deliberately excluded from comparison. Review ambiguous images before using them to measure recognition accuracy. This small, uniform dataset is appropriate for prototype workflow tests, not a real-world accuracy benchmark.

Do not pass test case labels or scenario filenames as evidence to the real vision model: send only image bytes under a neutral filename. Using filenames/labels for MockAiVisionService is acceptable only as an explicitly mocked pipeline demonstration, not proof of image understanding. Status expectations assume sufficient detection confidence. No real government or CarJam data is included.
