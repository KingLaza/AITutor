import { useEffect, useState } from 'react'
import {
  deleteConversation,
  deleteProfile,
  fetchConversations,
  fetchMessages,
  renameProfile,
  type Conversation,
  type Profile,
} from '../services/api'

interface Props {
  profile: Profile
  onBack: () => void
  onOpen: (subject: string, conversationId: number) => void
  onRenamed: (profile: Profile) => void
  onDeleted: () => void
}

function formatDate(iso: string): string {
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('sr-Latn-RS', { day: 'numeric', month: 'numeric', year: 'numeric' })
}

export default function ProfilePage({ profile, onBack, onOpen, onRenamed, onDeleted }: Props) {
  const [name, setName] = useState(profile.name)
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null)

  const load = () => {
    fetchConversations(profile.id)
      .then(setConversations)
      .catch(() => setConversations([]))
  }
  useEffect(load, [profile.id])

  async function rename(e: React.FormEvent) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed || trimmed === profile.name) return
    try {
      onRenamed(await renameProfile(profile.id, trimmed))
      setMessage({ text: 'Ime je promenjeno.', error: false })
    } catch (err) {
      setMessage({ text: err instanceof Error ? err.message : 'Promena nije uspela.', error: true })
    }
  }

  // Razgovor se izvozi kao običan tekst, da se može sačuvati ili poslati.
  async function exportConversation(c: Conversation) {
    const messages = await fetchMessages(profile.id, c.subject, c.id)
    const text = messages.map((m) => `${m.role === 'user' ? profile.name : 'AITutor'}:\n${m.content}`).join('\n\n')
    const url = URL.createObjectURL(new Blob([`${c.subject}\n\n${text}\n`], { type: 'text/plain;charset=utf-8' }))
    const link = document.createElement('a')
    link.href = url
    link.download = `razgovor-${c.subject.replace(/\s+/g, '-')}-${c.id}.txt`
    link.click()
    URL.revokeObjectURL(url)
  }

  async function removeConversation(c: Conversation) {
    if (!window.confirm(`Obrisati razgovor „${c.title || 'bez naslova'}“?`)) return
    await deleteConversation(profile.id, c.subject, c.id)
    load()
  }

  async function removeProfile() {
    if (!window.confirm(`Obrisati profil „${profile.name}“ i sve njegove razgovore? Ovo se ne može poništiti.`)) return
    await deleteProfile(profile.id)
    onDeleted()
  }

  const bySubject = [...new Set(conversations.map((c) => c.subject))]

  return (
    <div className="profile-page">
      <button className="link" onClick={onBack}>
        ← Nazad na razgovor
      </button>
      <h1>Profil</h1>

      <form onSubmit={rename} className="profile-form">
        <label htmlFor="rename">Ime</label>
        <div className="row">
          <input id="rename" value={name} maxLength={40} onChange={(e) => setName(e.target.value)} />
          <button type="submit" className="secondary" disabled={!name.trim() || name.trim() === profile.name}>
            Sačuvaj
          </button>
        </div>
        {message && <p className={`small ${message.error ? 'error' : 'muted'}`}>{message.text}</p>}
      </form>

      <h2>Razgovori</h2>
      {conversations.length === 0 && <p className="muted">Još nema sačuvanih razgovora.</p>}
      {bySubject.map((subject) => (
        <div key={subject} className="conversation-group">
          <h3>{subject}</h3>
          <ul>
            {conversations
              .filter((c) => c.subject === subject)
              .map((c) => (
                <li key={c.id}>
                  <div className="conversation-title">
                    <button className="link title" onClick={() => onOpen(c.subject, c.id)}>
                      {c.title || 'Bez naslova'}
                    </button>
                    <span className="muted small">
                      {formatDate(c.updated)} · {c.messages} poruka
                    </span>
                  </div>
                  <div className="conversation-actions">
                    <button className="link" onClick={() => exportConversation(c)}>
                      izvezi
                    </button>
                    <button className="link" onClick={() => removeConversation(c)}>
                      obriši
                    </button>
                  </div>
                </li>
              ))}
          </ul>
        </div>
      ))}

      <div className="danger-zone">
        <button className="secondary danger" onClick={removeProfile}>
          Obriši profil
        </button>
        <p className="muted small">Briše profil i sve njegove razgovore. Materijal koji je dodat ostaje.</p>
      </div>
    </div>
  )
}
