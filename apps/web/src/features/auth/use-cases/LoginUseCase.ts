import { AuthService } from '../services/auth.service';
import type { LoginPayload } from '../types/auth.types';

export class LoginUseCase {
  constructor(private readonly service = new AuthService()) {}

  async execute(payload: LoginPayload) {
    return this.service.login(payload);
  }
}
