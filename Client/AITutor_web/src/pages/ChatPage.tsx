import { useState, useRef, useEffect } from 'react'
import { fetchSubjects, sendChatMessage, type ChatMessage, type TaskState } from '../services/chatService'

const MAX_HINT_LEVEL = 3

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [activeTask, setActiveTask] = useState<TaskState | null>(null)
  const [subjects, setSubjects] = useState<string[]>([])
  const [subject, setSubject] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, loading])

  useEffect(() => {
    fetchSubjects()
      .then((list) => {
        setSubjects(list)
        setSubject((current) => current ?? list[0] ?? null)
      })
      .catch(() => setSubjects([]))
  }, [])

  function changeSubject(next: string) {
    setSubject(next)
    setMessages([])
    setActiveTask(null)
  }

  async function send(text: string) {
    if (!text || loading) return

    const history = messages
    setMessages((prev) => [...prev, { role: 'user', content: text }])
    setInput('')
    setLoading(true)

    try {
      const result = await sendChatMessage(text, history, activeTask, subject)
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
    <div className="chat-page">
      <header className="header">
        <h1>AITutor</h1>
        <div className="subject-row">
          <label htmlFor="subject">Predmet</label>
          <select id="subject" value={subject ?? ''} onChange={(e) => changeSubject(e.target.value)} disabled={subjects.length === 0 || loading}>
            {subjects.length === 0 && <option value="">(nema predmeta)</option>}
            {subjects.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
        <p>Pitanja iz gradiva i zadaci sa nagoveštajima</p>
      </header>
      <main className="chat">
        {messages.length === 0 && (
          <div className="empty">Postavi pitanje iz gradiva ili nalepi zadatak sa ispita.</div>
        )}
        {messages.map((m, i) => (
          <div key={i} className={`message ${m.role}`}>
            <div className={`bubble ${m.role}`}>{m.content}</div>
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
        {loading && <div className="bubble assistant loading">…</div>}
        {showHintButtons && (
          <div className="hint-actions">
            <span className="hint-level">Nagoveštaj {activeTask.level}/{MAX_HINT_LEVEL}</span>
            <button onClick={() => send('Sledeći nagoveštaj')}>Sledeći nagoveštaj</button>
            <button onClick={() => send('Prikaži rešenje')}>Prikaži rešenje</button>
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
        <button onClick={() => send(input.trim())} disabled={loading}>
          Pošalji
        </button>
      </footer>
    </div>
  )
}
