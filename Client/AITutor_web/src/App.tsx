import { useEffect, useState } from 'react'
import ChatPage from './pages/ChatPage'
import ProfilePicker from './components/ProfilePicker'
import { fetchProfiles, type Profile } from './services/api'

const PROFILE_KEY = 'aitutor.profileId'

function storedProfileId(): number | null {
  try {
    const value = localStorage.getItem(PROFILE_KEY)
    return value ? Number(value) : null
  } catch {
    return null
  }
}

function storeProfileId(id: number | null) {
  try {
    if (id === null) localStorage.removeItem(PROFILE_KEY)
    else localStorage.setItem(PROFILE_KEY, String(id))
  } catch {
    // privatni režim: profil se samo ne pamti između poseta
  }
}

function App() {
  const [profiles, setProfiles] = useState<Profile[] | null>(null)
  const [profile, setProfile] = useState<Profile | null>(null)
  const [failed, setFailed] = useState(false)

  function load() {
    setFailed(false)
    fetchProfiles()
      .then((list) => {
        setProfiles(list)
        const saved = storedProfileId()
        setProfile((current) => current ?? list.find((p) => p.id === saved) ?? null)
      })
      .catch(() => setFailed(true))
  }

  useEffect(load, [])

  function select(next: Profile) {
    storeProfileId(next.id)
    setProfile(next)
    setProfiles((list) => (list && !list.some((p) => p.id === next.id) ? [...list, next] : list))
  }

  function switchProfile() {
    storeProfileId(null)
    setProfile(null)
  }

  if (failed) {
    return (
      <div className="picker">
        <h1>AITutor</h1>
        <p className="error">Server nije dostupan. Proveri da li je API pokrenut.</p>
        <button className="secondary" onClick={load}>
          Pokušaj ponovo
        </button>
      </div>
    )
  }
  if (profiles === null) return <div className="picker muted">Učitavanje…</div>
  if (profile === null) return <ProfilePicker profiles={profiles} onSelect={select} />
  return (
    <ChatPage
      profile={profile}
      onSwitchProfile={switchProfile}
      onProfileRenamed={(renamed) => {
        setProfile(renamed)
        setProfiles((list) => list && list.map((p) => (p.id === renamed.id ? renamed : p)))
      }}
      onProfileDeleted={() => {
        setProfiles((list) => list && list.filter((p) => p.id !== profile.id))
        switchProfile()
      }}
    />
  )
}

export default App
