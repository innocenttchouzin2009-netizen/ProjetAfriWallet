import { PrismaClient, UserRole } from '@prisma/client';

const prisma = new PrismaClient();

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

  await prisma.auditLog.create({
    data: {
      userId: admin.id,
      action: 'SEED_RUN',
      entity: 'Database',
      payloadJson: JSON.stringify({ productId: signatureProduct.id }),
    },
  });

  console.log('Seed completed');
}

main()
  .catch((error) => {
    console.error(error);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
