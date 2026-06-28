export default function ProPage() {
  return (
    <main className="min-h-screen bg-[#C8A45C] px-6 py-24 text-black md:px-16">
      <h1 className="text-5xl font-black">Commandes professionnelles</h1>
      <p className="mt-6 max-w-2xl text-lg">
        Casquettes personnalisées pour entreprises, restaurants, clubs, associations,
        événements et marques.
      </p>
      <div className="mt-10 grid gap-6 md:grid-cols-4">
        {['20 pièces', '50 pièces', '100 pièces', '500+ pièces'].map((item) => (
          <div key={item} className="rounded-3xl bg-black p-6 text-white">
            <h2 className="text-2xl font-bold">{item}</h2>
            <p className="mt-3 text-white/60">Tarif dégressif disponible.</p>
          </div>
        ))}
      </div>
      <button className="mt-10 rounded-full bg-black px-8 py-4 font-bold text-white">
        Demander un devis
      </button>
    </main>
  );
}
