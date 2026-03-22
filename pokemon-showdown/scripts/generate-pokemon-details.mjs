import { readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'

const args = process.argv.slice(2)

const getArgValue = (name, fallback) => {
  const index = args.indexOf(name)
  if (index === -1 || index === args.length - 1) return fallback
  return args[index + 1]
}

const hasFlag = (name) => args.includes(name)

const concurrency = Number.parseInt(getArgValue('--concurrency', '12'), 10)
const limitArg = Number.parseInt(getArgValue('--limit', '0'), 10)
const useResults = !hasFlag('--raw')

const inputPath = path.join(process.cwd(), 'public', 'pokemon-list.json')
const outputPath = path.join(process.cwd(), 'public', 'pokemon-details.json')
const moveMetaCache = new Map()
const spriteBaseUrl = 'https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon'

const readPokemonNames = async () => {
  const file = await readFile(inputPath, 'utf8')
  const parsed = JSON.parse(file)

  if (useResults) {
    if (!Array.isArray(parsed.results)) {
      throw new Error('Expected pokemon-list.json to contain a results array')
    }

    return parsed.results.map((entry) => entry.name)
  }

  if (!Array.isArray(parsed)) {
    throw new Error('Expected pokemon-list.json to be an array when using --raw')
  }

  return parsed
}

const getMoveMeta = async (moveName, moveUrl) => {
  const cached = moveMetaCache.get(moveName)
  if (cached) {
    return cached
  }

  const promise = (async () => {
    const targetUrl = moveUrl ?? `https://pokeapi.co/api/v2/move/${encodeURIComponent(moveName)}`
    const res = await fetch(targetUrl)

    if (!res.ok) {
      throw new Error(`Failed move lookup for ${moveName} (${res.status})`)
    }

    const data = await res.json()

    return {
      damageClass: data.damage_class?.name ?? null,
      power: data.power,
      type: data.type?.name ?? null
    }
  })()

  moveMetaCache.set(moveName, promise)
  return promise
}

const getLevelUpMoves = (pokemon) => {
  const moveMap = new Map()

  for (const moveEntry of pokemon.moves) {
    const levelUpDetails = moveEntry.version_group_details.filter(
      (details) => details.move_learn_method.name === 'level-up'
    )

    if (levelUpDetails.length === 0) {
      continue
    }

    const level = Math.min(...levelUpDetails.map((details) => details.level_learned_at))
    const existing = moveMap.get(moveEntry.move.name)

    if (!existing || level < existing.level) {
      moveMap.set(moveEntry.move.name, {
        name: moveEntry.move.name,
        url: moveEntry.move.url,
        level
      })
    }
  }

  return Array.from(moveMap.values()).sort((a, b) => a.level - b.level || a.name.localeCompare(b.name))
}

const splitMovesByDamage = async (moves) => {
  const damaging = []
  const status = []

  for (const move of moves) {
    const meta = await getMoveMeta(move.name, move.url)
    const enrichedMove = {
      name: move.name,
      level: move.level,
      damage_class: meta.damageClass,
      power: meta.power,
      type: meta.type
    }

    if (meta.damageClass === 'status') {
      status.push(enrichedMove)
      continue
    }

    damaging.push(enrichedMove)
  }

  return { damaging, status }
}

const findFallbackDamagingMoves = async (pokemon) => {
  const moves = []

  for (const moveEntry of pokemon.moves) {
    const meta = await getMoveMeta(moveEntry.move.name, moveEntry.move.url)
    if (meta.damageClass !== 'status') {
      moves.push({
        name: moveEntry.move.name,
        level: null,
        damage_class: meta.damageClass,
        power: meta.power,
        type: meta.type,
        fallback: true
      })
    }
  }

  const unique = new Map()
  for (const move of moves) {
    if (!unique.has(move.name)) {
      unique.set(move.name, move)
    }
  }

  return Array.from(unique.values())
}

const mapPokemon = async (pokemon) => {
  const statMap = Object.fromEntries(
    pokemon.stats.map((s) => [s.stat.name, s.base_stat])
  )

  const levelUpMoves = getLevelUpMoves(pokemon)
  const { damaging: damagingLevelUpMoves, status: statusLevelUpMoves } = await splitMovesByDamage(levelUpMoves)
  const fallbackDamagingMoves = damagingLevelUpMoves.length > 0 ? [] : await findFallbackDamagingMoves(pokemon)

  return {
    id: pokemon.id,
    name: pokemon.name,
    types: pokemon.types
      .slice()
      .sort((a, b) => a.slot - b.slot)
      .map((t) => t.type.name),
    abilities: pokemon.abilities.map((a) => ({
      name: a.ability.name,
      hidden: a.is_hidden,
      slot: a.slot
    })),
    sprites: {
      front_default: pokemon.sprites?.front_default ?? `${spriteBaseUrl}/${pokemon.id}.png`,
      back_default: pokemon.sprites?.back_default ?? `${spriteBaseUrl}/back/${pokemon.id}.png`
    },
    stats: {
      hp: statMap.hp ?? 0,
      attack: statMap.attack ?? 0,
      defense: statMap.defense ?? 0,
      special_attack: statMap['special-attack'] ?? 0,
      special_defense: statMap['special-defense'] ?? 0,
      speed: statMap.speed ?? 0
    },
    level_up_moves: levelUpMoves.map((move) => move.name),
    attacks: damagingLevelUpMoves.map((move) => move.name),
    level_up_damaging_moves: damagingLevelUpMoves,
    level_up_status_moves: statusLevelUpMoves,
    fallback_attacks: fallbackDamagingMoves,
    items: pokemon.held_items.map((item) => item.item.name)
  }
}

const fetchPokemonDetails = async (name) => {
  const res = await fetch(`https://pokeapi.co/api/v2/pokemon/${encodeURIComponent(name)}`)

  if (!res.ok) {
    throw new Error(`Failed for ${name} (${res.status})`)
  }

  const data = await res.json()
  return mapPokemon(data)
}

const runWithConcurrency = async (names) => {
  const results = new Array(names.length)
  let cursor = 0

  const worker = async () => {
    while (true) {
      const current = cursor
      cursor += 1

      if (current >= names.length) {
        return
      }

      const name = names[current]
      results[current] = await fetchPokemonDetails(name)

      if ((current + 1) % 50 === 0 || current === names.length - 1) {
        console.log(`Fetched ${current + 1}/${names.length}`)
      }
    }
  }

  const workers = Array.from({ length: Math.max(1, concurrency) }, () => worker())
  await Promise.all(workers)

  return results
}

const main = async () => {
  const names = await readPokemonNames()
  const targetNames = limitArg > 0 ? names.slice(0, limitArg) : names

  console.log(`Preparing to fetch details for ${targetNames.length} pokemon`)

  const details = await runWithConcurrency(targetNames)

  await writeFile(
    outputPath,
    JSON.stringify(
      {
        generatedAt: new Date().toISOString(),
        source: 'https://pokeapi.co/api/v2/pokemon/{name}',
        count: details.length,
        pokemon: details
      },
      null,
      2
    ),
    'utf8'
  )

  console.log(`Saved ${details.length} entries to public/pokemon-details.json`)
}

main().catch((error) => {
  console.error(error)
  process.exit(1)
})