import { useCallback, useEffect, useRef, useState } from 'react'
import { deleteMaterial, fetchMaterials, fetchUploadJob, uploadMaterial, type Material, type UploadJob } from '../services/api'

const ACCEPTED = '.pdf,.txt,.md,.c,.h,.java,.sh'
const POLL_MS = 1000

export default function MaterialsPanel({ subject }: { subject: string }) {
  const [materials, setMaterials] = useState<Material[]>([])
  const [job, setJob] = useState<UploadJob | null>(null)
  const [waiting, setWaiting] = useState(0)
  const [errors, setErrors] = useState<string[]>([])
  const [dragging, setDragging] = useState(false)
  const subjectRef = useRef(subject)
  const busy = useRef(false)
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
    setErrors([])
    load(subject)
  }, [subject, load])

  // Fajlovi se obrađuju jedan za drugim (obrada troši procesor); promena predmeta prekida praćenje, ne i obradu na serveru.
  async function uploadAll(files: File[]) {
    if (files.length === 0 || busy.current) return
    busy.current = true
    const uploadSubject = subject
    setErrors([])
    try {
      for (const [index, file] of files.entries()) {
        setWaiting(files.length - index - 1)
        try {
          const id = await uploadMaterial(file, uploadSubject)
          for (;;) {
            const current = await fetchUploadJob(id)
            if (subjectRef.current !== uploadSubject) return
            setJob(current)
            if (current.state === 'error') throw new Error(current.error ?? 'Obrada nije uspela.')
            if (current.state === 'done') break
            await new Promise((resolve) => setTimeout(resolve, POLL_MS))
          }
        } catch (err) {
          if (subjectRef.current === uploadSubject)
            setErrors((prev) => [...prev, `${file.name}: ${err instanceof Error ? err.message : 'slanje nije uspelo'}`])
        }
        await load(uploadSubject)
      }
    } finally {
      busy.current = false
      setWaiting(0)
      setJob(null)
    }
  }

  function onPick(e: React.ChangeEvent<HTMLInputElement>) {
    const files = [...(e.target.files ?? [])]
    e.target.value = ''
    uploadAll(files)
  }

  function onDrop(e: React.DragEvent) {
    e.preventDefault()
    setDragging(false)
    uploadAll([...e.dataTransfer.files])
  }

  async function remove(file: string) {
    try {
      await deleteMaterial(subject, file)
      await load(subject)
    } catch (err) {
      setErrors([err instanceof Error ? err.message : 'Brisanje nije uspelo.'])
    }
  }

  const running = job?.state === 'running'
  const percent = job && job.total > 0 ? Math.round((job.processed / job.total) * 100) : 0

  return (
    <section
      className={`materials${dragging ? ' dragging' : ''}`}
      onDragOver={(e) => {
        e.preventDefault()
        setDragging(true)
      }}
      onDragLeave={() => setDragging(false)}
      onDrop={onDrop}
    >
      <h2>Moj materijal</h2>
      {materials.length === 0 && !running && <p className="muted small">Za ovaj predmet nema dodatog materijala.</p>}
      <ul>
        {materials.map((m) => (
          <li key={m.sourceFile}>
            <span className="file-name" title={m.sourceFile}>
              {m.sourceFile}
            </span>
            <span className="muted small">{m.chunks}</span>
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
            {waiting > 0 && `, još ${waiting} u redu`}
          </div>
          <div className="progress">
            <div style={{ width: `${percent}%` }} />
          </div>
        </div>
      )}
      {errors.map((message) => (
        <p key={message} className="error small">
          {message}
        </p>
      ))}

      <input ref={fileInput} type="file" accept={ACCEPTED} multiple onChange={onPick} hidden />
      <button className="secondary" onClick={() => fileInput.current?.click()} disabled={running}>
        Dodaj fajlove
      </button>
      <p className="muted small">Prevuci fajlove ovde ili ih izaberi (PDF, TXT, MD, C, H, JAVA, SH). Broj uz fajl je broj delova teksta. Ostaje na ovom računaru.</p>
    </section>
  )
}
