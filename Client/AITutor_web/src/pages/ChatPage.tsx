import { useState, useRef, useEffect } from 'react'
import MaterialsPanel from '../components/MaterialsPanel'
import MessageText from '../components/MessageText'
import {
  clearMessages,
  fetchMessages,
  fetchSubjects,
  sendChatMessage,
  type ChatMessage,
  type Profile,
  type TaskState,
} from '../services/api'

const MAX_HINT_LEVEL = 3
const SUBJECT_KEY = 'aitutor.subject'

function storedSubject(): string | null {
  try {
    return localStorage.getItem(SUBJECT_KEY)
  } catch {
    return null
  }
}

interface Props {
  profile: Profile
  onSwitchProfile: () => void
}

export default function ChatPage({ profile, onSwitchProfile }: Props) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [activeTask, setActiveTask] = useState<TaskState | null>(null)
  const [subjects, setSubjects] = useState<string[]>([])
  const [subject, setSubject] = useState<string | null>(null)
  const [menuOpen, setMenuOpen] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, loading])

  useEffect(() => {
    fetchSubjects()
      .then((list) => {
        setSubjects(list)
        const saved = storedSubject()
        setSubject(saved && list.includes(saved) ? saved : (list[0] ?? null))
      })
      .catch(() => setSubjects([]))
  }, [])

  // Istorija se učitava za svaki par (profil, predmet); aktivni zadatak se vraća iz poslednjeg odgovora.
  useEffect(() => {
    if (!subject) return
    let cancelled = false
    fetchMessages(profile.id, subject)
      .then((stored) => {
        if (cancelled) return
        setMessages(stored)
        setActiveTask(stored.length > 0 ? (stored[stored.length - 1].activeTask ?? null) : null)
      })
      .catch(() => {
        if (cancelled) return
        setMessages([])
        setActiveTask(null)
      })
    return () => {
      cancelled = true
    }
  }, [profile.id, subject])

  function changeSubject(next: string) {
    setSubject(next)
    try {
      localStorage.setItem(SUBJECT_KEY, next)
    } catch {
      // nije kritično
    }
  }

  async function newConversation() {
    if (!subject || loading) return
    try {
      await clearMessages(profile.id, subject)
      setMessages([])
      setActiveTask(null)
      setMenuOpen(false)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Nepoznata greška'
      setMessages((prev) => [...prev, { role: 'assistant', content: `Greška: ${msg}` }])
    }
  }

  async function send(text: string) {
    if (!text || loading || !subject) return

    const history = messages
    setMessages((prev) => [...prev, { role: 'user', content: text }])
    setInput('')
    setLoading(true)

    try {
      const result = await sendChatMessage(text, history, activeTask, subject, profile.id)
      setMessages((prev) => [
        ...prev,
        { role: 'assistant', content: result.reply, sources: result.sources, intent: result.intent },
      ])
      setActiveTask(result.activeTask)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Nepoznata greška'
      setMessages((prev) => [...prev, { role: 'assistant', content: `Greška: ${msg}` }])
    } finally {
      setLoading(false)
    }
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      send(input.trim())
    }
  }

  const showHintButtons = activeTask !== null && activeTask.level < MAX_HINT_LEVEL && !loading

  return (
    <div className={`layout ${menuOpen ? 'menu-open' : ''}`}>
      <aside className="sidebar">
        <div className="brand">AITutor</div>

        <div className="field">
          <span className="label">Profil</span>
          <div className="profile-line">
            <strong>{profile.name}</strong>
            <button className="link" onClick={onSwitchProfile}>
              promeni
            </button>
          </div>
        </div>

        <div className="field">
          <label className="label" htmlFor="subject">
            Predmet
          </label>
          <select
            id="subject"
            value={subject ?? ''}
            onChange={(e) => changeSubject(e.target.value)}
            disabled={subjects.length === 0 || loading}
          >
            {subjects.length === 0 && <option value="">(nema predmeta)</option>}
            {subjects.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>

        <button className="secondary" onClick={newConversation} disabled={loading || messages.length === 0}>
          Novi razgovor
        </button>

        {subject && <MaterialsPanel subject={subject} />}
      </aside>

      <div className="main">
        <div className="topbar">
          <button className="link" onClick={() => setMenuOpen((open) => !open)}>
            {menuOpen ? 'Zatvori' : 'Meni'}
          </button>
          <span>{subject}</span>
        </div>

        <main className="chat">
          {messages.length === 0 && (
            <div className="empty">Postavi pitanje iz gradiva ili nalepi zadatak sa ispita.</div>
          )}
          {messages.map((m, i) => (
            <div key={i} className={`message ${m.role}`}>
              <div className="bubble">
                <MessageText text={m.content} />
              </div>
              {m.role === 'assistant' && m.sources && m.sources.length > 0 && (
                <details className="sources">
                  <summary>Izvori ({m.sources.length})</summary>
                  <ul>
                    {m.sources.map((s, j) => (
                      <li key={j}>{s}</li>
                    ))}
                  </ul>
                </details>
              )}
            </div>
          ))}
          {loading && <div className="message assistant muted">Odgovor se priprema…</div>}
          {showHintButtons && (
            <div className="hint-actions">
              <span className="muted small">
                Nagoveštaj {activeTask.level}/{MAX_HINT_LEVEL}
              </span>
              <button className="secondary" onClick={() => send('Sledeći nagoveštaj')}>
                Sledeći nagoveštaj
              </button>
              <button className="secondary" onClick={() => send('Prikaži rešenje')}>
                Prikaži rešenje
              </button>
            </div>
          )}
          <div ref={bottomRef} />
        </main>

        <footer className="composer">
          <textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Unesi pitanje ili zadatak…"
            rows={2}
          />
          <button className="primary" onClick={() => send(input.trim())} disabled={loading || !input.trim()}>
            Pošalji
          </button>
        </footer>
      </div>
    </div>
  )
}
