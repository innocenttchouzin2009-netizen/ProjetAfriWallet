"use client";

import dynamic from 'next/dynamic';
import { useStudioDesigner } from "@/features/studio/hooks/useStudioDesigner";

const StudioCanvas = dynamic(() => import('@/features/studio/components/StudioCanvas'));
const StudioControls = dynamic(() => import('@/features/studio/components/StudioControls'));
const StudioSummary = dynamic(() => import('@/features/studio/components/StudioSummary'));

export default function StudioPage() {
  const { design, updateDesign, totalPrice, resetDesign, saveDesign } = useStudioDesigner();

  return (
    <main className="min-h-screen bg-white px-6 py-28 text-black md:px-16">
      <p className="text-sm uppercase tracking-[0.4em] text-[#C8A45C]">
        D&C Studio
      </p>
      <h1 className="mt-4 text-5xl font-black">
        Personnalise ta casquette
      </h1>
      <p className="mt-6 max-w-2xl text-black/60">
        Crée ton design avec texte, logo, broderie, couleur et placement.
      </p>
      <div className="mt-12 grid gap-8 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <StudioCanvas design={design} />
        </div>
        <div className="space-y-8">
          <StudioControls design={design} updateDesign={updateDesign} />
          <StudioSummary
            design={design}
            totalPrice={totalPrice}
            resetDesign={resetDesign}
            saveDesign={saveDesign}
          />
        </div>
      </div>
    </main>
  );
}
