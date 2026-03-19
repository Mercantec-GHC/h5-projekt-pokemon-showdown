import { NextResponse } from 'next/server'
import { writeFile } from 'fs/promises'
import path from 'path'

const base_URL = 'https://pokeapi.co/api/v2/'

export async function GET(request: Request) {
	const url = new URL(request.url)
	const save = url.searchParams.get('save') === 'true' || url.searchParams.get('download') === 'true'
	const name = url.searchParams.get('name')

	try {
		const target = name
			? `${base_URL}pokemon/${encodeURIComponent(name)}`
			: `${base_URL}pokemon?limit=100000&offset=0`

		const res = await fetch(target)
		if (!res.ok) {
			return NextResponse.json({ error: 'Upstream error' }, { status: res.status })
		}

		const data = await res.json()

		if (save) {
			// write to public/pokemon-list.json in project root
			const outPath = path.join(process.cwd(), 'public', 'pokemon-list.json')
			await writeFile(outPath, JSON.stringify(data, null, 2), 'utf8')
			return NextResponse.json({ saved: true, path: '/pokemon-list.json' })
		}

		return NextResponse.json(data)
	} catch (err) {
		return NextResponse.json({ error: 'Fetch failed', details: String(err) }, { status: 500 })
	}
}