import { AuthService } from '../services/auth.service';
import type { ResetPasswordPayload } from '../types/auth.types';

export class ResetPasswordUseCase {
  constructor(private readonly service = new AuthService()) {}

  async execute(payload: ResetPasswordPayload) {
    return this.service.resetPassword(payload);
  }
}
