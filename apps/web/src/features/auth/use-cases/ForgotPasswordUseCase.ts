import { AuthService } from '../services/auth.service';
import type { ForgotPasswordPayload } from '../types/auth.types';

export class ForgotPasswordUseCase {
  constructor(private readonly service = new AuthService()) {}

  async execute(payload: ForgotPasswordPayload) {
    return this.service.forgotPassword(payload);
  }
}
