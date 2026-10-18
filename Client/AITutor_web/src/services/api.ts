export interface TaskState {
  text: string
  level: number
}

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
  sources?: string[]
  intent?: string
  activeTask?: TaskState | null
}

export interface ChatResult {
  reply: string
  sources: string[]
  intent: string
  activeTask: TaskState | null
}

export interface Profile {
  id: number
  name: string
}

export interface Material {
  sourceFile: string
  chunks: number
}

export interface UploadJob {
  id: string
  fileName: string
  subject: string
  state: 'running' | 'done' | 'error'
  processed: number
  total: number
  chunks: number
  error: string | null
}

// VITE_API_URL: adresa API-ja (podrazumevano lokalni razvoj); u Dockeru je prazna, pa se koristi isti origin preko nginx proksija.
const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'
const HISTORY_TURNS = 6

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, init)
  if (!res.ok) {
    const detail = (await res.text().catch(() => '')).trim()
    throw new Error(detail && detail.length < 200 ? detail : `Server vratio ${res.status}`)
  }
  return res.status === 204 ? (undefined as T) : res.json()
}

const json = (method: string, body: unknown): RequestInit => ({
  method,
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(body),
})

export function fetchSubjects(): Promise<string[]> {
  return request('/api/subjects')
}

export function sendChatMessage(
  message: string,
  history: ChatMessage[],
  activeTask: TaskState | null,
  subject: string | null,
  profileId: number,
): Promise<ChatResult> {
  return request<ChatResult>(
    '/api/chat',
    json('POST', {
      message,
      history: history.slice(-HISTORY_TURNS).map((m) => ({ role: m.role, content: m.content })),
      activeTask,
      subject,
      profileId,
    }),
  ).then((r) => ({ ...r, sources: r.sources ?? [], activeTask: r.activeTask ?? null }))
}

export function fetchProfiles(): Promise<Profile[]> {
  return request('/api/profiles')
}

export function createProfile(name: string): Promise<Profile> {
  return request('/api/profiles', json('POST', { name }))
}

export function fetchMessages(profileId: number, subject: string): Promise<ChatMessage[]> {
  return request(`/api/profiles/${profileId}/messages?subject=${encodeURIComponent(subject)}`)
}

export function clearMessages(profileId: number, subject: string): Promise<void> {
  return request(`/api/profiles/${profileId}/messages?subject=${encodeURIComponent(subject)}`, { method: 'DELETE' })
}

export function fetchMaterials(subject: string): Promise<Material[]> {
  return request(`/api/materials?subject=${encodeURIComponent(subject)}`)
}

export function deleteMaterial(subject: string, file: string): Promise<void> {
  return request(`/api/materials?subject=${encodeURIComponent(subject)}&file=${encodeURIComponent(file)}`, {
    method: 'DELETE',
  })
}

export async function uploadMaterial(file: File, subject: string): Promise<string> {
  const form = new FormData()
  form.append('file', file)
  form.append('subject', subject)
  const { jobId } = await request<{ jobId: string }>('/api/materials/upload', { method: 'POST', body: form })
  return jobId
}

export function fetchUploadJob(id: string): Promise<UploadJob> {
  return request(`/api/materials/jobs/${id}`)
}
