export type StudioPlacement = "front" | "left" | "right" | "back";
export type EmbroideryType = "flat" | "3d" | "patch" | "dtf";
export type StudioDesign = {
  productName: string;
  color: string;
  text: string;
  logo: string;
  placement: StudioPlacement;
  embroideryType: EmbroideryType;
  quantity: number;
  basePrice: number;
  logoSize: number;
  x: number;
  y: number;
};
