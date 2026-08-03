import { NextRequest, NextResponse } from 'next/server';
import { OrderStatus } from '@prisma/client';
import { z } from 'zod';
import { prisma } from '@/lib/prisma';
import { requireRole } from '@/features/auth/guards/require-role';
import { getCollectionLabel } from '@/features/admin/catalog/data/catalog-taxonomy';
import { AuditService } from '@/features/audit/services/audit.service';

export const dynamic = 'force-dynamic';

const EXCLUDED_STATUSES: OrderStatus[] = [OrderStatus.DRAFT, OrderStatus.CANCELED];
const DashboardQuerySchema = z.object({
  range: z.enum(['day', 'week', 'month', 'year']).default('week'),
});

type DashboardRange = 'day' | 'week' | 'month' | 'year';
const DEFAULT_SLA_THRESHOLD = 15;
const SLA_THRESHOLD_SETTING_ID = 'sla-threshold';
const ALERT_WEBHOOK_URL = process.env.ADMIN_ALERT_WEBHOOK_URL;

function startOfToday() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

function startOfWeek() {
  const now = new Date();
  const day = now.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  const result = new Date(now);
  result.setDate(now.getDate() + diff);
  result.setHours(0, 0, 0, 0);
  return result;
}

function startOfMonth() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1);
}

function startOfYear() {
  const now = new Date();
  return new Date(now.getFullYear(), 0, 1);
}

function addDays(base: Date, days: number) {
  const next = new Date(base);
  next.setDate(base.getDate() + days);
  return next;
}

function addMonths(base: Date, months: number) {
  return new Date(base.getFullYear(), base.getMonth() + months, 1);
}

