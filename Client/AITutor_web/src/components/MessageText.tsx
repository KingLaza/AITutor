import type { ReactNode } from 'react'

// Minimalan prikaz odgovora: blokovi koda (```), liste ("- " ili "* "), **podebljano** i `kod` u tekstu. Sve ostalo je običan tekst.
function inline(text: string): ReactNode[] {
  return text.split(/(\*\*[^*]+\*\*|`[^`]+`)/).map((part, i) => {
    if (part.startsWith('**') && part.endsWith('**') && part.length > 4) return <strong key={i}>{part.slice(2, -2)}</strong>
    if (part.startsWith('`') && part.endsWith('`') && part.length > 2) return <code key={i}>{part.slice(1, -1)}</code>
    return part
  })
}

function paragraphs(text: string, keyBase: number): ReactNode[] {
  const blocks: ReactNode[] = []
  let list: string[] = []
  let prose: string[] = []
  const flushProse = () => {
    if (prose.length > 0) blocks.push(<p key={`${keyBase}-p${blocks.length}`}>{inline(prose.join('\n'))}</p>)
    prose = []
  }
  const flushList = () => {
    if (list.length > 0)
      blocks.push(
        <ul key={`${keyBase}-l${blocks.length}`}>
          {list.map((item, i) => (
            <li key={i}>{inline(item)}</li>
          ))}
        </ul>,
      )
    list = []
  }
  for (const line of text.split('\n')) {
    const bullet = /^\s*[-*]\s+(.*)$/.exec(line)
    if (bullet) {
      flushProse()
      list.push(bullet[1])
    } else if (line.trim() === '') {
      flushProse()
      flushList()
    } else {
      flushList()
      prose.push(line)
    }
  }
  flushProse()
  flushList()
  return blocks
}

export default function MessageText({ text }: { text: string }) {
  const parts = text.split(/```[^\n]*\n?/)
  return (
    <>
      {parts.map((part, i) =>
        i % 2 === 1 ? (
          <pre key={i}>
            <code>{part.replace(/\n$/, '')}</code>
          </pre>
        ) : (
          paragraphs(part, i)
        ),
      )}
    </>
  )
}
