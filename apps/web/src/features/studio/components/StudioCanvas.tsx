import { StudioDesign } from "../types/studio.types";

type Props = {
  design: StudioDesign;
};

const placementStyles: Record<StudioDesign["placement"], string> = {
  front: "",
  left: "-rotate-3",
  right: "rotate-3",
  back: "scale-95",
};

export default function StudioCanvas({ design }: Props) {
  return (
    <div className="rounded-3xl bg-[#0D0D0D] p-8 text-white">
      <div className="flex h-[420px] items-center justify-center rounded-3xl bg-gradient-to-br from-neutral-900 to-neutral-700">
        <div className="relative mx-auto h-[320px] w-[320px] rounded-[32px] bg-black/80 p-6 shadow-[inset_0_0_40px_rgba(0,0,0,0.4)]">
          <div className="absolute left-4 top-4 rounded-full bg-white/10 px-3 py-1 text-xs uppercase tracking-[0.3em] text-white/60">
            {design.placement}
          </div>
          <div className="absolute inset-0 flex items-center justify-center">
            <div
              className={`relative h-[220px] w-[220px] rounded-[24px] border border-white/10 bg-white/5 ${placementStyles[design.placement]}`}
              style={{ transform: `${design.placement === "back" ? "scale(0.95)" : design.placement === "left" ? "rotate(-3deg)" : design.placement === "right" ? "rotate(3deg)" : "none"}` }}
            >
              <div
                className="absolute flex items-center justify-center rounded-full border border-white/10 bg-[#C8A45C]/20 text-center text-[0.8rem] font-black text-white"
                style={{
                  left: `${design.x}%`,
                  top: `${design.y}%`,
                  width: `${design.logoSize}px`,
                  height: `${design.logoSize}px`,
                  transform: "translate(-50%, -50%)",
                }}
              >
                <span className="px-2 break-words">
                  {design.logo || design.text || "D&C"}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
