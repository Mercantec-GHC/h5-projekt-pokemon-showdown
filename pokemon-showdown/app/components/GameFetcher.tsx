"use client"

import { useState } from 'react'

export default function GameFetcher() {
  const [data, setData] = useState<any | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [savedPath, setSavedPath] = useState<string | null>(null)

  async function fetchPokemon() {
    setLoading(true)
    setError(null)
    try {
      const res = await fetch('/api/game')
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const json = await res.json()
      setData(json)
    } catch (err: any) {
      setError(err?.message ?? String(err))
      setData(null)
    } finally {
      setLoading(false)
    }
  }

  async function downloadAll() {
    setSaving(true)
    setError(null)
    setSavedPath(null)
    try {
      const res = await fetch('/api/game?save=true')
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const json = await res.json()
      if (json?.saved && json.path) {
        setSavedPath(json.path)
      } else {
        throw new Error(json?.error || 'Unknown response')
      }
    } catch (err: any) {
      setError(err?.message ?? String(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="w-full max-w-2xl">
      <div className="flex gap-3 mb-4">
        <button
          onClick={fetchPokemon}
          disabled={loading}
          className="px-4 py-2 bg-blue-600 text-white rounded disabled:opacity-50"
        >
          {loading ? 'Loading…' : 'Fetch Pokémon (Ditto)'}
        </button>
        <button
          onClick={() => { setData(null); setError(null); setSavedPath(null) }}
          className="px-3 py-2 border rounded"
        >
          Clear
        </button>
        <button
          onClick={downloadAll}
          disabled={saving}
          className="px-4 py-2 bg-green-600 text-white rounded disabled:opacity-50"
        >
          {saving ? 'Saving…' : 'Download full list & save'}
        </button>
      </div>

      {error && (
        <div className="mb-4 text-red-600">Error: {error}</div>
      )}

      {data ? (
        <pre className="p-4 bg-gray-100 rounded overflow-auto text-sm" style={{maxHeight: 400}}>
          {JSON.stringify(data, null, 2)}
        </pre>
      ) : (
        <div className="text-sm text-gray-500">No data yet. Click the button to fetch.</div>
      )}

      {savedPath && (
        <div className="mt-4">
          Saved JSON: <a href={savedPath} className="text-blue-600 underline">{savedPath}</a>
        </div>
      )}
    </div>
  )
}
