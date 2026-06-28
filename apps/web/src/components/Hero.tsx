import Link from 'next/link';

export default function Hero() {
  return (
    <section className="relative overflow-hidden rounded-[32px] bg-gradient-to-br from-[#0D0D0D] via-[#111827] to-[#0b1220] px-6 py-20 text-white md:px-16">
      <div className="mx-auto max-w-6xl">
        <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">Dope&Cute Studio</p>
        <h1 className="mt-6 text-5xl font-black leading-tight">Des casquettes premium, stylées et personnalisables.</h1>
        <p className="mt-6 max-w-2xl text-lg text-white/70">
          Explore notre collection de chapeaux uniques et crée un style qui te ressemble.
        </p>
        <div className="mt-10 flex flex-wrap gap-4">
          <Link href="/shop" className="rounded-full bg-[#C8A45C] px-8 py-4 text-sm font-bold text-black transition hover:bg-[#bfa760]">
            Voir la boutique
          </Link>
          <Link href="/studio" className="rounded-full border border-white/10 px-8 py-4 text-sm font-semibold text-white transition hover:border-white/30">
            Personnaliser
          </Link>
        </div>
      </div>
    </section>
  );
}
