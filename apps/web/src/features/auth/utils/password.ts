import { compare, hash } from 'bcryptjs';

const SALT_ROUNDS = 12;

export async function hashPassword(plainPassword: string): Promise<string> {
  if (!plainPassword || plainPassword.trim().length < 8) {
    throw new Error('Password must be at least 8 characters long.');
  }

  return hash(plainPassword, SALT_ROUNDS);
}

export async function verifyPassword(
  plainPassword: string,
  hashedPassword: string,
): Promise<boolean> {
  if (!plainPassword || !hashedPassword) {
    return false;
  }

  return compare(plainPassword, hashedPassword);
}
