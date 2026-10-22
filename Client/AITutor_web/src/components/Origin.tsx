// Oznaka porekla odgovora: šta je izračunao kod, a šta je napisao model (sve ostalo, npr. ćaskanje i odbijanje, bez oznake).
const LABELS: Record<string, { text: string; calculated: boolean }> = {
  calculated: { text: 'Izračunato programski, bez procene modela', calculated: true },
  hint: { text: 'Unapred napisan nagoveštaj za ovaj tip zadatka', calculated: true },
  generated: { text: 'Generisao model, proveri prema izvorima', calculated: false },
}

export default function Origin({ origin }: { origin?: string }) {
  const label = origin ? LABELS[origin] : undefined
  if (!label) return null
  return <div className={`origin${label.calculated ? ' calculated' : ''}`}>{label.text}</div>
}
