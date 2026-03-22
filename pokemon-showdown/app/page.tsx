import Image from "next/image";
import GameFetcher from './components/GameFetcher'

export default function Home() {
  return (
    <div className="flex flex-col flex-1 items-center justify-center bg-zinc-50 font-sans dark:bg-black">
      <main className="flex flex-1 w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white dark:bg-black sm:items-start">
      <div className="d-flex justify-content-center">

      <p className="font-bold text-4xl">Pokemon Showdown</p>
      </div>
        <section className="w-full mt-8 bg-gray-100 text-black rounded-lg p-6">
          <GameFetcher />
        </section>
      </main>
    </div>
  );
}
