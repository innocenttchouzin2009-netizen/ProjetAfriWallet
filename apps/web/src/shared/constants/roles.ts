export const USER_ROLES = {
  guest: 'guest',
  customer: 'customer',
  staff: 'staff',
  admin: 'admin',
} as const;

export const PERMISSIONS = {
  browseCatalog: 'browse_catalog',
  manageCart: 'manage_cart',
  checkout: 'checkout',
  manageOrders: 'manage_orders',
  manageProducts: 'manage_products',
  manageUsers: 'manage_users',
} as const;

export type UserRole = typeof USER_ROLES[keyof typeof USER_ROLES];
export type Permission = typeof PERMISSIONS[keyof typeof PERMISSIONS];

export const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {
  guest: [PERMISSIONS.browseCatalog],
  customer: [PERMISSIONS.browseCatalog, PERMISSIONS.manageCart, PERMISSIONS.checkout],
  staff: [PERMISSIONS.browseCatalog, PERMISSIONS.manageCart, PERMISSIONS.checkout, PERMISSIONS.manageOrders],
  admin: [
    PERMISSIONS.browseCatalog,
    PERMISSIONS.manageCart,
    PERMISSIONS.checkout,
    PERMISSIONS.manageOrders,
    PERMISSIONS.manageProducts,
    PERMISSIONS.manageUsers,
  ],
};

export const canAccess = (role: UserRole, permission: Permission) => ROLE_PERMISSIONS[role]?.includes(permission) ?? false;
