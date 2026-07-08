import { PrismaClient, UserRole } from '@prisma/client';

const prisma = new PrismaClient();

type CityCollection = {
  collection: 'premium' | 'regional' | 'nrw' | 'frpremium' | 'frheritage' | 'frriviera' | 'bepremium' | 'beheritage' | 'beardennes';
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

const franceCityCollections: CityCollection[] = [
  { collection: 'frpremium', city: 'Paris', landmark: 'Eiffel Tower and Seine skyline', priceCents: 6390, stock: 30 },
  { collection: 'frpremium', city: 'Lyon', landmark: 'Basilica of Fourviere and old town', priceCents: 6190, stock: 24 },
  { collection: 'frpremium', city: 'Marseille', landmark: 'Vieux-Port and Notre-Dame de la Garde', priceCents: 6190, stock: 24 },
  { collection: 'frpremium', city: 'Toulouse', landmark: 'Capitole facade and pink city vibe', priceCents: 6090, stock: 22 },
  { collection: 'frpremium', city: 'Nice', landmark: 'Promenade des Anglais horizon', priceCents: 6190, stock: 22 },
  { collection: 'frpremium', city: 'Nantes', landmark: 'Castle of the Dukes silhouette', priceCents: 5990, stock: 20 },
  { collection: 'frpremium', city: 'Strasbourg', landmark: 'Cathedral and timbered houses', priceCents: 6090, stock: 20 },
  { collection: 'frpremium', city: 'Montpellier', landmark: 'Place de la Comedie linework', priceCents: 5990, stock: 18 },
  { collection: 'frpremium', city: 'Bordeaux', landmark: 'Place de la Bourse reflection', priceCents: 6090, stock: 18 },
  { collection: 'frpremium', city: 'Lille', landmark: 'Old stock exchange icon', priceCents: 5990, stock: 18 },
  { collection: 'frheritage', city: 'Rouen', landmark: 'Rouen Cathedral profile', priceCents: 5690, stock: 16 },
  { collection: 'frheritage', city: 'Reims', landmark: 'Coronation cathedral silhouette', priceCents: 5690, stock: 16 },
  { collection: 'frheritage', city: 'Dijon', landmark: 'Palace of the Dukes contour', priceCents: 5690, stock: 16 },
  { collection: 'frheritage', city: 'Clermont-Ferrand', landmark: 'Black cathedral skyline', priceCents: 5690, stock: 14 },
  { collection: 'frheritage', city: 'Tours', landmark: 'Loire chateau spirit', priceCents: 5590, stock: 14 },
  { collection: 'frheritage', city: 'Orleans', landmark: 'Joan of Arc square motif', priceCents: 5590, stock: 14 },
  { collection: 'frheritage', city: 'Avignon', landmark: 'Palace of the Popes outline', priceCents: 5590, stock: 14 },
  { collection: 'frheritage', city: 'Poitiers', landmark: 'Romanesque facade line', priceCents: 5490, stock: 14 },
  { collection: 'frheritage', city: 'Nancy', landmark: 'Place Stanislas geometry', priceCents: 5590, stock: 14 },
  { collection: 'frheritage', city: 'Metz', landmark: 'Metz Cathedral stained roof form', priceCents: 5490, stock: 14 },
  { collection: 'frriviera', city: 'Cannes', landmark: 'Croisette and festival palace lines', priceCents: 5890, stock: 16 },
  { collection: 'frriviera', city: 'Antibes', landmark: 'Old ramparts and marina', priceCents: 5790, stock: 15 },
  { collection: 'frriviera', city: 'Saint-Tropez', landmark: 'Harbor masts and bell tower', priceCents: 5890, stock: 15 },
  { collection: 'frriviera', city: 'Annecy', landmark: 'Canals and alpine frame', priceCents: 5890, stock: 15 },
  { collection: 'frriviera', city: 'Grenoble', landmark: 'Bastille cable car silhouette', priceCents: 5790, stock: 15 },
  { collection: 'frriviera', city: 'Chamonix', landmark: 'Mont Blanc ridge profile', priceCents: 5990, stock: 14 },
  { collection: 'frriviera', city: 'Biarritz', landmark: 'Rocher de la Vierge line', priceCents: 5790, stock: 14 },
  { collection: 'frriviera', city: 'Bayonne', landmark: 'Nive river and old bridge', priceCents: 5690, stock: 14 },
  { collection: 'frriviera', city: 'La Rochelle', landmark: 'Old port towers icon', priceCents: 5790, stock: 14 },
  { collection: 'frriviera', city: 'Perpignan', landmark: 'Castillet fortress motif', priceCents: 5690, stock: 14 },
];

const belgiumCityCollections: CityCollection[] = [
  { collection: 'bepremium', city: 'Brussels', landmark: 'Grand Place and Atomium silhouette', priceCents: 6190, stock: 24 },
  { collection: 'bepremium', city: 'Antwerp', landmark: 'Cathedral tower and port lines', priceCents: 6090, stock: 22 },
  { collection: 'bepremium', city: 'Ghent', landmark: 'Graslei quays and Belfry icon', priceCents: 6090, stock: 22 },
  { collection: 'bepremium', city: 'Bruges', landmark: 'Medieval canal skyline', priceCents: 6190, stock: 20 },
  { collection: 'bepremium', city: 'Leuven', landmark: 'Town hall gothic facade', priceCents: 5990, stock: 18 },
  { collection: 'bepremium', city: 'Liege', landmark: 'Stairs of Bueren profile', priceCents: 5990, stock: 18 },
  { collection: 'bepremium', city: 'Namur', landmark: 'Citadel contour and river junction', priceCents: 5890, stock: 18 },
  { collection: 'bepremium', city: 'Mons', landmark: 'Belfry and Grand-Place frame', priceCents: 5890, stock: 16 },
  { collection: 'bepremium', city: 'Charleroi', landmark: 'Industrial heritage linework', priceCents: 5790, stock: 16 },
  { collection: 'bepremium', city: 'Mechelen', landmark: 'St Rumbold tower silhouette', priceCents: 5890, stock: 16 },
  { collection: 'beheritage', city: 'Ypres', landmark: 'Menin Gate memorial arch', priceCents: 5690, stock: 15 },
  { collection: 'beheritage', city: 'Tournai', landmark: 'Notre-Dame cathedral outlines', priceCents: 5690, stock: 15 },
  { collection: 'beheritage', city: 'Dinant', landmark: 'Citadel and collegiate church', priceCents: 5790, stock: 15 },
  { collection: 'beheritage', city: 'Spa', landmark: 'Historic thermal architecture', priceCents: 5590, stock: 14 },
  { collection: 'beheritage', city: 'Kortrijk', landmark: 'Broel towers silhouette', priceCents: 5590, stock: 14 },
  { collection: 'beheritage', city: 'Aalst', landmark: 'Belfry and carnival heritage motif', priceCents: 5490, stock: 14 },
  { collection: 'beheritage', city: 'Lier', landmark: 'Zimmer tower profile', priceCents: 5490, stock: 14 },
  { collection: 'beheritage', city: 'Hasselt', landmark: 'Cathedral and boulevard line', priceCents: 5490, stock: 14 },
  { collection: 'beheritage', city: 'Nivelles', landmark: 'Collegiate church icon', priceCents: 5390, stock: 14 },
  { collection: 'beheritage', city: 'Waterloo', landmark: 'Lion mound silhouette', priceCents: 5590, stock: 14 },
  { collection: 'beardennes', city: 'Ostend', landmark: 'North Sea promenade horizon', priceCents: 5790, stock: 15 },
  { collection: 'beardennes', city: 'Knokke-Heist', landmark: 'Coastal line and dunes', priceCents: 5890, stock: 14 },
  { collection: 'beardennes', city: 'Blankenberge', landmark: 'Pier and beachfront outline', priceCents: 5690, stock: 14 },
  { collection: 'beardennes', city: 'De Haan', landmark: 'Belle Epoque seafront style', priceCents: 5590, stock: 14 },
  { collection: 'beardennes', city: 'La Roche-en-Ardenne', landmark: 'Castle ruins and river bend', priceCents: 5790, stock: 14 },
  { collection: 'beardennes', city: 'Durbuy', landmark: 'Old stone streets profile', priceCents: 5690, stock: 14 },
  { collection: 'beardennes', city: 'Bastogne', landmark: 'WWII memorial linework', priceCents: 5790, stock: 14 },
  { collection: 'beardennes', city: 'Bouillon', landmark: 'Medieval castle silhouette', priceCents: 5690, stock: 14 },
  { collection: 'beardennes', city: 'Malmedy', landmark: 'Cathedral and valley frame', priceCents: 5590, stock: 14 },
  { collection: 'beardennes', city: 'Stavelot', landmark: 'Abbey and race heritage icon', priceCents: 5590, stock: 14 },
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
        : collection === 'nrw'
          ? { start: '#0B132B', end: '#1C2541', accent: '#E63946' }
          : collection === 'frpremium'
            ? { start: '#0B1D51', end: '#D72638', accent: '#F4D35E' }
            : collection === 'frheritage'
              ? { start: '#3C1642', end: '#086375', accent: '#F4A259' }
                : collection === 'frriviera'
                  ? { start: '#1D3557', end: '#457B9D', accent: '#E63946' }
                  : collection === 'bepremium'
                    ? { start: '#111111', end: '#E63946', accent: '#FFD166' }
                    : collection === 'beheritage'
                      ? { start: '#1D3557', end: '#6C757D', accent: '#F4A259' }
                      : { start: '#0B132B', end: '#1C2541', accent: '#5BC0BE' };

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

  let germanyCreatedOrUpdatedCount = 0;
  let franceCreatedOrUpdatedCount = 0;
  let belgiumCreatedOrUpdatedCount = 0;

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

    germanyCreatedOrUpdatedCount += 1;
  }

  for (let index = 0; index < franceCityCollections.length; index += 1) {
    const item = franceCityCollections[index];
    const citySlug = slugify(item.city);
    const slug = `${item.collection}-${citySlug}-cap`;
    const padded = String(index + 1).padStart(3, '0');
    const sku = `CITY-${item.collection.toUpperCase()}-${padded}`;
    const imageUrl = buildCityImageDataUrl(item.collection, item.city);

    await prisma.product.upsert({
      where: { slug },
      update: {
        name: `${item.city} City Cap`,
        description: `Dope&Cute Studio France City Collection. Front embroidery: ${item.city}. Landmark line: ${item.landmark}. Back detail: subtle FR tricolor and D&C side logo.`,
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
        description: `Dope&Cute Studio France City Collection. Front embroidery: ${item.city}. Landmark line: ${item.landmark}. Back detail: subtle FR tricolor and D&C side logo.`,
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

    franceCreatedOrUpdatedCount += 1;
  }

  for (let index = 0; index < belgiumCityCollections.length; index += 1) {
    const item = belgiumCityCollections[index];
    const citySlug = slugify(item.city);
    const slug = `${item.collection}-${citySlug}-cap`;
    const padded = String(index + 1).padStart(3, '0');
    const sku = `CITY-${item.collection.toUpperCase()}-${padded}`;
    const imageUrl = buildCityImageDataUrl(item.collection, item.city);

    await prisma.product.upsert({
      where: { slug },
      update: {
        name: `${item.city} City Cap`,
        description: `Dope&Cute Studio Belgium City Collection. Front embroidery: ${item.city}. Landmark line: ${item.landmark}. Back detail: subtle BE tricolor and D&C side logo.`,
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
        description: `Dope&Cute Studio Belgium City Collection. Front embroidery: ${item.city}. Landmark line: ${item.landmark}. Back detail: subtle BE tricolor and D&C side logo.`,
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

    belgiumCreatedOrUpdatedCount += 1;
  }

  await prisma.auditLog.create({
    data: {
      userId: admin.id,
      action: 'SEED_RUN',
      entity: 'Database',
      payloadJson: JSON.stringify({
        productId: signatureProduct.id,
        germanyCityCollectionProducts: germanyCreatedOrUpdatedCount,
        franceCityCollectionProducts: franceCreatedOrUpdatedCount,
        belgiumCityCollectionProducts: belgiumCreatedOrUpdatedCount,
      }),
    },
  });

  console.log(
    `Seed completed with ${germanyCreatedOrUpdatedCount} Germany City, ${franceCreatedOrUpdatedCount} France City and ${belgiumCreatedOrUpdatedCount} Belgium City Collection products.`,
  );
}

main()
  .catch((error) => {
    console.error(error);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
