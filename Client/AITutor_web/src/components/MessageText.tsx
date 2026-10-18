// Prikazuje odgovor kao običan tekst; samo ograđeni blokovi koda dobijaju monospace prikaz.
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
          part.trim() && <p key={i}>{part.trim()}</p>
        ),
      )}
    </>
  )
}
