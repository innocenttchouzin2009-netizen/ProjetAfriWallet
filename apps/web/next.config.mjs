/** @type {import('next').NextConfig} */
const csp = [
  "default-src 'self'",
  "base-uri 'self'",
  "object-src 'none'",
  "frame-ancestors 'none'",
  "script-src 'self' 'unsafe-inline' https://js.stripe.com https://www.paypal.com https://www.paypalobjects.com",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: https://www.paypal.com https://www.paypalobjects.com",
  "connect-src 'self' https://api.stripe.com https://js.stripe.com https://checkout.stripe.com https://www.paypal.com https://www.paypalobjects.com",
  "frame-src 'self' https://js.stripe.com https://checkout.stripe.com https://www.paypal.com",
  "form-action 'self' https://checkout.stripe.com https://www.paypal.com",
].join('; ');

const nextConfig = {
  reactStrictMode: true,
  poweredByHeader: false,
  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          {
            key: 'Content-Security-Policy',
            value: csp,
          },
          {
            key: 'X-Frame-Options',
            value: 'DENY',
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin',
          },
          {
            key: 'Permissions-Policy',
            value: 'camera=(), microphone=(), geolocation=(), payment=(self)',
          },
        ],
      },
    ];
  },
  eslint: {
    ignoreDuringBuilds: true,
  },
  images: {
    domains: ['images.unsplash.com'],
    formats: ['image/avif', 'image/webp'],
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'images.unsplash.com',
      },
    ],
  },
  experimental: {
    optimizePackageImports: ['@stripe/stripe-js'],
  },
};

export default nextConfig;
