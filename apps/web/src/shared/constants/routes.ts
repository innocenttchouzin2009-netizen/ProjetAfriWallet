export const APP_ROUTES = {
  home: '/',
  shop: '/shop',
  studio: '/studio',
  pro: '/pro',
  contact: '/contact',
  cart: '/cart',
  checkout: '/checkout',
  admin: '/admin',
  login: '/login',
  profile: '/profile',
} as const;

export type AppRoute = typeof APP_ROUTES[keyof typeof APP_ROUTES];
