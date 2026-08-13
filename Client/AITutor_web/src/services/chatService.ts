export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

interface ChatResponse {
  reply: string
}

const API_URL = 'http://localhost:5000/api/chat'

export async function sendChatMessage(message: string): Promise<string> {
  const res = await fetch(API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message }),
  })
  if (!res.ok) throw new Error(`Server vratio ${res.status}`)
  const data: ChatResponse = await res.json()
  return data.reply
}
