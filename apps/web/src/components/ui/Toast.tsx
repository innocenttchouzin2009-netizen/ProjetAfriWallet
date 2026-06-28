import { useEffect } from 'react';

type ToastProps = {
  message: string;
  duration?: number;
  onClose: () => void;
};

export default function Toast({ message, duration = 3000, onClose }: ToastProps) {
  useEffect(() => {
    const timer = window.setTimeout(onClose, duration);
    return () => window.clearTimeout(timer);
  }, [duration, onClose]);

  return (
    <div className="fixed bottom-6 right-6 rounded-3xl bg-[#0f172a] px-5 py-4 text-sm text-white shadow-2xl">
      {message}
    </div>
  );
}
