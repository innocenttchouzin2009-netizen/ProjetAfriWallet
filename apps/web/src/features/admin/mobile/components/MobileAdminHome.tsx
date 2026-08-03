"use client";

import { useCallback, useEffect, useMemo, useState } from 'react';
import MobileOrderList from './MobileOrderList';
import MobileProductEditor from './MobileProductEditor';
import MobilePromotionEditor from './MobilePromotionEditor';

type MobileView = 'home' | 'products' | 'promotions' | 'orders';
type MobileRole = 'super-admin' | 'manager' | 'production' | 'vendor' | 'support' | 'client';

const CARD_BASE = 'rounded-[24px] border border-black/10 bg-white/90 p-4 shadow-[0_10px_24px_rgba(0,0,0,0.08)]';

interface MobileAdminHomeProps {
  role: MobileRole;
}

export default function MobileAdminHome({ role }: MobileAdminHomeProps) {
  const [view, setView] = useState<MobileView>('home');
  const [phoneBaseUrl, setPhoneBaseUrl] = useState('');
  const [copyMessage, setCopyMessage] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'checking' | 'ok' | 'error'>('checking');
  const [connectionMessage, setConnectionMessage] = useState('Vérification en cours...');
  const [ipCandidates, setIpCandidates] = useState<string[]>([]);
  const [ipLoading, setIpLoading] = useState(false);
  const [ipError, setIpError] = useState<string | null>(null);

  const canManageCatalog = role === 'super-admin' || role === 'manager';
  const canManageOrders = role === 'super-admin' || role === 'manager' || role === 'production' || role === 'support';

  useEffect(() => {
    if (typeof window === 'undefined') return;
    setPhoneBaseUrl(window.location.origin);
  }, []);

  useEffect(() => {
    if (view === 'products' && !canManageCatalog) {
      setView('home');
    }
    if (view === 'promotions' && !canManageCatalog) {
      setView('home');
    }
    if (view === 'orders' && !canManageOrders) {
      setView('home');
    }
  }, [view, canManageCatalog, canManageOrders]);

  const verifyConnection = useCallback(async () => {
    setConnectionStatus('checking');
    setConnectionMessage('Vérification en cours...');

    try {
      const response = await fetch('/api/admin/dashboard?range=week', { cache: 'no-store' });
      if (!response.ok) {
        throw new Error('Connexion admin refusée');
      }
      setConnectionStatus('ok');
      setConnectionMessage('Connexion mobile OK');
    } catch {
      setConnectionStatus('error');
      setConnectionMessage('Impossible de joindre l\'admin depuis ce mobile');
    }
  }, []);

  useEffect(() => {
    void verifyConnection();
  }, [verifyConnection]);

  const detectLocalIps = useCallback(async () => {
    setIpLoading(true);
    setIpError(null);

    try {
      const response = await fetch('/api/admin/mobile/network-info', { cache: 'no-store' });
      if (!response.ok) {
        throw new Error('Détection IP indisponible');
      }

      const data = (await response.json()) as { originCandidates?: string[] };
      const candidates = data.originCandidates ?? [];
      setIpCandidates(candidates);

      if (candidates.length > 0) {
        setPhoneBaseUrl((current) => current || candidates[0]);
      }
    } catch {
      setIpError('Aucune IP détectée automatiquement.');
    } finally {
      setIpLoading(false);
    }
  }, []);

  useEffect(() => {
    void detectLocalIps();
  }, [detectLocalIps]);

  const phoneAdminUrl = `${phoneBaseUrl.replace(/\/$/, '')}/admin/mobile`;
  const qrUrl = phoneBaseUrl
    ? `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(phoneAdminUrl)}`
    : '';

  const title = useMemo(() => {
    if (view === 'products') return 'Produits';
    if (view === 'promotions') return 'Promotions';
    if (view === 'orders') return 'Commandes';
    return 'Admin Mobile';
  }, [view]);

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,_#eaf2ff_0%,_#f7f8fc_46%,_#eceff5_100%)] px-4 pb-28 pt-6 text-[#1f2a38]">
      <section className="mx-auto w-full max-w-md">
        <header className="mb-4 rounded-[26px] border border-white/70 bg-white/80 p-4 shadow-[0_12px_28px_rgba(18,24,40,0.10)] backdrop-blur">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[#6f7f97]">Dope&Cute Studio</p>
          <h1 className="mt-2 text-2xl font-black text-[#182235]">{title}</h1>
          <p className="mt-1 text-sm text-[#526176]">Interface optimisée téléphone style iOS.</p>
          <div className={`mt-3 inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-semibold ${connectionStatus === 'ok' ? 'border-[#4db36a]/30 bg-[#4db36a]/12 text-[#1d7f3d]' : connectionStatus === 'error' ? 'border-[#db5d53]/30 bg-[#db5d53]/12 text-[#9c2c24]' : 'border-[#1f7aff]/30 bg-[#1f7aff]/10 text-[#1f7aff]'}`}>
            <span className={`inline-block h-2 w-2 rounded-full ${connectionStatus === 'ok' ? 'bg-[#2aa84f]' : connectionStatus === 'error' ? 'bg-[#d13d33]' : 'bg-[#1f7aff]'}`} />
            {connectionMessage}
            {connectionStatus === 'error' ? (
              <button
                onClick={() => void verifyConnection()}
                className="rounded-full border border-current/30 px-2 py-0.5"
              >
                Réessayer
              </button>
            ) : null}
          </div>
        </header>

        {view === 'home' ? (
          <div className="space-y-3">
            {canManageCatalog ? (
              <>
                <button onClick={() => setView('products')} className={`${CARD_BASE} w-full text-left transition active:scale-[0.99]`}>
                  <p className="text-sm font-semibold text-[#6a7a92]">Catalogue</p>
                  <p className="mt-1 text-lg font-bold">Ajouter et publier des produits</p>
                  <p className="mt-1 text-sm text-[#5d6f87]">Upload photos depuis galerie/caméra, prix, stock, collection.</p>
                </button>

                <button onClick={() => setView('promotions')} className={`${CARD_BASE} w-full text-left transition active:scale-[0.99]`}>
                  <p className="text-sm font-semibold text-[#6a7a92]">Promotions</p>
                  <p className="mt-1 text-lg font-bold">Créer des offres et codes promo</p>
                  <p className="mt-1 text-sm text-[#5d6f87]">Pourcentage, montant fixe, période de validité.</p>
                </button>
              </>
            ) : (
              <div className={`${CARD_BASE} bg-[#f5f8ff]`}>
                <p className="text-sm font-semibold text-[#607089]">Accès catalogue restreint</p>
                <p className="mt-1 text-sm text-[#5d6f87]">Ton rôle actuel ne permet pas d&apos;éditer les produits/promotions.</p>
              </div>
            )}

            {canManageOrders ? (
              <button onClick={() => setView('orders')} className={`${CARD_BASE} w-full text-left transition active:scale-[0.99]`}>
                <p className="text-sm font-semibold text-[#6a7a92]">Commandes</p>
                <p className="mt-1 text-lg font-bold">Changer les statuts rapidement</p>
                <p className="mt-1 text-sm text-[#5d6f87]">Passer de CONFIRMED à SHIPPED/DELIVERED en mobile.</p>
              </button>
            ) : null}

            <div className={`${CARD_BASE}`}>
              <p className="text-sm font-semibold text-[#6a7a92]">Accès téléphone</p>
              <p className="mt-1 text-sm text-[#5d6f87]">Mets l&apos;IP locale de ton PC (ex: http://192.168.1.24:3000), puis scanne.</p>

              <div className="mt-3 rounded-2xl border border-black/10 bg-[#f8fbff] p-3">
                <p className="text-xs font-semibold uppercase tracking-[0.1em] text-[#6d7f97]">Onboarding rapide</p>
                <p className="mt-1 text-xs text-[#5d6f87]">1) Détecte ton IP 2) Ouvre le lien 3) Connecte-toi avec un rôle admin.</p>
                <button
                  onClick={() => void detectLocalIps()}
                  disabled={ipLoading}
                  className="mt-2 rounded-full border border-[#1f7aff]/30 bg-[#1f7aff]/10 px-3 py-2 text-xs font-semibold text-[#1f7aff] disabled:opacity-60"
                >
                  {ipLoading ? 'Détection...' : 'Détecter mon IP locale'}
                </button>
                {ipCandidates.length ? (
                  <div className="mt-2 flex flex-wrap gap-2">
                    {ipCandidates.map((candidate) => (
                      <button
                        key={candidate}
                        onClick={() => {
                          setPhoneBaseUrl(candidate);
                          setCopyMessage(null);
                        }}
                        className="rounded-full border border-black/10 bg-white px-3 py-1 text-xs font-semibold text-[#4d5f79]"
                      >
                        Utiliser {candidate}
                      </button>
                    ))}
                  </div>
                ) : null}
                {ipError ? <p className="mt-2 text-xs text-[#9c2c24]">{ipError}</p> : null}
              </div>

              <input
                value={phoneBaseUrl}
                onChange={(event) => {
                  setPhoneBaseUrl(event.target.value);
                  setCopyMessage(null);
                }}
                placeholder="http://192.168.x.x:3000"
                className="mt-3 w-full rounded-2xl border border-black/10 bg-[#f4f7fc] px-4 py-3 text-sm"
              />

              <div className="mt-3 rounded-2xl border border-black/10 bg-[#f8fbff] p-3">
                <p className="break-all text-xs font-semibold text-[#245084]">{phoneAdminUrl}</p>
                <button
                  onClick={async () => {
                    try {
                      await navigator.clipboard.writeText(phoneAdminUrl);
                      setCopyMessage('Lien copié.');
                    } catch {
                      setCopyMessage('Copie impossible sur ce navigateur.');
                    }
                  }}
                  className="mt-2 rounded-full border border-[#1f7aff]/30 bg-[#1f7aff]/10 px-3 py-2 text-xs font-semibold text-[#1f7aff]"
                >
                  Copier le lien
                </button>
                {copyMessage ? <p className="mt-2 text-xs text-[#607089]">{copyMessage}</p> : null}
              </div>

              {qrUrl ? (
                /* eslint-disable-next-line @next/next/no-img-element */
                <img src={qrUrl} alt="QR accès admin mobile" className="mx-auto mt-3 h-44 w-44 rounded-2xl border border-black/10 bg-white p-2" />
              ) : null}
            </div>
          </div>
        ) : null}

        {view === 'products' ? <MobileProductEditor onBack={() => setView('home')} /> : null}
        {view === 'promotions' ? <MobilePromotionEditor onBack={() => setView('home')} /> : null}
        {view === 'orders' ? <MobileOrderList onBack={() => setView('home')} /> : null}
      </section>

      <nav className="fixed bottom-4 left-1/2 z-20 w-[calc(100%-1.5rem)] max-w-md -translate-x-1/2 rounded-[26px] border border-white/70 bg-white/85 p-2 shadow-[0_16px_34px_rgba(22,28,45,0.18)] backdrop-blur">
        <div className="grid grid-cols-4 gap-2 text-xs font-semibold">
          <button onClick={() => setView('home')} className={`rounded-[18px] px-3 py-2 ${view === 'home' ? 'bg-[#1f7aff] text-white' : 'text-[#607089]'}`}>Accueil</button>
          <button onClick={() => canManageCatalog && setView('products')} className={`rounded-[18px] px-3 py-2 ${view === 'products' ? 'bg-[#1f7aff] text-white' : 'text-[#607089]'} ${canManageCatalog ? '' : 'opacity-40'}`}>Produits</button>
          <button onClick={() => canManageCatalog && setView('promotions')} className={`rounded-[18px] px-3 py-2 ${view === 'promotions' ? 'bg-[#1f7aff] text-white' : 'text-[#607089]'} ${canManageCatalog ? '' : 'opacity-40'}`}>Promos</button>
          <button onClick={() => canManageOrders && setView('orders')} className={`rounded-[18px] px-3 py-2 ${view === 'orders' ? 'bg-[#1f7aff] text-white' : 'text-[#607089]'} ${canManageOrders ? '' : 'opacity-40'}`}>Commandes</button>
        </div>
      </nav>
    </main>
  );
}
