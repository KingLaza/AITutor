import { useState } from 'react'
import { createProfile, type Profile } from '../services/api'

interface Props {
  profiles: Profile[]
  onSelect: (profile: Profile) => void
}

export default function ProfilePicker({ profiles, onSelect }: Props) {
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function create(e: React.FormEvent) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed || busy) return
    setBusy(true)
    setError(null)
    try {
      onSelect(await createProfile(trimmed))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Profil nije napravljen.')
      setBusy(false)
    }
  }

  return (
    <div className="picker">
      <h1>AITutor</h1>
      <p className="muted">Izaberi profil da bi se razgovori čuvali na ovom računaru.</p>

      {profiles.length > 0 && (
        <ul className="profile-list">
          {profiles.map((p) => (
            <li key={p.id}>
              <button onClick={() => onSelect(p)}>{p.name}</button>
            </li>
          ))}
        </ul>
      )}

      <form onSubmit={create} className="profile-form">
        <label htmlFor="profile-name">{profiles.length > 0 ? 'Ili napravi novi profil' : 'Ime profila'}</label>
        <div className="row">
          <input
            id="profile-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={40}
            placeholder="npr. Lazar"
            autoFocus
          />
          <button type="submit" className="primary" disabled={!name.trim() || busy}>
            Nastavi
          </button>
        </div>
        {error && <p className="error">{error}</p>}
      </form>
      <p className="muted small">Bez lozinke: profil služi samo za čuvanje istorije razgovora.</p>
    </div>
  )
}
