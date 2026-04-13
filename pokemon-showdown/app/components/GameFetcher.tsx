"use client"

import { useState, useEffect } from 'react'

const API = process.env.NEXT_PUBLIC_GAME_API_BASE ?? 'http://localhost:5108/game'

type Move = { name: string; damage_class: string | null }
type Poke = { id: number; name: string; currentHp: number; maxHp: number; fainted: boolean; sprites?: { front_default?: string }; moveset: Move[] }
type Battle = { battleId: string; turn: number; winner: 'player' | 'bot' | null; log: string[]; player: { activeIndex: number; team: Poke[] }; bot: { activeIndex: number; team: Poke[] } }

export default function GameFetcher() {
  const [battle, setBattle] = useState<Battle | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [move, setMove] = useState<string | null>(null)
  const [lastInput, setLastInput] = useState<string | null>(null)

  const p = battle?.player.team[battle.player.activeIndex]
  const b = battle?.bot.team[battle.bot.activeIndex]
  const moves = p?.moveset || []
  const moveIdx = moves.findIndex(m => m.name === move)

  const updateMove = (s: Battle) => {
    const a = s.player.team[s.player.activeIndex]
    if (!move || !a.moveset?.some(m => m.name === move)) setMove(a.moveset?.[0]?.name || null)
  }

  const [switchFocus, setSwitchFocus] = useState(false)
  const [switchIdx, setSwitchIdx] = useState<number | null>(null)
  const handleDirection = (dir: string) => {
    if (!moves.length || battle?.winner) return
    if (switchFocus && battle) {
      const team = battle.player.team
      let idx = switchIdx ?? 0
      if (dir === 'Left') {
        setSwitchIdx(((idx - 1) + team.length) % team.length)
      } else if (dir === 'Right') {
        setSwitchIdx((idx + 1) % team.length)
      } else if (dir === 'Up') {
        setSwitchFocus(false)
        setSwitchIdx(null)
      } else if (dir === 'Down') {
        setSwitchFocus(false)
        setSwitchIdx(null)
      }
      setLastInput(dir)
      setTimeout(() => setLastInput(null), 500)
      return
    }
    const idx = moveIdx === -1 ? 0 : moveIdx
    let row = Math.floor(idx / 2)
    let col = idx % 2
    let newIdx = idx
    if (dir === 'Up') {
      if (row === 0) return 
      newIdx = (row - 1) * 2 + col
    } else if (dir === 'Down') {
      if (row === 1) {
        setSwitchFocus(true)
        setLastInput(dir)
        setTimeout(() => setLastInput(null), 500)
        if (battle) {
          const team = battle.player.team
          let firstEnabled = team.findIndex((pk, i) => !pk.fainted && i !== battle.player.activeIndex)
          if (firstEnabled === -1) firstEnabled = 0
          setSwitchIdx(firstEnabled)
        } else {
          setSwitchIdx(0)
        }
        return
      }
      newIdx = (row + 1) * 2 + col
      if (newIdx >= moves.length) newIdx = idx 
    } else if (dir === 'Left') {
      if (col === 0) return 
      newIdx = row * 2 + (col - 1)
    } else if (dir === 'Right') {
      if (col === 1 || row * 2 + (col + 1) >= moves.length) return 
      newIdx = row * 2 + (col + 1)
    }
    setMove(moves[newIdx].name)
    setLastInput(dir)
    setTimeout(() => setLastInput(null), 500)
  }

  const handleAction = (action: string) => {
    if (action === 'select') {
      if (switchFocus) {
        if (switchIdx !== null && battle) {
          const team = battle.player.team
          if (switchIdx !== battle.player.activeIndex && !team[switchIdx].fainted) {
            turn('switch', switchIdx)
            setSwitchFocus(false)
            setSwitchIdx(null)
          }
        }
      } else {
        if (move) {
          turn('move')
        }
      }
    }
    setLastInput(action)
    setTimeout(() => setLastInput(null), 500)
  }

  // Poll Arduino input via MQTT
  useEffect(() => {
    const timer = setInterval(async () => {
      try {
        const res = await fetch(`${API}/input`)
        const input = await res.json()
        if (input.type === 'direction' && input.value) {
          handleDirection(input.value)
        } else if (input.type === 'action' && input.value) {
          handleAction(input.value)
        }
      } catch (e) {}
    }, 100)
    return () => clearInterval(timer)
  }, [moveIdx, moves, switchFocus, switchIdx]);


  const call = async (url: string, body?: any) => {
    try {
      const res = await fetch(`${API}${url}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const json = await res.json()
      const s = json.state
      setBattle(s)
      updateMove(s)
      setError('')
    } catch (e: any) {
      setError(e.message)
    }
  }

  const start = async () => { setLoading(true); await call('/start', { teamSize: 6, movesPerPokemon: 4 }); setLoading(false) }
  const turn = (type: string, idx?: number) => { setLoading(true); call('/turn', { battleId: battle?.battleId, type, moveName: move, switchIndex: idx }).finally(() => setLoading(false)) }

  const Card = ({ pk, isBot }: { pk?: Poke; isBot?: boolean }) => (
    <div className="p-3 rounded border bg-white dark:bg-gray-900">
      <div className="font-semibold mb-1">{isBot ? 'Bot' : 'Player'}</div>
      <div className="capitalize">{pk?.name || '-'}</div>
      <div className="text-sm">HP: {pk?.currentHp || 0}/{pk?.maxHp || 0}</div>
      {pk?.sprites?.front_default && <img src={pk.sprites.front_default} alt={pk.name} className="w-20 h-20 mt-2" />}
    </div>
  )

  return (
    <div className="w-full max-w-2xl bg-white dark:bg-gray-800 p-4 rounded shadow">
      <div className="flex gap-2 mb-4">
        <button onClick={start} disabled={loading} className="px-4 py-2 bg-blue-600 text-white rounded disabled:opacity-50">{loading && !battle ? 'Start...' : 'Start'}</button>
        <button onClick={() => turn('move')} disabled={!move || !battle || loading || !!battle?.winner} className="px-4 py-2 bg-orange-600 text-white rounded disabled:opacity-50">Attack</button>
        <button
          onClick={() => {
            setSwitchFocus(true)
            setSwitchIdx(switchIdx !== null ? switchIdx : 0)
          }}
          disabled={!battle || loading || !!battle?.winner || switchFocus}
          className={`px-4 py-2 bg-green-600 text-white rounded disabled:opacity-50 ${switchFocus ? 'ring-2 ring-green-400' : ''}`}
        >
          Switch
        </button>
        <button onClick={() => { setBattle(null); setMove(null); setError('') }} className="px-3 py-2 border rounded">Clear</button>
        
      </div>
      {error && <div className="mb-4 text-red-600 text-sm">Error: {error}</div>}
      {battle ? (
        <div className="space-y-4">
          <div className="text-sm">Battle {battle.battleId} • Turn {battle.turn}{battle.winner && ` • Winner: ${battle.winner}`}</div>
          <div className="grid grid-cols-2 gap-4"><Card pk={p} /><Card pk={b} isBot /></div>
          <div>
            <label className="block text-sm font-medium mb-1">Move (Use Arrow Keys or Arduino)</label>
            <div className="grid grid-cols-2 gap-2">
              {p?.moveset?.map((m, idx) => (
                <button
                  key={m.name}
                  onClick={() => setMove(m.name)}
                  disabled={!!battle.winner}
                  className={`p-2 rounded border text-left text-sm transition ${(!switchFocus && move === m.name) ? 'border-blue-600 bg-blue-50 dark:bg-blue-900 ring-2 ring-blue-400' : 'border-gray-300'} disabled:opacity-50`}
                >
                  <div className="font-semibold capitalize">{m.name}</div>
                  <div className="text-xs">({m.damage_class || '?'})</div>
                </button>
              ))}
            </div>
          </div>
          <div>
            <div className="text-sm font-semibold mb-2">Team</div>
            <div className="grid grid-cols-6 gap-2">
              {battle.player.team?.map((pk, i) => {
                const isActive = i === battle.player.activeIndex
                const disabled = isActive || pk.fainted || !!battle.winner || loading
                const isSwitchSelected = switchFocus && switchIdx === i
                return (
                  <button
                    key={`${pk.id}-${i}`}
                    disabled={disabled}
                    onClick={() => {
                      turn('switch', i)
                      setSwitchFocus(false)
                      setSwitchIdx(null)
                    }}
                    className={`p-2 rounded border text-center text-xs ${isActive ? 'border-blue-500' : 'border-gray-300'} ${pk.fainted ? 'opacity-40 grayscale' : ''} ${isSwitchSelected ? 'ring-2 ring-green-400' : ''}`}
                    onMouseEnter={() => setSwitchIdx(i)}
                  >
                    {pk.sprites?.front_default ? <img src={pk.sprites.front_default} alt={pk.name} className="w-12 h-12 mx-auto" /> : <div className="w-12 h-12 mx-auto rounded bg-gray-200" />}
                    <div className="truncate">{pk.name}</div>
                    <div className="text-[10px]">{pk.currentHp}/{pk.maxHp}</div>
                  </button>
                )
              })}
            </div>
          </div>
          <div>
            <div className="text-sm font-semibold mb-1">Log</div>
            <div className="max-h-48 overflow-auto p-3 rounded bg-gray-100 text-xs dark:bg-gray-900">
              {battle.log.length ? <ul className="list-disc pl-4 space-y-0.5">{battle.log.map((e, i) => <li key={i}>{e}</li>)}</ul> : <div className="text-gray-500">No events.</div>}
            </div>
          </div>
        </div>
      ) : (
        <div className="text-sm text-gray-500">Click Start to begin.</div>
      )}
    </div>
  )
}
