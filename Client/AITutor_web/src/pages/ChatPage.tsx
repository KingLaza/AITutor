import { useState, useRef, useEffect } from 'react'
import { sendChatMessage, type ChatMessage } from '../services/chatService'

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  async function handleSend() {
    const text = input.trim()
    if (!text || loading) return

    setMessages((prev) => [...prev, { role: 'user', content: text }])
    setInput('')
    setLoading(true)

    try {
      const reply = await sendChatMessage(text)
      setMessages((prev) => [...prev, { role: 'assistant', content: reply }])
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
      handleSend()
    }
  }

  return (
    <div className="chat-page">
      <header className="header">
        <h1>AITutor</h1>
        <p>test konekcije — direktan razgovor sa lokalnim modelom</p>
      </header>
      <main className="chat">
        {messages.length === 0 && <div className="empty">Postavi pitanje da testiraš konekciju.</div>}
        {messages.map((m, i) => (
          <div key={i} className={`bubble ${m.role}`}>{m.content}</div>
        ))}
        {loading && <div className="bubble assistant loading">…</div>}
        <div ref={bottomRef} />
      </main>
      <footer className="composer">
        <textarea value={input} onChange={(e) => setInput(e.target.value)} onKeyDown={handleKeyDown} placeholder="Unesi pitanje…" rows={2} />
        <button onClick={handleSend} disabled={loading}>Pošalji</button>
      </footer>
    </div>
  )
}
