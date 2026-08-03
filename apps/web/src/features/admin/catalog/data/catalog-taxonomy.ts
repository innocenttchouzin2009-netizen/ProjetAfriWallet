export interface CollectionDefinition {
  slug: string;
  label: string;
  group: 'country' | 'regional' | 'special';
}

export const HAT_TYPE_OPTIONS = [
  'Snapback',
  'Baseball',
  'Trucker',
  'Dad Hat',
  '5-Panel',
  'Fitted',
] as const;

export const COLLECTION_DEFINITIONS: CollectionDefinition[] = [
  { slug: 'depremium', label: 'Germany City', group: 'country' },
  { slug: 'frpremium', label: 'France City', group: 'country' },
  { slug: 'itpremium', label: 'Italy City', group: 'country' },
  { slug: 'bepremium', label: 'Belgium City', group: 'country' },
  { slug: 'nlpremium', label: 'Netherlands City', group: 'country' },
  { slug: 'chpremium', label: 'Switzerland City', group: 'country' },
  { slug: 'espremium', label: 'Spain City', group: 'country' },
  { slug: 'atpremium', label: 'Austria City', group: 'country' },
  { slug: 'ukpremium', label: 'UK City', group: 'country' },
  { slug: 'europe', label: 'Europe Collection', group: 'regional' },
  { slug: 'world', label: 'World Collection', group: 'regional' },
  { slug: 'limited', label: 'Limited Edition', group: 'special' },
];

export function getCollectionLabel(slug: string | null | undefined): string {
  if (!slug) return 'Unassigned';
  const found = COLLECTION_DEFINITIONS.find((item) => item.slug === slug);
  return found?.label ?? slug;
}

export function extractCollectionSlugFromProductSlug(productSlug: string): string {
  const [head] = productSlug.split('-');
  return head || 'depremium';
}
