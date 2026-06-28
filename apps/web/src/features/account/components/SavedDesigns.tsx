import { savedDesigns } from '../data/account.data';

export default function SavedDesigns() {
  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Créations</p>
          <h2 className="mt-2 text-2xl font-black">Sauvegardées</h2>
        </div>
        <button className="rounded-full border border-white/15 px-4 py-2 text-sm text-white/70">
          Nouveau design
        </button>
      </div>

      <div className="mt-6 grid gap-4 lg:grid-cols-3">
        {savedDesigns.map((design) => (
          <article
            key={design.id}
            className="rounded-[24px] border border-white/10 bg-black/20 p-5"
          >
            <div className={`h-24 rounded-[20px] bg-gradient-to-br ${design.accent}`} />
            <h3 className="mt-4 text-lg font-semibold">{design.title}</h3>
            <p className="mt-2 text-sm text-white/70">{design.description}</p>
            <div className="mt-4 flex items-center justify-between text-sm text-white/50">
              <span>{design.updatedAt}</span>
              <button className="text-[#C8A45C]">Ouvrir</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