function formatDateKey(value: Date): string {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function toHourKey(value: Date): string {
  return `${formatDateKey(value)} ${String(value.getHours()).padStart(2, '0')}:00`;
}

function toMonthKey(value: Date): string {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}`;
}

function buildTrendBuckets(range: DashboardRange, now: Date) {
  if (range === 'day') {
    const start = startOfToday();
    const buckets: Array<{ key: string; label: string }> = [];
    for (let hour = 0; hour < 24; hour += 1) {
      const date = new Date(start);
      date.setHours(hour, 0, 0, 0);
      buckets.push({
        key: toHourKey(date),
        label: `${String(hour).padStart(2, '0')}h`,
      });
    }
    return { start, buckets };
  }

  if (range === 'week') {
    const start = new Date(now);
    start.setHours(0, 0, 0, 0);
    start.setDate(start.getDate() - 6);
    const buckets: Array<{ key: string; label: string }> = [];
    for (let i = 0; i < 7; i += 1) {
      const date = new Date(start);
      date.setDate(start.getDate() + i);
      buckets.push({
        key: formatDateKey(date),
        label: date.toLocaleDateString('fr-FR', { weekday: 'short' }),
      });
    }
    return { start, buckets };
  }

  if (range === 'month') {
    const start = new Date(now);
    start.setHours(0, 0, 0, 0);
    start.setDate(start.getDate() - 29);
    const buckets: Array<{ key: string; label: string }> = [];
    for (let i = 0; i < 30; i += 1) {
      const date = new Date(start);
      date.setDate(start.getDate() + i);
      buckets.push({
        key: formatDateKey(date),
        label: date.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' }),
      });
    }
    return { start, buckets };
  }

  const start = new Date(now.getFullYear(), now.getMonth() - 11, 1);
  const buckets: Array<{ key: string; label: string }> = [];
  for (let i = 0; i < 12; i += 1) {
    const date = new Date(start.getFullYear(), start.getMonth() + i, 1);
    buckets.push({
      key: toMonthKey(date),
      label: date.toLocaleDateString('fr-FR', { month: 'short' }),
    });
  }
  return { start, buckets };
}

function trendKeyForDate(range: DashboardRange, value: Date) {
  if (range === 'day') return toHourKey(new Date(value.getFullYear(), value.getMonth(), value.getDate(), value.getHours()));
  if (range === 'year') return toMonthKey(value);
  return formatDateKey(value);
}

function computeDeltaPct(current: number, previous: number) {
  if (previous <= 0) return current > 0 ? 100 : 0;
  return Number((((current - previous) / previous) * 100).toFixed(1));
}

function getCollectionSlug(productSlug: string) {
  const [head] = productSlug.split('-');
  return head || 'unknown';
}

async function getStoredSlaThreshold() {
  const latest = await prisma.auditLog.findFirst({
    where: {
      action: 'ADMIN_SLA_THRESHOLD_UPDATED',
      entity: 'Settings',
      entityId: SLA_THRESHOLD_SETTING_ID,
    },
    orderBy: { createdAt: 'desc' },
  });

  if (!latest?.payloadJson) return DEFAULT_SLA_THRESHOLD;

  try {
    const payload = JSON.parse(latest.payloadJson) as { thresholdPct?: number };
    const value = Number(payload.thresholdPct);
    if (Number.isFinite(value) && value >= 1 && value <= 100) {
      return Math.round(value);
    }
  } catch {
    return DEFAULT_SLA_THRESHOLD;
  }

  return DEFAULT_SLA_THRESHOLD;
}

function aggregateCountries(
  orders: Array<{
    totalCents: number;
    shippingAddress: { country: string } | null;
  }>,
) {
  const map = new Map<string, { revenueCents: number; orders: number }>();
  for (const order of orders) {
    const country = order.shippingAddress?.country?.trim() || 'Unknown';
    const current = map.get(country) ?? { revenueCents: 0, orders: 0 };
    current.revenueCents += order.totalCents;
    current.orders += 1;
    map.set(country, current);
  }
  return map;
}

function aggregateCollections(
  orders: Array<{
    items: Array<{
      totalPriceCents: number;
      productVariant: { product: { slug: string } };
    }>;
  }>,
) {
  const map = new Map<string, { revenueCents: number; units: number }>();
  for (const order of orders) {
    for (const item of order.items) {
      const collectionSlug = getCollectionSlug(item.productVariant.product.slug);
      const current = map.get(collectionSlug) ?? { revenueCents: 0, units: 0 };
      current.revenueCents += item.totalPriceCents;
      current.units += 1;
      map.set(collectionSlug, current);
    }
  }
  return map;
}

function buildRangeWindows(range: DashboardRange, now: Date) {
  if (range === 'day') {
    const currentStart = startOfToday();
    const currentEnd = now;
    const previousStart = addDays(currentStart, -1);
    const previousEnd = currentStart;
    return { currentStart, currentEnd, previousStart, previousEnd };
  }

  if (range === 'week') {
    const currentStart = addDays(startOfToday(), -6);
    const currentEnd = now;
    const previousStart = addDays(currentStart, -7);
    const previousEnd = currentStart;
    return { currentStart, currentEnd, previousStart, previousEnd };
  }

  if (range === 'month') {
    const currentStart = addDays(startOfToday(), -29);
    const currentEnd = now;
    const previousStart = addDays(currentStart, -30);
    const previousEnd = currentStart;
    return { currentStart, currentEnd, previousStart, previousEnd };
  }

  const currentStart = new Date(now.getFullYear(), now.getMonth() - 11, 1);
  const currentEnd = now;
  const previousStart = addMonths(currentStart, -12);
  const previousEnd = currentStart;
  return { currentStart, currentEnd, previousStart, previousEnd };
}

async function notifySlaWebhook(payload: {
  date: string;
  breachRatePct: number;
  thresholdPct: number;
  overdueCount: number;
  activePipelineCount: number;
}) {
  if (!ALERT_WEBHOOK_URL) return;

  try {
    await fetch(ALERT_WEBHOOK_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        event: 'ADMIN_SLA_BREACH_ALERT',
        service: 'dope-cute-studio-admin',
        ...payload,
      }),
    });
  } catch {
    // Webhook notifications are best effort and must not break dashboard API.
  }
}

export async function GET(request: NextRequest) {
  const auth = requireRole(request, ['manager', 'production', 'support']);
  if (auth instanceof NextResponse) return auth;

  const { searchParams } = new URL(request.url);
  const parsed = DashboardQuerySchema.safeParse({
    range: searchParams.get('range') ?? undefined,
  });
  if (!parsed.success) {
    return NextResponse.json({ message: parsed.error.issues.map((issue) => issue.message).join('; ') }, { status: 400 });
  }
  const range = parsed.data.range;

  const now = new Date();
  const today = startOfToday();
  const todayKey = formatDateKey(today);
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  const weekStart = startOfWeek();
  const monthStart = startOfMonth();
  const yearStart = startOfYear();
  const lateThreshold = new Date(now.getTime() - 48 * 60 * 60 * 1000);
  const trendWindow = buildTrendBuckets(range, now);
  const rangeWindow = buildRangeWindows(range, now);
  const slaThresholdPct = await getStoredSlaThreshold();

  const [
    todayRevenue,
    yesterdayRevenue,
    weekRevenue,
    monthRevenue,
    yearRevenue,
    pendingCount,
    productionCount,
    readyCount,
    shippedCount,
    deliveredCount,
    lateOrdersCount,
    newCustomersToday,
    lowStockCount,
    currentWindowOrders,
    previousWindowOrders,
    pipelineOrders,
  ] = await Promise.all([
    prisma.order.aggregate({
      _sum: { totalCents: true },
      where: {
        createdAt: { gte: today },
        status: { notIn: EXCLUDED_STATUSES },
      },
    }),
    prisma.order.aggregate({
      _sum: { totalCents: true },
      where: {
        createdAt: { gte: yesterday, lt: today },
        status: { notIn: EXCLUDED_STATUSES },
      },
    }),
    prisma.order.aggregate({
      _sum: { totalCents: true },
      where: {
        createdAt: { gte: weekStart },
        status: { notIn: EXCLUDED_STATUSES },
      },
    }),
    prisma.order.aggregate({
      _sum: { totalCents: true },
      where: {
        createdAt: { gte: monthStart },
        status: { notIn: EXCLUDED_STATUSES },
      },
    }),
    prisma.order.aggregate({
      _sum: { totalCents: true },
      where: {
        createdAt: { gte: yearStart },
        status: { notIn: EXCLUDED_STATUSES },
      },
    }),
    prisma.order.count({ where: { status: OrderStatus.CONFIRMED } }),
    prisma.order.count({ where: { status: OrderStatus.IN_PRODUCTION } }),
    prisma.order.count({ where: { status: OrderStatus.READY } }),
    prisma.order.count({ where: { status: OrderStatus.SHIPPED } }),
    prisma.order.count({ where: { status: OrderStatus.DELIVERED } }),
    prisma.order.count({
      where: {
        status: { in: [OrderStatus.CONFIRMED, OrderStatus.IN_PRODUCTION] },
        createdAt: { lt: lateThreshold },
      },
    }),
    prisma.user.count({
      where: {
        createdAt: { gte: today },
      },
    }),
    prisma.productVariant.count({
      where: {
        isActive: true,
        stock: { lte: 10 },
      },
    }),
    prisma.order.findMany({
      where: {
        createdAt: { gte: rangeWindow.currentStart, lt: rangeWindow.currentEnd },
        status: { notIn: EXCLUDED_STATUSES },
      },
      include: {
        shippingAddress: true,
        items: {
          include: {
            productVariant: {
              include: {
                product: true,
              },
            },
          },
        },
      },
      orderBy: { createdAt: 'asc' },
    }),
    prisma.order.findMany({
      where: {
        createdAt: { gte: rangeWindow.previousStart, lt: rangeWindow.previousEnd },
        status: { notIn: EXCLUDED_STATUSES },
      },
      include: {
        shippingAddress: true,
        items: {
          include: {
            productVariant: {
              include: {
                product: true,
              },
            },
          },
        },
      },
      orderBy: { createdAt: 'asc' },
    }),
    prisma.order.findMany({
      where: {
        status: { in: [OrderStatus.CONFIRMED, OrderStatus.IN_PRODUCTION, OrderStatus.READY] },
      },
      select: {
        status: true,
        createdAt: true,
      },
    }),
  ]);

  const todayValue = todayRevenue._sum.totalCents ?? 0;
  const yesterdayValue = yesterdayRevenue._sum.totalCents ?? 0;
  const changePct = yesterdayValue > 0 ? ((todayValue - yesterdayValue) / yesterdayValue) * 100 : todayValue > 0 ? 100 : 0;

  const productTotals = new Map<string, { variantId: string; name: string; variantName: string; sku: string; units: number; revenueCents: number }>();
  for (const order of currentWindowOrders) {
    for (const item of order.items) {
      const id = item.productVariantId;
      const current = productTotals.get(id) ?? {
        variantId: id,
        name: item.productVariant.product.name,
        variantName: item.productVariant.name,
        sku: item.productVariant.sku,
        units: 0,
        revenueCents: 0,
      };
      current.units += item.quantity;
      current.revenueCents += item.totalPriceCents;
      productTotals.set(id, current);
    }
  }

  const topProducts = [...productTotals.values()]
    .sort((a, b) => b.units - a.units)
    .slice(0, 5);

  const currentCountries = aggregateCountries(currentWindowOrders);
  const previousCountries = aggregateCountries(previousWindowOrders);
  const salesByCountry = [...currentCountries.entries()]
    .map(([country, values]) => ({
      country,
      revenueCents: values.revenueCents,
      orders: values.orders,
      deltaPct: computeDeltaPct(values.revenueCents, previousCountries.get(country)?.revenueCents ?? 0),
    }))
    .sort((a, b) => b.revenueCents - a.revenueCents)
    .slice(0, 8);

  const currentCollections = aggregateCollections(currentWindowOrders);
  const previousCollections = aggregateCollections(previousWindowOrders);
  const topCollections = [...currentCollections.entries()]
    .map(([slug, values]) => ({
      slug,
      label: getCollectionLabel(slug),
      revenueCents: values.revenueCents,
      units: values.units,
      deltaPct: computeDeltaPct(values.revenueCents, previousCollections.get(slug)?.revenueCents ?? 0),
    }))
    .sort((a, b) => b.revenueCents - a.revenueCents)
    .slice(0, 6);

  const trendMap = new Map<string, number>();
  for (const bucket of trendWindow.buckets) {
    trendMap.set(bucket.key, 0);
  }

  for (const order of currentWindowOrders) {
    const key = trendKeyForDate(range, order.createdAt);
    trendMap.set(key, (trendMap.get(key) ?? 0) + order.totalCents);
  }

  const trend = trendWindow.buckets.map((bucket) => ({
    key: bucket.key,
    label: bucket.label,
    revenueCents: trendMap.get(bucket.key) ?? 0,
  }));

  const pendingAges = pipelineOrders.filter((order) => order.status === OrderStatus.CONFIRMED).map((order) => (now.getTime() - order.createdAt.getTime()) / 3600000);
  const productionAges = pipelineOrders.filter((order) => order.status === OrderStatus.IN_PRODUCTION).map((order) => (now.getTime() - order.createdAt.getTime()) / 3600000);
  const readyAges = pipelineOrders.filter((order) => order.status === OrderStatus.READY).map((order) => (now.getTime() - order.createdAt.getTime()) / 3600000);
  const activePipelineCount = pipelineOrders.length;
  const overduePipelineCount = pipelineOrders.filter((order) => order.createdAt < lateThreshold).length;
  const avg = (values: number[]) => (values.length ? Number((values.reduce((sum, v) => sum + v, 0) / values.length).toFixed(1)) : 0);
  const breachRatePct = activePipelineCount ? Number(((overduePipelineCount / activePipelineCount) * 100).toFixed(1)) : 0;
  const slaAlert = breachRatePct >= slaThresholdPct;

  if (slaAlert) {
    const existingAlert = await prisma.auditLog.findFirst({
      where: {
        action: 'ADMIN_SLA_BREACH_ALERT',
        entity: 'Dashboard',
        entityId: todayKey,
      },
    });

    if (!existingAlert) {
      const alertPayload = {
        date: todayKey,
        breachRatePct,
        thresholdPct: slaThresholdPct,
        overdueCount: overduePipelineCount,
        activePipelineCount,
      };

      await AuditService.log({
        action: 'ADMIN_SLA_BREACH_ALERT',
        entity: 'Dashboard',
        entityId: todayKey,
        payload: alertPayload,
      });

      await notifySlaWebhook(alertPayload);
    }
  }

  return NextResponse.json({
    kpis: {
      revenueTodayCents: todayValue,
      revenueChangePct: Number(changePct.toFixed(1)),
      revenueWeekCents: weekRevenue._sum.totalCents ?? 0,
      revenueMonthCents: monthRevenue._sum.totalCents ?? 0,
      revenueYearCents: yearRevenue._sum.totalCents ?? 0,
      pendingOrders: pendingCount,
      inProductionOrders: productionCount,
      readyOrders: readyCount,
      shippedOrders: shippedCount,
      deliveredOrders: deliveredCount,
      lateOrders: lateOrdersCount,
      newCustomersToday,
      lowStockCount,
    },
    sla: {
      pendingAvgHours: avg(pendingAges),
      productionAvgHours: avg(productionAges),
      readyAvgHours: avg(readyAges),
      overdueCount: overduePipelineCount,
      activePipelineCount,
      breachRatePct,
    },
    topProducts,
    topCollections,
    salesByCountry,
    settings: {
      slaThresholdPct,
      slaAlert,
    },
    range,
    trend,
  });
}
