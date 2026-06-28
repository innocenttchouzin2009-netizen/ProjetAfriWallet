import { customerProfile } from '../data/account.data';
import OrderHistory from './OrderHistory';
import SavedDesigns from './SavedDesigns';

export default function AccountDashboard() {
  return (
    <div className="space-y-8">
      <section className="rounded-[36px] border border-white/10 bg-gradient-to-br from-[#C8A45C]/20 via-black/80 to-black p-8">
        <p className="text-sm uppercase tracking-[0.35em] text-[#C8A45C]">Compte client</p>
        <div className="mt-6 flex flex-col gap-8 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <h1 className="text-4xl font-black md:text-5xl">Bienvenue, {customerProfile.name}</h1>
            <p className="mt-4 max-w-2xl text-white/70">
              Voici un aperçu de ton espace client avec tes commandes, tes créations sauvegardées et tes informations de profil.
            </p>
          </div>

          <div className="rounded-[24px] border border-white/10 bg-white/10 px-5 py-4 text-sm text-white/80">
            <p className="text-[#C8A45C]">Abonnement</p>
            <p className="mt-1 text-xl font-semibold">{customerProfile.membership}</p>
          </div>
        </div>
      </section>

      <section className="grid gap-8 lg:grid-cols-[0.8fr_1.2fr]">
        <div className="rounded-[32px] border border-white/10 bg-white/5 p-6">
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Profil</p>
          <h2 className="mt-2 text-2xl font-black">Informations client</h2>

          <div className="mt-6 space-y-4 text-sm text-white/70">
            <div>
              <p className="text-white/40">Nom</p>
              <p className="mt-1 font-semibold text-white">{customerProfile.name}</p>
            </div>
            <div>
              <p className="text-white/40">Email</p>
              <p className="mt-1 font-semibold text-white">{customerProfile.email}</p>
            </div>
            <div>
              <p className="text-white/40">Téléphone</p>
              <p className="mt-1 font-semibold text-white">{customerProfile.phone}</p>
            </div>
            <div>
              <p className="text-white/40">Adresse</p>
              <p className="mt-1 font-semibold text-white">{customerProfile.address}</p>
            </div>
          </div>
        </div>

        <OrderHistory />
      </section>

      <SavedDesigns />
    </div>
  );
}
