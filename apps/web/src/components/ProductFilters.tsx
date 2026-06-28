import { useEffect, useState } from 'react';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import { useDebounce } from '@/hooks/useDebounce/useDebounce';
import { GetProductsParams } from '@/features/catalog/use-cases/GetProductsUseCase';

type FilterOption = {
  value: string;
  label: string;
};

type ProductFiltersProps = {
  params: GetProductsParams;
  categories: FilterOption[];
  colors: FilterOption[];
  onChange: (changes: Partial<GetProductsParams>) => void;
  onReset: () => void;
};

export default function ProductFilters({ params, categories, colors, onChange, onReset }: ProductFiltersProps) {
  const [search, setSearch] = useState(params.query ?? '');
  const debouncedSearch = useDebounce(search, 300);

  useEffect(() => {
    onChange({ query: debouncedSearch || undefined, page: 1 });
  }, [debouncedSearch, onChange]);

  useEffect(() => {
    setSearch(params.query ?? '');
  }, [params.query]);

  return (
    <aside className="space-y-6 rounded-[32px] border border-white/10 bg-white/5 p-8">
      <h2 className="text-2xl font-black">Filtres</h2>
      <div className="space-y-4">
        <div>
          <label className="mb-2 block text-sm font-semibold text-white/80">Recherche</label>
          <Input
            value={search}
            placeholder="Rechercher un produit..."
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        <div>
          <label className="mb-2 block text-sm font-semibold text-white/80">Catégorie</label>
          <Select
            value={params.category ?? 'all'}
            onChange={(event) => onChange({ category: event.target.value === 'all' ? undefined : event.target.value, page: 1 })}
            options={[{ value: 'all', label: 'Toutes' }, ...categories]}
          />
        </div>
        <div>
          <label className="mb-2 block text-sm font-semibold text-white/80">Couleur</label>
          <Select
            value={params.color ?? 'all'}
            onChange={(event) => onChange({ color: event.target.value === 'all' ? undefined : event.target.value, page: 1 })}
            options={[{ value: 'all', label: 'Toutes' }, ...colors]}
          />
        </div>
        <div>
          <label className="mb-2 block text-sm font-semibold text-white/80">Prix</label>
          <Select
            value={params.minPrice ? 'premium' : 'all'}
            onChange={(event) => {
              const value = event.target.value;
              if (value === 'premium') {
                onChange({ minPrice: 50, maxPrice: undefined, page: 1 });
              } else {
                onChange({ minPrice: undefined, maxPrice: undefined, page: 1 });
              }
            }}
            options={[
              { value: 'all', label: 'Tous' },
              { value: 'premium', label: '50€ et plus' },
            ]}
          />
        </div>
        <div>
          <label className="mb-2 block text-sm font-semibold text-white/80">Disponibilité</label>
          <Select
            value={params.inStock === false ? 'out' : params.inStock ? 'in' : 'all'}
            onChange={(event) => {
              const value = event.target.value;
              if (value === 'in') onChange({ inStock: true, page: 1 });
              else if (value === 'out') onChange({ inStock: false, page: 1 });
              else onChange({ inStock: undefined, page: 1 });
            }}
            options={[
              { value: 'all', label: 'Tous' },
              { value: 'in', label: 'En stock' },
              { value: 'out', label: 'Rupture' },
            ]}
          />
        </div>
        <div>
          <label className="mb-2 block text-sm font-semibold text-white/80">Trier</label>
          <Select
            value={params.sort ?? 'default'}
            onChange={(event) =>
              onChange({
                sort: event.target.value === 'default' ? undefined : (event.target.value as GetProductsParams['sort']),
                page: 1,
              })
            }
            options={[
              { value: 'default', label: 'Par défaut' },
              { value: 'priceAsc', label: 'Prix croissant' },
              { value: 'priceDesc', label: 'Prix décroissant' },
              { value: 'newest', label: 'Nouveautés' },
              { value: 'bestSelling', label: 'Meilleures ventes' },
            ]}
          />
        </div>
        <div className="flex gap-3">
          <Button
            variant="secondary"
            className="flex-1"
            onClick={() => {
              setSearch('');
              onReset();
            }}
          >
            Réinitialiser
          </Button>
        </div>
      </div>
    </aside>
  );
}
