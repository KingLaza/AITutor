import { useState, useRef, useEffect } from 'react'
import MaterialsPanel from '../components/MaterialsPanel'
import MessageText from '../components/MessageText'
import Origin from '../components/Origin'
import ProfilePage from '../components/ProfilePage'
import Sources from '../components/Sources'
import {
  deleteConversation,
  fetchConversations,
  fetchMessages,
  fetchSubjects,
  sendChatMessage,
  type AnswerMode,
  type ChatMessage,
  type Conversation,
  type Profile,
  type TaskState,
} from '../services/api'

const MAX_HINT_LEVEL = 3
// Server na početak izračunatog rešenja piše napomenu; u UI je zamenjuje oznaka porekla ispod odgovora.
const CALCULATED_PREFIX = /^Rešenje izračunato programski[^\n]*\n+/
const SUBJECT_KEY = 'aitutor.subject'
const MODE_KEY = 'aitutor.mode'

const MODES: { value: AnswerMode; label: string }[] = [
  { value: 'Rag', label: 'Ceo sistem (pretraga i alati)' },
  { value: 'RagNoTools', label: 'Samo pretraga, bez alata' },
  { value: 'Plain', label: 'Bez materijala (samo model)' },
]

function stored(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

function store(key: string, value: string) {
  try {
    localStorage.setItem(key, value)
  } catch {
    // nije kritično
  }
}

interface Current {
  subject: string
  id: number
}

interface Props {
  profile: Profile
  onSwitchProfile: () => void
  onProfileRenamed: (profile: Profile) => void
  onProfileDeleted: () => void
}

export default function ChatPage({ profile, onSwitchProfile, onProfileRenamed, onProfileDeleted }: Props) {
  const [view, setView] = useState<'chat' | 'profile'>('chat')
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [activeTask, setActiveTask] = useState<TaskState | null>(null)
  const [subjects, setSubjects] = useState<string[]>([])
  const [subject, setSubject] = useState<string | null>(null)
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [current, setCurrent] = useState<Current | null>(null)
  const [mode, setMode] = useState<AnswerMode>(() => (MODES.some((m) => m.value === stored(MODE_KEY)) ? (stored(MODE_KEY) as AnswerMode) : 'Rag'))
  const pendingOpen = useRef<Current | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, loading])

  useEffect(() => {
    fetchSubjects()
      .then((list) => {
        setSubjects(list)
        const saved = stored(SUBJECT_KEY)
        setSubject(saved && list.includes(saved) ? saved : (list[0] ?? null))
      })
      .catch(() => setSubjects([]))
  }, [])

  // Razgovori izabranog predmeta; otvara se najskoriji (ili otvoren sa stranice profila), a ako ih nema, počinje novi.
  useEffect(() => {
    if (!subject) return
    let cancelled = false
    fetchConversations(profile.id, subject)
      .catch(() => [] as Conversation[])
      .then((list) => {
        if (cancelled) return
        setConversations(list)
        const pending = pendingOpen.current
        pendingOpen.current = null
        setCurrent(pending && pending.subject === subject ? pending : { subject, id: list[0]?.id ?? 1 })
      })
    return () => {
      cancelled = true
    }
  }, [profile.id, subject])

  // Poruke razgovora; aktivni zadatak se vraća iz poslednjeg odgovora.
  useEffect(() => {
    if (!current) return
    let cancelled = false
    fetchMessages(profile.id, current.subject, current.id)
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
  }, [profile.id, current])

  function refreshConversations() {
    if (subject) fetchConversations(profile.id, subject).then(setConversations).catch(() => undefined)
  }

  function changeSubject(next: string) {
    setSubject(next)
    store(SUBJECT_KEY, next)
  }

  function changeMode(next: AnswerMode) {
    setMode(next)
    store(MODE_KEY, next)
  }

  function newConversation() {
    if (!subject || !current || loading || messages.length === 0) return
    const next = Math.max(current.id, ...conversations.map((c) => c.id)) + 1
    setCurrent({ subject, id: next })
    setMessages([])
    setActiveTask(null)
  }

  async function removeConversation(c: Conversation) {
    if (!window.confirm(`Obrisati razgovor „${c.title || 'bez naslova'}“?`)) return
    await deleteConversation(profile.id, c.subject, c.id)
    const rest = conversations.filter((x) => x.id !== c.id)
    setConversations(rest)
    if (current?.id === c.id) setCurrent({ subject: c.subject, id: rest[0]?.id ?? 1 })
  }

  function openFromProfile(targetSubject: string, id: number) {
    setView('chat')
    if (targetSubject === subject) {
      setCurrent({ subject: targetSubject, id })
    } else {
      pendingOpen.current = { subject: targetSubject, id }
      changeSubject(targetSubject)
    }
  }

  async function send(text: string) {
    if (!text || loading || !subject || !current) return

    const history = messages
    setMessages((prev) => [...prev, { role: 'user', content: text }])
    setInput('')
    setLoading(true)

    try {
      const result = await sendChatMessage(text, history, activeTask, subject, profile.id, current.id, mode)
      setMessages((prev) => [
        ...prev,
        { role: 'assistant', content: result.reply, sources: result.sources, intent: result.intent, origin: result.origin },
      ])
      setActiveTask(result.activeTask)
      refreshConversations()
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
  const currentIsListed = conversations.some((c) => c.id === current?.id)

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="brand">AITutor</div>

        <div className="field">
          <span className="label">Profil</span>
          <div className="profile-line">
            <strong>{profile.name}</strong>
            <span>
              <button className="link" onClick={() => setView(view === 'profile' ? 'chat' : 'profile')}>
                profil
              </button>
              {' · '}
              <button className="link" onClick={onSwitchProfile}>
                promeni
              </button>
            </span>
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

        <section className="conversations">
          <div className="section-head">
            <h2>Razgovori</h2>
            <button className="link" onClick={newConversation} disabled={loading || messages.length === 0}>
              novi
            </button>
          </div>
          <ul>
            {!currentIsListed && current && (
              <li className="active">
                <span className="conversation-name">Novi razgovor</span>
              </li>
            )}
            {conversations.map((c) => (
              <li key={c.id} className={c.id === current?.id ? 'active' : ''}>
                <button className="link conversation-name" onClick={() => !loading && setCurrent({ subject: c.subject, id: c.id })}>
                  {c.title || 'Bez naslova'}
                </button>
                <button className="link" onClick={() => removeConversation(c)} aria-label="Obriši razgovor">
                  ×
                </button>
              </li>
            ))}
          </ul>
        </section>

        {subject && <MaterialsPanel subject={subject} />}

        <details className="advanced">
          <summary>Napredno</summary>
          <label className="label" htmlFor="mode">
            Režim odgovora
          </label>
          <select id="mode" value={mode} onChange={(e) => changeMode(e.target.value as AnswerMode)}>
            {MODES.map((m) => (
              <option key={m.value} value={m.value}>
                {m.label}
              </option>
            ))}
          </select>
        </details>
      </aside>

      <div className="main">
        {view === 'profile' ? (
          <ProfilePage
            profile={profile}
            onBack={() => setView('chat')}
            onOpen={openFromProfile}
            onRenamed={onProfileRenamed}
            onDeleted={onProfileDeleted}
          />
        ) : (
          <>
            <main className="chat">
              {messages.length === 0 && (
                <div className="empty">Postavi pitanje iz gradiva ili nalepi zadatak sa ispita.</div>
              )}
              {messages.map((m, i) => (
                <div key={i} className={`message ${m.role}`}>
                  <div className="bubble">
                    <MessageText text={m.origin === 'calculated' ? m.content.replace(CALCULATED_PREFIX, '') : m.content} />
                  </div>
                  {m.role === 'assistant' && (
                    <div className="meta">
                      <Origin origin={m.origin} />
                      {m.sources && <Sources sources={m.sources} />}
                    </div>
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
          </>
        )}
      </div>
    </div>
  )
}
