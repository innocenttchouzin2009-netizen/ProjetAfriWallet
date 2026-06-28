import { EmbroideryType, StudioPlacement } from "../types/studio.types";

export const capColors = ["Black", "White", "Beige", "Navy", "Camo"];
export const placements: StudioPlacement[] = ["front", "left", "right", "back"];
export const embroideryTypes: EmbroideryType[] = ["flat", "3d", "patch", "dtf"];

export const initialStudioDesign = {
  productName: "D&C Signature Black",
  color: "Black",
  text: "D&C",
  logo: "D&C",
  placement: "front",
  embroideryType: "flat",
  quantity: 1,
  basePrice: 49.9,
  logoSize: 60,
  x: 50,
  y: 50,
} as const;
