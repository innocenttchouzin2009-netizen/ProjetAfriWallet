"use client";

import { useEffect } from 'react';

type CartToastProps = {
  message: string;
  visible: boolean;
  onClose: () => void;
};

export default function CartToast({ message, visible, onClose }: CartToastProps) {
  useEffect(() => {
    if (!visible) return;

    const timeout = window.setTimeout(() => onClose(), 2400);
    return () => window.clearTimeout(timeout);
  }, [visible, onClose]);

  if (!visible) return null;

  return (
    <div className="fixed bottom-6 right-6 z-[100] animate-[fadeIn_0.25s_ease-out]">
      <div className="flex items-center gap-3 rounded-full border border-[#C8A45C]/40 bg-gradient-to-r from-[#0D0D0D] via-[#161616] to-[#0D0D0D] px-5 py-3.5 text-sm font-semibold text-[#F6E5B1] shadow-[0_0_0_1px_rgba(255,255,255,0.04),0_0_24px_rgba(200,164,92,0.16),0_14px_40px_rgba(0,0,0,0.45)] backdrop-blur">
        <div className="flex h-7 w-7 items-center justify-center rounded-full bg-[#C8A45C] text-sm font-black text-black shadow-[0_0_16px_rgba(200,164,92,0.35)]">
          ✓
        </div>
        <span>{message}</span>
      </div>
    </div>
  );
}
