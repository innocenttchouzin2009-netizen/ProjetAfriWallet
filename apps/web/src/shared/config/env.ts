import { z } from 'zod';

const envSchema = z.object({
  NEXT_PUBLIC_API_BASE_URL: z.string().url().optional(),
  NEXT_PUBLIC_APP_NAME: z.string().default('Dope Cute Studio'),
  NEXT_PUBLIC_ENABLE_EXPERIMENTAL: z.string().optional(),
  NEXT_PUBLIC_ANALYTICS_ID: z.string().optional(),
});

const parsed = envSchema.safeParse(process.env);

if (!parsed.success) {
  const issues = parsed.error.issues.map((issue) => `${issue.path.join('.')}: ${issue.message}`).join(', ');
  throw new Error(`Invalid environment variables: ${issues}`);
}

export const env = parsed.data;

export type PublicEnv = z.infer<typeof envSchema>;
