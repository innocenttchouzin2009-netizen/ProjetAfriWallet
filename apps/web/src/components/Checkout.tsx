import Button from './ui/Button';

export default function Checkout() {
  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-8">
      <h2 className="text-2xl font-black">Paiement</h2>
      <p className="mt-4 text-white/70">Valide tes informations et passe ta commande.</p>
      <div className="mt-6 space-y-4">
        <div className="rounded-3xl border border-white/10 bg-[#0D0D0D] p-4">Détails du paiement</div>
        <Button className="w-full">Payer maintenant</Button>
      </div>
    </section>
  );
}
