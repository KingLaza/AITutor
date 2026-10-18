import { useCallback, useEffect, useRef, useState } from 'react'
import { deleteMaterial, fetchMaterials, fetchUploadJob, uploadMaterial, type Material, type UploadJob } from '../services/api'

const ACCEPTED = '.pdf,.txt,.md,.c,.h,.java,.sh'
const POLL_MS = 1000

export default function MaterialsPanel({ subject }: { subject: string }) {
  const [materials, setMaterials] = useState<Material[]>([])
  const [job, setJob] = useState<UploadJob | null>(null)
  const [error, setError] = useState<string | null>(null)
  const subjectRef = useRef(subject)
  const fileInput = useRef<HTMLInputElement>(null)

  const load = useCallback(async (forSubject: string) => {
    try {
      const list = await fetchMaterials(forSubject)
      if (subjectRef.current === forSubject) setMaterials(list)
    } catch {
      if (subjectRef.current === forSubject) setMaterials([])
    }
  }, [])

  useEffect(() => {
    subjectRef.current = subject
    setJob(null)
    setError(null)
    load(subject)
  }, [subject, load])

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    const uploadSubject = subject
    setError(null)
    try {
      const id = await uploadMaterial(file, uploadSubject)
      for (;;) {
        const current = await fetchUploadJob(id)
        if (subjectRef.current !== uploadSubject) return
        setJob(current)
        if (current.state !== 'running') {
          if (current.state === 'error') setError(current.error ?? 'Obrada nije uspela.')
          break
        }
        await new Promise((resolve) => setTimeout(resolve, POLL_MS))
      }
      await load(uploadSubject)
    } catch (err) {
      if (subjectRef.current === uploadSubject) {
        setJob(null)
        setError(err instanceof Error ? err.message : 'Slanje nije uspelo.')
      }
    }
  }

  async function remove(file: string) {
    try {
      await deleteMaterial(subject, file)
      await load(subject)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Brisanje nije uspelo.')
    }
  }

  const running = job?.state === 'running'
  const percent = job && job.total > 0 ? Math.round((job.processed / job.total) * 100) : 0

  return (
    <section className="materials">
      <h2>Moj materijal</h2>
      {materials.length === 0 && !running && <p className="muted small">Za ovaj predmet nema dodatog materijala.</p>}
      <ul>
        {materials.map((m) => (
          <li key={m.sourceFile}>
            <span className="file-name" title={m.sourceFile}>
              {m.sourceFile}
            </span>
            <button className="link" onClick={() => remove(m.sourceFile)} aria-label={`Ukloni ${m.sourceFile}`}>
              ukloni
            </button>
          </li>
        ))}
      </ul>

      {running && job && (
        <div className="progress-wrap">
          <div className="small">
            Obrada: {job.fileName} {job.total > 0 && `(${job.processed}/${job.total})`}
          </div>
          <div className="progress">
            <div style={{ width: `${percent}%` }} />
          </div>
        </div>
      )}
      {error && <p className="error small">{error}</p>}

      <input ref={fileInput} type="file" accept={ACCEPTED} onChange={onFile} hidden />
      <button className="secondary" onClick={() => fileInput.current?.click()} disabled={running}>
        Dodaj fajl
      </button>
      <p className="muted small">PDF, TXT, MD, C, H, JAVA, SH. Ostaje na ovom računaru.</p>
    </section>
  )
}
