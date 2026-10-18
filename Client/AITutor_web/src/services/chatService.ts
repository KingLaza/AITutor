export interface TaskState {
  text: string
  level: number
}

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
  sources?: string[]
  intent?: string
}

export interface ChatResult {
  reply: string
  sources: string[]
  intent: string
  activeTask: TaskState | null
}

interface ChatResponse {
  reply: string
  sources: string[]
  intent: string
  activeTask: TaskState | null
}

// VITE_API_URL: adresa API-ja (podrazumevano lokalni razvoj); u Dockeru je prazna, pa se koristi isti origin preko nginx proksija.
const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'
const API_URL = `${API_BASE}/api/chat`
const SUBJECTS_URL = `${API_BASE}/api/subjects`
const HISTORY_TURNS = 6

export async function sendChatMessage(
  message: string,
  history: ChatMessage[],
  activeTask: TaskState | null,
  subject: string | null,
): Promise<ChatResult> {
  const res = await fetch(API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      message,
      history: history.slice(-HISTORY_TURNS).map((m) => ({ role: m.role, content: m.content })),
      activeTask,
      subject,
    }),
  })
  if (!res.ok) throw new Error(`Server vratio ${res.status}`)
  const data: ChatResponse = await res.json()
  return { reply: data.reply, sources: data.sources ?? [], intent: data.intent, activeTask: data.activeTask ?? null }
}

export async function fetchSubjects(): Promise<string[]> {
  const res = await fetch(SUBJECTS_URL)
  if (!res.ok) throw new Error(`Server vratio ${res.status}`)
  return res.json()
}
