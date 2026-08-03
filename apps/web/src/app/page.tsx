export default function Home() {
  return (
    <main className="min-h-screen bg-[#0D0D0D] text-white">
      <header className="fixed top-0 z-50 flex w-full items-center justify-between border-b border-white/10 bg-black/60 px-6 py-4 backdrop-blur-md">
        <div>
          <h1 className="text-xl font-bold tracking-[0.25em]">DOPE&CUTE</h1>
          <p className="text-right text-[10px] uppercase tracking-[0.5em] text-[#C8A45C]">
            studio
          </p>
        </div>

        <nav className="hidden gap-8 text-sm text-white/70 md:flex">
          <a href="#collections">Collections</a>
          <a href="#studio">Studio</a>
          <a href="#products">Produits</a>
          <a href="#pro">Professionnels</a>
        </nav>

        <button className="rounded-full bg-[#C8A45C] px-5 py-2 text-sm font-semibold text-black">
          Panier
        </button>
      </header>

      <section className="flex min-h-screen items-center px-6 pt-28 md:px-16">
        <div className="max-w-4xl">
          <p className="mb-6 text-sm uppercase tracking-[0.5em] text-[#C8A45C]">
            Design Your Identity
          </p>

          <h2 className="text-5xl font-black leading-tight md:text-8xl">
            Crée ta casquette.
            <br />
            Porte ton identité.
          </h2>

          <p className="mt-8 max-w-2xl text-lg text-white/70">
            Dope&Cute Studio crée des casquettes premium personnalisées :
            broderie, logo, prénom, design, patch et collections exclusives.
          </p>

          <div className="mt-10 flex flex-col gap-4 sm:flex-row">
            <a
              href="#studio"
              className="rounded-full bg-[#C8A45C] px-8 py-4 text-center font-bold text-black"
            >
              Personnaliser maintenant
            </a>
            <a
              href="#products"
              className="rounded-full border border-white/20 px-8 py-4 text-center font-bold"
            >
              Voir la collection
            </a>
            <a
              href="/shop"
              className="rounded-full border border-[#C8A45C] bg-[#C8A45C]/10 px-8 py-4 text-center font-bold text-[#C8A45C]"
            >
              City Collections
            </a>
          </div>
        </div>
      </section>

      <section id="collections" className="px-6 py-24 md:px-16">
        <h3 className="text-3xl font-bold">Collections</h3>

        <div className="mt-10 grid gap-6 md:grid-cols-3">
          {[
            "Baseball Cap",
            "Snapback Cap",
            "Trucker Hat",
            "Washed Cap",
            "Camouflage Hat",
            "Performance Cap",
          ].map((item) => (
            <div
              key={item}
              className="rounded-3xl border border-white/10 bg-white/5 p-8 transition hover:bg-white/10"
            >
              <p className="text-2xl font-bold">{item}</p>
              <p className="mt-3 text-white/60">
                Modèle premium personnalisable avec broderie, logo ou prénom.
              </p>
            </div>
          ))}
        </div>
      </section>

      <section id="studio" className="bg-white px-6 py-24 text-black md:px-16">
        <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">
          D&C Studio
        </p>
        <h3 className="mt-4 text-4xl font-black md:text-6xl">
          Ton design. Notre broderie.
        </h3>
        <p className="mt-6 max-w-2xl text-lg text-black/60">
          Choisis ton modèle, ajoute ton logo, ton prénom ou ton design, puis
          visualise ta création avant de commander.
        </p>

        <div className="mt-10 grid gap-6 md:grid-cols-4">
          {["Logo", "Prénom", "Broderie 3D", "Patch"].map((item) => (
            <div key={item} className="rounded-3xl bg-black p-6 text-white">
              <p className="text-xl font-bold">{item}</p>
            </div>
          ))}
        </div>
      </section>

      <section id="products" className="px-6 py-24 md:px-16">
        <h3 className="text-3xl font-bold">Produits populaires</h3>

        <div className="mt-10 grid gap-6 md:grid-cols-3">
          {[
            ["D&C Signature Black", "49,90 €"],
            ["D&C Urban Snapback", "44,90 €"],
            ["D&C Camo Edition", "54,90 €"],
          ].map(([name, price]) => (
            <div
              key={name}
              className="rounded-3xl border border-white/10 bg-white/5 p-6"
            >
              <div className="mb-6 flex h-64 items-center justify-center rounded-2xl bg-gradient-to-br from-white/20 to-white/5">
                <span className="text-5xl font-black text-white/30">D&C</span>
              </div>
              <h4 className="text-xl font-bold">{name}</h4>
              <p className="mt-2 text-[#C8A45C]">{price}</p>
              <button className="mt-6 w-full rounded-full bg-white px-5 py-3 font-bold text-black">
                Ajouter au panier
              </button>
            </div>
          ))}
        </div>
      </section>

      <section id="pro" className="bg-[#C8A45C] px-6 py-24 text-black md:px-16">
        <h3 className="text-4xl font-black">Commandes professionnelles</h3>
        <p className="mt-6 max-w-2xl text-lg">
          Restaurants, entreprises, clubs, associations et événements : créez
          vos casquettes personnalisées en série avec devis professionnel.
        </p>
        <button className="mt-8 rounded-full bg-black px-8 py-4 font-bold text-white">
          Demander un devis
        </button>
      </section>

      <footer className="px-6 py-10 text-center text-white/50">
        © 2026 DOPE&CUTE studio — Design Your Identity
      </footer>
    </main>
  );
}
