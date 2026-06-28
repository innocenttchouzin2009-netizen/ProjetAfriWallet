"use client";

import { useMemo, useState } from "react";
import { initialStudioDesign } from "../data/studio.data";
import { StudioDesign } from "../types/studio.types";
import { StudioService } from "../services/studio.service";

export function useStudioDesigner() {
  const [design, setDesign] = useState<StudioDesign>(initialStudioDesign);

  function updateDesign<T extends keyof StudioDesign>(
    key: T,
    value: StudioDesign[T]
  ) {
    setDesign((current) => ({
      ...current,
      [key]: value,
    }));
  }

  const totalPrice = useMemo(() => {
    return StudioService.calculatePrice(design);
  }, [design]);

  function resetDesign() {
    setDesign(initialStudioDesign);
  }

  function saveDesign() {
    if (typeof window !== "undefined") {
      localStorage.setItem("studioDesign", JSON.stringify(design));
    }
  }

  return {
    design,
    updateDesign,
    totalPrice,
    resetDesign,
    saveDesign,
  };
}
