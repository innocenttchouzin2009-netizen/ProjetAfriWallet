import { recentOrders } from '../data/account.data';

export default function OrderHistory() {
  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Commandes</p>
          <h2 className="mt-2 text-2xl font-black">Historique</h2>
        </div>
        <button className="rounded-full border border-white/15 px-4 py-2 text-sm text-white/70">
          Voir tout
        </button>
      </div>

      <div className="mt-6 space-y-4">
        {recentOrders.map((order) => (
          <article
            key={order.id}
            className="rounded-[24px] border border-white/10 bg-black/20 p-5"
          >
            <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
              <div>
                <p className="text-sm font-semibold text-[#C8A45C]">{order.id}</p>
                <p className="mt-1 text-white/80">{order.date}</p>
              </div>
              <span className="rounded-full border border-[#C8A45C]/40 bg-[#C8A45C]/10 px-3 py-1 text-sm text-[#F5E0AC]">
                {order.status}
              </span>
            </div>

            <div className="mt-4 flex flex-col gap-2 text-sm text-white/70">
              {order.items.map((item) => (
                <p key={item}>• {item}</p>
              ))}
            </div>

            <div className="mt-4 flex items-center justify-between border-t border-white/10 pt-4 text-sm">
              <span>Total</span>
              <span className="font-semibold text-white">{order.total}</span>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
