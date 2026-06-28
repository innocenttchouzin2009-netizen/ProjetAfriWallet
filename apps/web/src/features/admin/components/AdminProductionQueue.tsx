import { productionQueue } from '../data/admin.data';

export default function AdminProductionQueue() {
  return (
    <section className="rounded-[32px] border border-white/10 bg-white/5 p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Production</p>
          <h2 className="mt-2 text-2xl font-black text-white">Queue broderie</h2>
        </div>
        <button className="rounded-full border border-white/10 px-4 py-2 text-sm text-white/70">
          Gérer
        </button>
      </div>

      <div className="mt-6 space-y-3">
        {productionQueue.map((task) => (
          <div key={task.id} className="rounded-[20px] border border-white/10 bg-black/20 p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-semibold text-white">{task.design}</p>
                <p className="text-sm text-white/60">{task.quantity} pièces • {task.dueDate}</p>
              </div>
              <span className="rounded-full border border-[#C8A45C]/40 bg-[#C8A45C]/10 px-3 py-1 text-sm text-[#F5E0AC]">
                {task.priority}
              </span>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
