import React, { useEffect, useRef, useState } from 'react'
import { verifyPlateMultipart, getDemoSamples, getDemoImage } from './api.js'
import './styles.css'

const scenarios = { correct: 'Matching vehicle', wrong_model: 'Different model', wrong_colour: 'Different colour', wrong_model_and_colour: 'Different model and colour' }
const fields = [['make', 'Make'], ['model', 'Model'], ['type', 'Vehicle type'], ['color', 'Colour']]
export default function App() {
  const [samples, setSamples] = useState([])
  const [sampleId, setSampleId] = useState('')
  const [sampleError, setSampleError] = useState('')
  useEffect(() => { getDemoSamples().then(setSamples).catch(() => setSampleError('Demo samples are unavailable. Check that the API is running.')) }, [])
  async function loadSample() {
    const sample = samples.find(s => s.id === sampleId)
    if (!sample) return
    setLoading(true); setError(''); setResult(null)
    try { const photo = await getDemoImage(sample); setPlate(''); setFile(photo) }
    catch { setError('Could not load the sample photo. Please try again.') }
    finally { setLoading(false) }
  }
  const [plate, setPlate] = useState('')
  const [file, setFile] = useState(null)
  const [preview, setPreview] = useState('')
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState(null)
  const [error, setError] = useState('')
  const [drag, setDrag] = useState(false)
  const resultRef = useRef(null)
  const uploadRef = useRef(null)
  const cameraRef = useRef(null)
  const plateRef = useRef(null)
  useEffect(() => {
    if (!file) { setPreview(''); return }
    const url = URL.createObjectURL(file); setPreview(url)
    return () => URL.revokeObjectURL(url)
  }, [file])
  function chooseFile(next) {
    if (loading || !next) return
    setResult(null)
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(next.type)) { setError('Choose a JPG, PNG, WebP or GIF photo.'); return }
    if (next.size > 5 * 1024 * 1024 || !next.size) { setError('Choose a photo smaller than 5 MB that is not empty.'); return }
    setFile(next); setPlate(''); setError('')
  }
  async function submit(e) {
    e.preventDefault()
    const normalized = plate.trim().toUpperCase().replace(/\s+/g, '')
    if (normalized && !/^[A-Z0-9-]{1,12}$/.test(normalized)) { setError('Enter a plate using up to 12 letters, numbers or hyphens.'); plateRef.current?.focus(); return }
    if (!file && !normalized) { setError('Take or choose a vehicle photo first.'); return }
    setPlate(normalized); setLoading(true); setError(''); setResult(null)
    try { setResult({ ...await verifyPlateMultipart(normalized, file, result?.analysisToken) }); requestAnimationFrame(() => resultRef.current?.focus()) }
    catch { setError('We couldn’t complete this check. Check your connection and try again. If it continues, ask your supervisor to check the service.') }
    finally { setLoading(false) }
  }
  function reset() { setPlate(''); setFile(null); setResult(null); setError(''); plateRef.current?.focus() }
  const status = result?.status
  const tone = status === 'Match' ? 'success' : ['Flagged', 'Suspicious'].includes(status) ? 'danger' : status === 'Unknown Plate' ? 'neutral' : 'warning'
  return <>
    <header className="topbar"><div className="brand"><span className="brand-icon" aria-hidden="true">✓</span><span>FakePlate<span className="brand-light">Buster</span></span></div><span className="demo-badge">Demo workspace</span></header>
    <main>
      <div className="page-heading"><div className="eyebrow">VEHICLE VERIFICATION</div><h1>A quick check. A clearer picture.</h1><p>Start with a vehicle photo. Review the plate and vehicle details together.</p></div>
      <div className="workspace">
        <section className="card form-card" aria-labelledby="check-heading"><div className="card-heading"><h2 id="check-heading">Check a vehicle</h2><p>Take or choose a photo. We’ll read the plate and compare the vehicle.</p></div>
          <form onSubmit={submit}>
            <fieldset disabled={loading}>
              <div className="photo-label">Vehicle photo <span>Start here</span></div>
              <div className={`upload ${drag ? 'drag' : ''}`} onDragOver={e => { e.preventDefault(); if (!loading) setDrag(true) }} onDragLeave={() => setDrag(false)} onDrop={e => { e.preventDefault(); setDrag(false); chooseFile(e.dataTransfer.files[0]) }}>
                {preview ? <><img className="preview" src={preview} alt="Vehicle selected for verification" /><div className="file-name">{file.name}</div><button type="button" className="text-button" onClick={() => { setFile(null); setResult(null) }}>Remove photo</button></> : <><div className="photo-icon" aria-hidden="true">▧</div><strong>Show the whole vehicle</strong><p>A clear photo helps compare its make and colour.</p></>}
                <div className="photo-actions"><button type="button" className="secondary" onClick={() => cameraRef.current.click()}>Take photo</button><button type="button" className="secondary" onClick={() => uploadRef.current.click()}>{file ? 'Change photo' : 'Choose photo'}</button></div>
                <input ref={uploadRef} type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden onChange={e => { chooseFile(e.target.files[0]); e.target.value = '' }} />
                <input ref={cameraRef} type="file" accept="image/*" capture="environment" hidden onChange={e => { chooseFile(e.target.files[0]); e.target.value = '' }} />
              </div>
              <p className="helper">JPG, PNG, WebP or GIF · Up to 5 MB</p>
              <details className="manual-plate"><summary>Enter or correct a plate (optional)</summary><label htmlFor="plate">Manual plate correction</label><input ref={plateRef} id="plate" className="plate-input" placeholder="e.g. 10000001" value={plate} maxLength={12} autoCapitalize="characters" autoComplete="off" onChange={e => setPlate(e.target.value.toUpperCase())} /><p className="helper">Leave empty to use the plate read from the photo. A correction changes the lookup only.</p></details>
              {error && <div role="alert" className="error">{error}</div>}
              <button className="primary verify" type="submit">{loading ? <><span className="spinner" /> Analysing vehicle…</> : 'Analyse vehicle'}</button>
              <p className="privacy">Photos are processed for this check and are not saved.</p>
            </fieldset>
          </form>
          <details className="demo-options"><summary>Try a sample vehicle</summary><p>40 synthetic photos · Mock vision uses fixture labels, not image recognition.</p>
            {sampleError && <p role="status">{sampleError}</p>}
            <label htmlFor="sample">Plate and photo scenario</label>
            <select id="sample" value={sampleId} disabled={loading || !samples.length} onChange={e => setSampleId(e.target.value)}><option value="">Choose a sample</option>{samples.map(sample => <option key={sample.id} value={sample.id}>{sample.plate} · {scenarios[sample.scenario] || sample.scenario}</option>)}</select>
            <button className="secondary" type="button" disabled={loading || !sampleId} onClick={loadSample}>Use this sample</button>
          </details>
        </section>
        <section className="card results" ref={resultRef} tabIndex={-1} aria-label="Verification result" aria-live="polite" aria-busy={loading}>
          {loading ? <div className="empty"><span className="spinner large" /><h2>Analysing this vehicle</h2><p>Reading the photo, then checking the registry and comparing details.</p></div> : result ? <>
            <div className={`verdict ${tone}`}><div className="eyebrow">RESULT FOR {result.lookupPlate || 'PHOTO'}</div><h2>{status}</h2><p>{result.message}</p></div>
            <div className="result-body"><div className="plate-result"><strong>Detected plate: {result.detectedPlate || 'Unreadable / unavailable'}</strong><p>Lookup plate: {result.lookupPlate || 'Not selected'} · Source: {result.plateSource || 'image'}</p><button type="button" className="text-button" onClick={() => { const input = plateRef.current; input.closest('details').open = true; input.focus(); input.scrollIntoView({ block: 'center', behavior: 'smooth' }) }}>Correct plate</button></div>{result.visionMode && <p className="vision-mode">{result.visionMode === 'Mock fixture' ? 'Mock vision · synthetic fixture labels, not AI recognition' : 'AWS image analysis'}</p>}<div className="next-step"><strong>What to do next</strong><p>{status === 'Analysis unavailable' ? 'Check the provider setup or choose a bundled demo sample.' : status === 'Match' ? 'The compared attributes agree. Continue with your usual checks.' : status === 'Flagged' || status === 'Suspicious' ? 'Ask an authorised team member to review this result and follow your organisation’s procedure.' : status === 'Unknown Plate' ? 'Check the plate for typing errors. If it is correct, use your usual verification process.' : !file ? 'Add a clear vehicle photo to compare its appearance with the record.' : 'Review the details below. A clearer photo or another check may be needed.'}</p></div>
              <div className="comparison">{[['Expected vehicle', result.details?.expected], ['Detected vehicle', result.details?.detected]].map(([title, vehicle]) => <div className="vehicle-card" key={title}><h3>{title}</h3>{vehicle ? <dl>{fields.map(([key, label]) => <div key={key} className={result.mismatches?.includes(key === 'color' ? 'colour' : key) ? 'mismatch' : ''}><dt>{label}</dt><dd>{vehicle[key] || 'Unknown'}{title === 'Detected vehicle' && typeof result.detection?.[key === 'color' ? 'colour' : key]?.confidence === 'number' && <small> · {Math.round(result.detection[key === 'color' ? 'colour' : key].confidence * 100)}% estimate</small>}</dd></div>)}{typeof vehicle.confidence === 'number' && <div><dt>Confidence</dt><dd>{Math.round(vehicle.confidence * 100)}%</dd></div>}</dl> : <p>{title === 'Expected vehicle' ? 'No registry record found.' : !file ? 'No photo added.' : 'Photo analysis was not performed.'}</p>}</div>)}</div>
              {!!result.reasonDescriptions?.length && <div className="differences"><h3>Details to review</h3><ul>{result.reasonDescriptions.map((reason, i) => <li key={i}>{reason}</li>)}</ul></div>}
              {result.detection?.warnings?.map((warning, i) => <p className="helper" key={i}>{warning}</p>)}<div className="demo-note">Demonstration only. Registry records are fictional. Fixture labels describe intended image generation and have not been independently vision-validated. A result is not proof of a cloned or stolen plate.</div>
              <button type="button" className="secondary new-check" onClick={reset}>Check another vehicle</button>
            </div>
          </> : <div className="empty"><div className="empty-icon" aria-hidden="true">⌕</div><h2>Ready when you are</h2><p>Take or choose a photo of one vehicle.<br />Keep the plate and vehicle clearly visible.</p><div className="explainer"><div><span>✓</span><p><strong>Clear result</strong>See whether the details match or need a review.</p></div><div><span>▤</span><p><strong>Side-by-side details</strong>Compare the recorded and detected vehicle.</p></div><div><span>◎</span><p><strong>A useful next step</strong>Know what to check before taking action.</p></div></div></div>}
        </section>
      </div>
      <footer><strong>Built to support your judgement.</strong><span>Demo registry · Provider shown with results · No official registry connection</span></footer>
    </main>
  </>
}
