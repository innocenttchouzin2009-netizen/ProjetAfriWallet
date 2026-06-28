export default function ContactPage() {
  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-24 text-white md:px-16">
      <h1 className="text-5xl font-black">Contact</h1>
      <p className="mt-4 text-white/60">
        Contacte Dope&Cute Studio pour une commande, un devis ou une collaboration.
      </p>
      <form className="mt-10 max-w-2xl rounded-3xl border border-white/10 bg-white/5 p-8">
        {['Nom', 'Email', 'Sujet'].map((item) => (
          <input
            key={item}
            className="mb-5 w-full rounded-xl border border-white/10 bg-black px-4 py-3 text-white"
            placeholder={item}
          />
        ))}
        <textarea
          className="h-40 w-full rounded-xl border border-white/10 bg-black px-4 py-3 text-white"
          placeholder="Message"
        />
        <button className="mt-6 rounded-full bg-[#C8A45C] px-8 py-4 font-bold text-black">
          Envoyer
        </button>
      </form>
    </main>
  );
}
