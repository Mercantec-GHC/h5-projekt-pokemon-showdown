"use client"

import { useState } from 'react'

const GAME_API_BASE = process.env.NEXT_PUBLIC_GAME_API_BASE ?? 'http://localhost:5108/game'

type Move = {
  name: string
  level: number | null
  damage_class: 'physical' | 'special' | 'status' | null
  power: number | null
  type: string | null
  accuracy?: number | null
  priority?: number | null
  target?: string | null
  secondary_chance?: number | null
  stat_changes?: { stat: string; stages: number }[] | null
  status?: { name: string; chance: number } | null
}

type BattlePokemon = {
  id: number
  name: string
  currentHp: number
  maxHp: number
  fainted: boolean
  status?: string | null
  attackStage?: number
  defenseStage?: number
  specialAttackStage?: number
  specialDefenseStage?: number
  speedStage?: number
  sprites?: {
    front_default?: string | null
    back_default?: string | null
  }
  moveset: Move[]
}

type BattleState = {
  battleId: string
  turn: number
  winner: 'player' | 'bot' | null
  log: string[]
  player: {
    activeIndex: number
    team: BattlePokemon[]
  }
  bot: {
    activeIndex: number
    team: BattlePokemon[]
  }
}

export default function GameFetcher() {
  const [battle, setBattle] = useState<BattleState | null>(null)
  const [battleId, setBattleId] = useState<string | null>(null)
  const [selectedMove, setSelectedMove] = useState<string | null>(null)
  const [starting, setStarting] = useState(false)
  const [acting, setActing] = useState(false)
  const [loadingState, setLoadingState] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const activePlayer = battle ? battle.player.team[battle.player.activeIndex] : null
  const activeBot = battle ? battle.bot.team[battle.bot.activeIndex] : null

  async function startBattle() {
    setStarting(true)
    setError(null)
    try {
      const res = await fetch(`${GAME_API_BASE}/start`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ teamSize: 6, movesPerPokemon: 4 })
      })

      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const json = await res.json()

      setBattleId(json.battleId)
      setBattle(json.state)
      setSelectedMove(json.state?.player?.team?.[0]?.moveset?.[0]?.name ?? null)
    } catch (err: any) {
      setError(err?.message ?? String(err))
      setBattle(null)
      setBattleId(null)
    } finally {
      setStarting(false)
    }
  }

  async function fetchBattleState() {
    if (!battleId) return

    setLoadingState(true)
    setError(null)
    try {
      const res = await fetch(`${GAME_API_BASE}/state?battleId=${encodeURIComponent(battleId)}`)
      if (!res.ok) throw new Error(`HTTP ${res.status}`)

      const json = await res.json()
      setBattle(json.state)
      setSelectedMove((prev) => {
        const active = json.state?.player?.team?.[json.state?.player?.activeIndex]
        if (!active?.moveset?.length) return null
        if (prev && active.moveset.some((move: Move) => move.name === prev)) return prev
        return active.moveset[0].name
      })
    } catch (err: any) {
      setError(err?.message ?? String(err))
    } finally {
      setLoadingState(false)
    }
  }

  async function playTurn() {
    if (!battleId || !selectedMove || battle?.winner) return

    setActing(true)
    setError(null)
    try {
      const res = await fetch(`${GAME_API_BASE}/turn`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ battleId, type: 'move', moveName: selectedMove })
      })

      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const json = await res.json()

      setBattle(json.state)
      setSelectedMove((prev) => {
        const active = json.state?.player?.team?.[json.state?.player?.activeIndex]
        if (!active?.moveset?.length) return null
        if (prev && active.moveset.some((move: Move) => move.name === prev)) return prev
        return active.moveset[0].name
      })
    } catch (err: any) {
      setError(err?.message ?? String(err))
    } finally {
      setActing(false)
    }
  }

  async function switchPokemon(targetIndex: number) {
    if (!battleId || battle?.winner) return

    setActing(true)
    setError(null)
    try {
      const res = await fetch(`${GAME_API_BASE}/turn`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ battleId, type: 'switch', switchIndex: targetIndex })
      })

      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const json = await res.json()

      setBattle(json.state)
      setSelectedMove((prev) => {
        const active = json.state?.player?.team?.[json.state?.player?.activeIndex]
        if (!active?.moveset?.length) return null
        if (prev && active.moveset.some((move: Move) => move.name === prev)) return prev
        return active.moveset[0].name
      })
    } catch (err: any) {
      setError(err?.message ?? String(err))
    } finally {
      setActing(false)
    }
  }

  return (
    <div className="w-full max-w-2xl bg-white dark:bg-gray-800 p-4 rounded shadow">
      <div className="flex gap-3 mb-4 bg-gray-50 dark:bg-gray-700 p-3 rounded">
        <button
          onClick={startBattle}
          disabled={starting}
          className="px-4 py-2 bg-blue-600 text-white rounded disabled:opacity-50"
        >
          {starting ? 'Starting…' : 'Start Battle'}
        </button>
        <button
          onClick={playTurn}
          disabled={acting || !battleId || !selectedMove || Boolean(battle?.winner)}
          className="px-4 py-2 bg-orange-600 text-white rounded disabled:opacity-50"
        >
          {acting ? 'Resolving…' : 'Play Turn'}
        </button>
        <button
          onClick={fetchBattleState}
          disabled={loadingState || !battleId}
          className="px-3 py-2 border rounded"
        >
          {loadingState ? 'Refreshing…' : 'Refresh State'}
        </button>
        <button
          onClick={() => {
            setBattle(null)
            setBattleId(null)
            setSelectedMove(null)
            setError(null)
          }}
          className="px-3 py-2 border rounded"
        >
          Clear
        </button>
      </div>

      {error && (
        <div className="mb-4 text-red-600">Error: {error}</div>
      )}

      {battle ? (
        <div className="space-y-4">
          <div className="text-sm text-gray-700 dark:text-gray-200">
            Battle ID: {battle.battleId} | Turn: {battle.turn}
            {battle.winner ? ` | Winner: ${battle.winner}` : ''}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-3 rounded border bg-white dark:bg-gray-900">
                <div className="font-semibold mb-1">Player Active</div>
                <div className="capitalize">{activePlayer?.name ?? 'None'}</div>
                <div className="text-sm">HP: {activePlayer?.currentHp ?? 0}/{activePlayer?.maxHp ?? 0}</div>
                {activePlayer?.sprites?.front_default ? (
                  <img src={activePlayer.sprites.front_default} alt={activePlayer.name} className="w-20 h-20 mt-2" />
                ) : null}
              </div>

            <div className="p-3 rounded border bg-white dark:bg-gray-900">
              <div className="font-semibold mb-1">Bot Active</div>
              <div className="capitalize">{activeBot?.name ?? 'None'}</div>
              <div className="text-sm">HP: {activeBot?.currentHp ?? 0}/{activeBot?.maxHp ?? 0}</div>
              {activeBot?.sprites?.front_default ? (
                <img src={activeBot.sprites.front_default} alt={activeBot.name} className="w-20 h-20 mt-2" />
              ) : null}
            </div>
          </div>

          <div>
            <label className="block mb-1 font-medium">Choose move</label>
            <div className="grid grid-cols-2 gap-2">
              {(activePlayer?.moveset ?? []).slice(0, 4).map((move) => (
                <button
                  key={move.name}
                  type="button"
                  className={`p-2 rounded border text-left ${selectedMove === move.name ? 'border-blue-600 bg-blue-50 dark:bg-blue-900' : 'border-gray-300 bg-white dark:bg-gray-800'} disabled:opacity-50`}
                  onClick={() => setSelectedMove(move.name)}
                  disabled={Boolean(battle.winner)}
                >
                  <span className="font-semibold capitalize">{move.name}</span>
                  <span className="ml-2 text-xs text-gray-600 dark:text-gray-300">({move.damage_class ?? 'unknown'})</span>
                </button>
              ))}
            </div>
          </div>

          <div>
            <div className="font-semibold mb-2">Switch Pokemon</div>
            <div className="grid grid-cols-3 sm:grid-cols-6 gap-2">
              {(battle.player.team ?? []).map((pokemon, index) => {
                const isActive = index === battle.player.activeIndex
                const isFainted = pokemon.fainted
                const disabled = isActive || isFainted || Boolean(battle.winner) || acting

                return (
                  <button
                    key={`${pokemon.id}-${index}`}
                    type="button"
                    disabled={disabled}
                    onClick={() => switchPokemon(index)}
                    className={`p-2 rounded border text-center ${isActive ? 'border-blue-500' : 'border-gray-300'} ${isFainted ? 'opacity-40 grayscale' : ''} disabled:cursor-not-allowed`}
                    title={isFainted ? `${pokemon.name} is fainted` : `Switch to ${pokemon.name}`}
                  >
                    {pokemon.sprites?.front_default ? (
                      <img
                        src={pokemon.sprites.front_default}
                        alt={pokemon.name}
                        className="w-12 h-12 mx-auto"
                      />
                    ) : (
                      <div className="w-12 h-12 mx-auto rounded bg-gray-200" />
                    )}
                    <div className="text-xs mt-1 capitalize truncate">{pokemon.name}</div>
                    <div className="text-[10px]">{pokemon.currentHp}/{pokemon.maxHp}</div>
                  </button>
                )
              })}
            </div>
          </div>

          <div>
            <div className="font-semibold mb-1">Battle Log</div>
            <div className="max-h-60 overflow-auto p-3 rounded bg-gray-100 text-sm dark:bg-gray-900">
              {battle.log.length === 0 ? (
                <div className="text-gray-500">No events yet.</div>
              ) : (
                <ul className="list-disc pl-5 space-y-1">
                  {battle.log.map((entry, index) => (
                    <li key={`${entry}-${index}`}>{entry}</li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </div>
      ) : (
        <div className="text-sm text-gray-500">No battle yet. Click Start Battle.</div>
      )}
    </div>
  )
}
