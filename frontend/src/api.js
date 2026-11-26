const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5088'

export async function verifyPlate(plateNumber, imageBase64, provider = 'Auto') {
  const headers = { 'Content-Type': 'application/json' }
  if (provider && provider !== 'Auto') headers['X-Vision-Provider'] = provider
  const res = await fetch(`${API_BASE_URL}/api/verify`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ plateNumber, image: imageBase64 })
  })
  if (!res.ok) {
    const msg = await res.text()
    throw new Error(`API error (${res.status}): ${msg}`)
  }
  return await res.json()
}

export async function verifyPlateMultipart(plateNumber, file, analysisToken = null) {
  const form = new FormData()
  form.append('plateNumber', plateNumber)
  if (file) form.append('image', file, 'vehicle-image')
  if (analysisToken) form.append('analysisToken', analysisToken)
  const headers = {}
  const res = await fetch(`${API_BASE_URL}/api/verify-multipart`, { method: 'POST', body: form, headers })
  if (!res.ok) {
    const msg = await res.text()
    throw new Error(`API error (${res.status}): ${msg}`)
  }
  return await res.json()
}

export async function getPlate(plateNumber) {
  const res = await fetch(`${API_BASE_URL}/api/plates/${encodeURIComponent(plateNumber)}`)
  if (!res.ok) throw new Error(`Plate not found: ${plateNumber}`)
  return await res.json()
}

export async function getDemoSamples() {
  const response = await fetch(`${API_BASE_URL}/api/demo/samples`)
  if (!response.ok) throw new Error('Sample list unavailable')
  return response.json()
}
export async function getDemoImage(sample) {
  const response = await fetch(`${API_BASE_URL}${sample.imageUrl}`)
  if (!response.ok) throw new Error('Sample photo unavailable')
  return new File([await response.blob()], 'vehicle.png', { type: 'image/png' })
}
