const TYPE_LABELS: Record<string, string> = {
  predavanja: 'Predavanja',
  vezbe: 'Vežbe',
  reseni_blanketi: 'Rešeni blanketi',
  usmeni: 'Usmeni',
  obrasci: 'Obrasci',
  pismeni: 'Ispit',
  moje: 'Moje',
}

// API vraća "tip/fajl (dist: 0.551)" ili "tip/fajl (nastavak)"; studentu je bitno samo odakle potiče i bez ponavljanja.
function parse(source: string): { type: string; file: string } {
  const withoutScore = source.replace(/\s*\((?:dist:[^)]*|nastavak)\)\s*$/, '')
  const slash = withoutScore.indexOf('/')
  const type = slash < 0 ? '' : withoutScore.slice(0, slash)
  const file = slash < 0 ? withoutScore : withoutScore.slice(slash + 1)
  return { type: TYPE_LABELS[type] ?? type, file: file.replace(/\.[A-Za-z0-9]+$/, '') }
}

export default function Sources({ sources }: { sources: string[] }) {
  const unique = [...new Map(sources.map(parse).map((s) => [`${s.type}/${s.file}`, s])).values()]
  if (unique.length === 0) return null
  return (
    <details className="sources">
      <summary>Izvori ({unique.length})</summary>
      <ul>
        {unique.map((s) => (
          <li key={`${s.type}/${s.file}`}>
            {s.type && <span className="source-type">{s.type}</span>}
            {s.file}
          </li>
        ))}
      </ul>
    </details>
  )
}
