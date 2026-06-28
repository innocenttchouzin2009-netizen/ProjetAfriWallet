import { StudioDesign } from "../types/studio.types";

export class StudioService {
  static calculatePrice(design: StudioDesign): number {
    let price = design.basePrice;
    if (design.text.trim()) price += 8;
    if (design.logo.trim()) price += 12;
    if (design.embroideryType === "3d") price += 10;
    if (design.embroideryType === "patch") price += 15;
    if (design.embroideryType === "dtf") price += 6;
    const subtotal = price * design.quantity;
    if (design.quantity >= 50) return subtotal * 0.8;
    if (design.quantity >= 20) return subtotal * 0.9;
    if (design.quantity >= 10) return subtotal * 0.95;
    return subtotal;
  }
}
