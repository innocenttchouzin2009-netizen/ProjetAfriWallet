import { z } from '@dopecute/config';

export const SignInSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
});

export const SignUpSchema = z.object({
  name: z.string().min(1),
  email: z.string().email(),
  password: z.string().min(8),
});

export type SignInForm = z.infer<typeof SignInSchema>;
export type SignUpForm = z.infer<typeof SignUpSchema>;
