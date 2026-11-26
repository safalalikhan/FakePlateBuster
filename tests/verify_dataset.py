"""Run against a local API: python3 tests/verify_dataset.py [http://localhost:5088]."""
import collections
import hashlib
import json
from pathlib import Path
import sys
import urllib.request
import urllib.error

root = Path(__file__).resolve().parents[1] / 'FakePlateBuster_Test_Dataset'
base = sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5088'
cases = json.loads((root / 'test_cases.json').read_text())
registry = json.loads((root / 'registry_by_plate.json').read_text())
def get(path):
    return json.load(urllib.request.urlopen(base + path, timeout=15))
def verify(plate, image=None, mime='image/png', token=None):
    boundary = 'FixtureTestBoundary'
    body = f'--{boundary}\r\nContent-Disposition: form-data; name="plateNumber"\r\n\r\n{plate}\r\n'.encode()
    if token:
        body += f'--{boundary}\r\nContent-Disposition: form-data; name="analysisToken"\r\n\r\n{token}\r\n'.encode()
    if image is not None:
        body += f'--{boundary}\r\nContent-Disposition: form-data; name="image"; filename="neutral.png"\r\nContent-Type: {mime}\r\n\r\n'.encode() + image + b'\r\n'
    body += f'--{boundary}--\r\n'.encode()
    req = urllib.request.Request(base + '/api/verify-multipart', data=body,
        headers={'Content-Type': f'multipart/form-data; boundary={boundary}', 'X-Vision-Provider': 'Mock'})
    return json.load(urllib.request.urlopen(req, timeout=15))
counts = collections.Counter()
for case in cases:
    image = (root / case['image']).read_bytes()
    assert hashlib.sha256(image).hexdigest() == case['sha256'], case['id']
    result = verify('', image)
    assert result['status'] == case['expected_status'], (case['id'], result)
    assert sorted(result['mismatches']) == sorted(case['expected_mismatches']), (case['id'], result)
    assert result['details']['expected']['model'] == registry[case['input_plate']]['model']
    assert result['visionMode'] == 'Mock fixture'
    assert result['detectedPlate'] == case['visible_plate'] and result['lookupPlate'] == case['input_plate']
    assert result['plateSource'] == 'image'
    counts[result['status']] += 1
assert counts == {'Match': 10, 'Review Needed': 20, 'Suspicious': 10}, counts
for plate, expected in registry.items():
    assert isinstance(plate, str)
    result = verify(plate)
    assert result['status'] == 'Review Needed' and result['details']['detected'] is None
    assert get('/api/plates/' + plate)['model'] == expected['model']
assert verify('99999999')['status'] == 'Unknown Plate'
assert verify('XYZ987')['status'] == 'Flagged'  # separate legacy fixture
assert verify('10000001', b'unrecognised photo')['status'] == 'Analysis unavailable'
# A different entered plate must never select the expected record from the image label.
result = verify('10000002', (root / cases[0]['image']).read_bytes())
assert result['details']['expected']['model'] == registry['10000002']['model']
assert result['details']['detected']['model'] == cases[0]['image_ground_truth']['model']
for content, mime in [(b'x', 'text/plain'), (b'', 'image/png'), (b'x' * (5 * 1024 * 1024 + 1), 'image/png')]:
    try:
        verify('10000001', content, mime)
        raise AssertionError('Invalid upload accepted')
    except urllib.error.HTTPError as error:
        assert error.code in (400, 413), error.code
samples = get('/api/demo/samples')
assert len(samples) == 40
for sample in samples:
    with urllib.request.urlopen(base + sample['imageUrl'], timeout=15) as response:
        assert hashlib.sha256(response.read()).hexdigest() == next(c['sha256'] for c in cases if c['id'] == sample['id'])
print('PASS: 40 fixtures, exact mismatch fields and hashes:', dict(counts))
print('PASS: registry, plate-only, unknown, flagged, unsupported, independent plate lookup, upload validation and 40 sample URLs')

photo = (root / cases[0]['image']).read_bytes()
original = verify('', photo)
corrected = verify('10000002', photo, token=original['analysisToken'])
assert corrected['detectedPlate'] == original['detectedPlate']
assert corrected['detection'] == original['detection']
assert corrected['lookupPlate'] == '10000002' and corrected['plateSource'] == 'manual'
assert corrected['details']['expected']['model'] == registry['10000002']['model']
print('PASS: protected correction reuses original detection with a fresh registry lookup')
