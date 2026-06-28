"use client";

import { useState } from 'react';
import Link from 'next/link';
import { useAuth } from '../hooks/useAuth';

export default function RegisterForm() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const { register, loading, error } = useAuth();

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    await register({ name, email, password });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-[32px] border border-white/10 bg-white/5 p-8">
      <div>
        <p className="text-sm uppercase tracking-[0.3em] text-[#C8A45C]">Créer un compte</p>
        <h2 className="mt-2 text-3xl font-black text-white">Rejoins Dope&Cute</h2>
      </div>

      {error ? <p className="rounded-full border border-[#F0B86E]/40 bg-[#F0B86E]/10 px-4 py-3 text-sm text-[#F0B86E]">{error}</p> : null}

      <label className="block text-sm text-white/70">
        Nom
        <input value={name} onChange={(event) => setName(event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" placeholder="Mina Laurent" required />
      </label>

      <label className="block text-sm text-white/70">
        Email
        <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" placeholder="you@dopecute.studio" required />
      </label>

      <label className="block text-sm text-white/70">
        Mot de passe
        <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="mt-2 w-full rounded-full border border-white/10 bg-black/30 px-4 py-3 text-white" placeholder="Minimum 8 caractères" required />
      </label>

      <button type="submit" disabled={loading} className="w-full rounded-full bg-[#C8A45C] px-5 py-3 font-semibold text-black disabled:opacity-60">
        {loading ? 'Création…' : 'Créer mon compte'}
      </button>

      <p className="text-sm text-white/60">
        Déjà inscrit ? <Link href="/login" className="text-[#C8A45C]">Se connecter</Link>
      </p>
    </form>
  );
}
