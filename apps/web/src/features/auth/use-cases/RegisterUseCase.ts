import { AuthService } from '../services/auth.service';
import type { RegisterPayload } from '../types/auth.types';

export class RegisterUseCase {
  constructor(private readonly service = new AuthService()) {}

  async execute(payload: RegisterPayload) {
    return this.service.register(payload);
  }
}
