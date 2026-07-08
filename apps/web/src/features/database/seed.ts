import { PrismaClient, UserRole } from '@prisma/client';

const prisma = new PrismaClient();

type CityCollection = {
  collection: 'premium' | 'regional' | 'nrw';
  city: string;
  landmark: string;
  priceCents: number;
  stock: number;
};

const germanyCityCollections: CityCollection[] = [
  { collection: 'premium', city: 'Berlin', landmark: 'Brandenburg Gate and TV Tower', priceCents: 5990, stock: 30 },
  { collection: 'premium', city: 'Hamburg', landmark: 'Elbphilharmonie and Harbor', priceCents: 5990, stock: 26 },
  { collection: 'premium', city: 'Munich', landmark: 'Neues Rathaus and Oktoberfest spirit', priceCents: 6190, stock: 24 },
  { collection: 'premium', city: 'Cologne', landmark: 'Cologne Cathedral silhouette', priceCents: 5990, stock: 28 },
  { collection: 'premium', city: 'Frankfurt am Main', landmark: 'Romer square and skyline', priceCents: 6190, stock: 22 },
  { collection: 'premium', city: 'Stuttgart', landmark: 'Schlossplatz and automotive heritage', priceCents: 5990, stock: 22 },
  { collection: 'premium', city: 'Dusseldorf', landmark: 'Rheinturm skyline linework', priceCents: 5990, stock: 20 },
  { collection: 'premium', city: 'Leipzig', landmark: 'Monument to the Battle of the Nations', priceCents: 5890, stock: 18 },
  { collection: 'premium', city: 'Dresden', landmark: 'Frauenkirche iconic dome', priceCents: 5890, stock: 18 },
  { collection: 'premium', city: 'Nuremberg', landmark: 'Imperial Castle profile', priceCents: 5890, stock: 18 },
  { collection: 'regional', city: 'Bremen', landmark: 'Roland Statue heritage line', priceCents: 5490, stock: 16 },
  { collection: 'regional', city: 'Hanover', landmark: 'New Town Hall architecture', priceCents: 5490, stock: 16 },
  { collection: 'regional', city: 'Heidelberg', landmark: 'Heidelberg Castle silhouette', priceCents: 5590, stock: 16 },
  { collection: 'regional', city: 'Freiburg im Breisgau', landmark: 'Freiburg Cathedral outline', priceCents: 5590, stock: 14 },
  { collection: 'regional', city: 'Karlsruhe', landmark: 'Karlsruhe Palace geometry', priceCents: 5490, stock: 14 },
  { collection: 'regional', city: 'Bonn', landmark: 'Beethoven House tribute', priceCents: 5390, stock: 14 },
  { collection: 'regional', city: 'Mainz', landmark: 'Mainz Cathedral line art', priceCents: 5490, stock: 14 },
  { collection: 'regional', city: 'Koblenz', landmark: 'Deutsches Eck monument', priceCents: 5490, stock: 14 },
  { collection: 'regional', city: 'Aachen', landmark: 'Aachen Cathedral heritage icon', priceCents: 5590, stock: 14 },
  { collection: 'regional', city: 'Lubeck', landmark: 'Holstentor gate silhouette', priceCents: 5490, stock: 14 },
  { collection: 'nrw', city: 'Solingen', landmark: 'Mungsten Bridge profile', priceCents: 5290, stock: 15 },
  { collection: 'nrw', city: 'Wuppertal', landmark: 'Schwebebahn elevated rail line', priceCents: 5290, stock: 15 },
  { collection: 'nrw', city: 'Essen', landmark: 'Zollverein industrial heritage', priceCents: 5290, stock: 15 },
  { collection: 'nrw', city: 'Dortmund', landmark: 'U-Tower skyline icon', priceCents: 5290, stock: 15 },
  { collection: 'nrw', city: 'Bochum', landmark: 'Mining heritage motif', priceCents: 5190, stock: 14 },
  { collection: 'nrw', city: 'Munster', landmark: 'Prinzipalmarkt facades', priceCents: 5290, stock: 14 },
  { collection: 'nrw', city: 'Bielefeld', landmark: 'Sparrenburg fortress profile', priceCents: 5190, stock: 14 },
  { collection: 'nrw', city: 'Duisburg', landmark: 'Landschaftspark structures', priceCents: 5190, stock: 14 },
  { collection: 'nrw', city: 'Aachen', landmark: 'Aachen Cathedral city edition', priceCents: 5290, stock: 14 },
  { collection: 'nrw', city: 'Paderborn', landmark: 'Paderborn Cathedral icon', priceCents: 5190, stock: 14 },
];

