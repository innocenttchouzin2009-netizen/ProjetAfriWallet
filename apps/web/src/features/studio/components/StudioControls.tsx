import {
  capColors,
  embroideryTypes,
  placements,
} from "../data/studio.data";
import { StudioDesign } from "../types/studio.types";

type Props = {
  design: StudioDesign;
  updateDesign: <T extends keyof StudioDesign>(
    key: T,
    value: StudioDesign[T]
  ) => void;
};

export default function StudioControls({ design, updateDesign }: Props) {
  return (
    <div className="rounded-3xl border border-black/10 bg-white p-8 text-black">
      <h2 className="text-2xl font-black">Configurateur</h2>
      <div className="mt-6 space-y-5">
        <input
          value={design.text}
          onChange={(e) => updateDesign("text", e.target.value)}
          className="w-full rounded-xl border border-black/20 px-4 py-3"
          placeholder="Texte / prénom"
        />
        <input
          value={design.logo}
          onChange={(e) => updateDesign("logo", e.target.value)}
          className="w-full rounded-xl border border-black/20 px-4 py-3"
          placeholder="Logo simulé"
        />
        <select
          value={design.color}
          onChange={(e) => updateDesign("color", e.target.value)}
          className="w-full rounded-xl border border-black/20 px-4 py-3"
        >
          {capColors.map((color) => (
            <option key={color}>{color}</option>
          ))}
        </select>
        <select
          value={design.placement}
          onChange={(e) =>
            updateDesign("placement", e.target.value as StudioDesign["placement"])
          }
          className="w-full rounded-xl border border-black/20 px-4 py-3"
        >
          {placements.map((placement) => (
            <option key={placement}>{placement}</option>
          ))}
        </select>
        <select
          value={design.embroideryType}
          onChange={(e) =>
            updateDesign(
              "embroideryType",
              e.target.value as StudioDesign["embroideryType"]
            )
          }
          className="w-full rounded-xl border border-black/20 px-4 py-3"
        >
          {embroideryTypes.map((type) => (
            <option key={type}>{type}</option>
          ))}
        </select>
        <div className="grid gap-3 sm:grid-cols-3">
          <input
            type="number"
            min={1}
            max={100}
            value={design.logoSize}
            onChange={(e) => updateDesign("logoSize", Number(e.target.value))}
            className="w-full rounded-xl border border-black/20 px-4 py-3"
            placeholder="Taille"
          />
          <input
            type="number"
            min={0}
            max={100}
            value={design.x}
            onChange={(e) => updateDesign("x", Number(e.target.value))}
            className="w-full rounded-xl border border-black/20 px-4 py-3"
            placeholder="X"
          />
          <input
            type="number"
            min={0}
            max={100}
            value={design.y}
            onChange={(e) => updateDesign("y", Number(e.target.value))}
            className="w-full rounded-xl border border-black/20 px-4 py-3"
            placeholder="Y"
          />
        </div>
        <input
          type="number"
          min={1}
          value={design.quantity}
          onChange={(e) => updateDesign("quantity", Number(e.target.value))}
          className="w-full rounded-xl border border-black/20 px-4 py-3"
        />
      </div>
    </div>
  );
}
