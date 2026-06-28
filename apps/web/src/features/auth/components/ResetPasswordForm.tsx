"use client";

import { useState } from 'react';
import { useAuth } from '../hooks/useAuth';

export default function ResetPasswordForm() {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const { resetPassword, loading, error } = useAuth();

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    await resetPassword({ password, confirmPassword });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-[32px] border border-white/10 bg-white/5 p-8">
      <div>
        <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Nouveau mot de passe</p>
        <h2 className="mt-2 text-3xl font-black text-white">Choisis un nouveau mot de passe</h2>
      </div>

      {error ? <p className="rounded-full border border-[#F0B86E]/40 bg-[#F0B86E]/10 px-4 py-3 text-sm text-[#F0B86E]">{error}</p> : null}

      <label className="block text-sm text-white/70">
        Nouveau mot de passe
        <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" required />
      </label>

      <label className="block text-sm text-white/70">
        Confirmer
        <input type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" required />
      </label>

      <button type="submit" disabled={loading} className="w-full rounded-full bg-[#C8A45C] px-5 py-3 font-semibold text-black disabled:opacity-60">
        {loading ? 'Enregistrement…' : 'Enregistrer'}
      </button>
    </form>
  );
}