function slugify(value: string) {
  return value
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

function buildCityImageDataUrl(collection: CityCollection['collection'], city: string) {
  const palette =
    collection === 'premium'
      ? { start: '#1A2A6C', end: '#B21F1F', accent: '#FDBB2D' }
      : collection === 'regional'
        ? { start: '#0F2027', end: '#2C5364', accent: '#C8A45C' }
        : { start: '#0B132B', end: '#1C2541', accent: '#E63946' };

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="1200" viewBox="0 0 1200 1200">
  <defs>
    <linearGradient id="g" x1="0" x2="1" y1="0" y2="1">
      <stop offset="0%" stop-color="${palette.start}" />
      <stop offset="100%" stop-color="${palette.end}" />
    </linearGradient>
  </defs>
  <rect width="1200" height="1200" fill="url(#g)" />
  <circle cx="980" cy="220" r="200" fill="${palette.accent}" fill-opacity="0.25" />
  <circle cx="250" cy="980" r="280" fill="${palette.accent}" fill-opacity="0.15" />
  <text x="80" y="880" fill="white" font-family="Arial, sans-serif" font-size="150" font-weight="900">${city.toUpperCase()}</text>
  <text x="84" y="955" fill="${palette.accent}" font-family="Arial, sans-serif" font-size="54" font-weight="700">DOPE&CUTE STUDIO</text>
</svg>`;

  return `data:image/svg+xml,${encodeURIComponent(svg)}`;
}

async function main() {
  const admin = await prisma.user.upsert({
    where: { email: 'admin@dopecute.studio' },
    update: {},
    create: {
      email: 'admin@dopecute.studio',
      passwordHash: 'change-me',
      firstName: 'Super',
      lastName: 'Admin',
      role: UserRole.SUPER_ADMIN,
    },
  });

  const signatureProduct = await prisma.product.upsert({
    where: { slug: 'dc-signature-black' },
    update: {},
    create: {
      name: 'D&C Signature Black',
      slug: 'dc-signature-black',
      description: 'Casquette premium personnalisable',
      variants: {
        create: [
          {
            name: 'Standard',
            sku: 'CAP-001-STD',
            priceCents: 4990,
            stock: 25,
            isActive: true,
          },
        ],
      },
    },
    include: { variants: true },
  });

  let createdOrUpdatedCount = 0;

  for (let index = 0; index < germanyCityCollections.length; index += 1) {
    const item = germanyCityCollections[index];
    const citySlug = slugify(item.city);
    const slug = `${item.collection}-${citySlug}-cap`;
    const padded = String(index + 1).padStart(3, '0');
    const sku = `CITY-${item.collection.toUpperCase()}-${padded}`;
    const imageUrl = buildCityImageDataUrl(item.collection, item.city);

    await prisma.product.upsert({
      where: { slug },
      update: {
        name: `${item.city} City Cap`,
        description: `Dope&Cute Studio Germany City Collection. Front embroidery: ${item.city}. Landmark line: ${item.landmark}. Back detail: subtle DE flag and D&C side logo.`,
        isActive: true,
        images: {
          deleteMany: {},
          create: [
            {
              url: imageUrl,
              isPrimary: true,
              sortOrder: 0,
            },
          ],
        },
      },
      create: {
        name: `${item.city} City Cap`,
        slug,
        description: `Dope&Cute Studio Germany City Collection. Front embroidery: ${item.city}. Landmark line: ${item.landmark}. Back detail: subtle DE flag and D&C side logo.`,
        isActive: true,
        images: {
          create: [
            {
              url: imageUrl,
              isPrimary: true,
              sortOrder: 0,
            },
          ],
        },
        variants: {
          create: [
            {
              name: 'Standard',
              sku,
              priceCents: item.priceCents,
              stock: item.stock,
              isActive: true,
            },
          ],
        },
      },
    });

    createdOrUpdatedCount += 1;
  }

  await prisma.auditLog.create({
    data: {
      userId: admin.id,
      action: 'SEED_RUN',
      entity: 'Database',
      payloadJson: JSON.stringify({
        productId: signatureProduct.id,
        germanyCityCollectionProducts: createdOrUpdatedCount,
      }),
    },
  });

  console.log(`Seed completed with ${createdOrUpdatedCount} Germany City Collection products.`);
}

main()
  .catch((error) => {
    console.error(error);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
