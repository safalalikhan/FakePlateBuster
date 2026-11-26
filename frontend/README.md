# FakePlateBuster frontend

Run `npm install` and `npm run dev`. Start the .NET API on port 5088 first.
Use `npm run build` for a production build.

The responsive dashboard supports plate checks, camera capture (on supported phones), gallery uploads, image previews and clear review guidance. The camera button uses the device's native file picker; desktop browsers may show a file chooser instead.

For a phone on the same Wi-Fi, run `npm run dev -- --host 0.0.0.0`, configure `VITE_API_BASE_URL` to the computer's LAN address on port 5088, and bind the API to that address. Restart Vite after changing its environment. Use only a trusted development network.

Photos are sent to the API in memory and are not persisted by this app. The default mock vision service recognises the 40 bundled original images by SHA-256 and returns their intended fixture labels; other images are unsupported. Use **Try a sample vehicle** to load a plate and photo scenario. This tests the workflow, not image-recognition accuracy. AWS mode sends images to AWS. There is no official registry connection. Do not use demo results as enforcement evidence.
