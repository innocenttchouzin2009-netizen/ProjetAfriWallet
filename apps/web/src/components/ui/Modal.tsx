type ModalProps = {
  title?: string;
  open: boolean;
  onClose: () => void;
  children: React.ReactNode;
};

export default function Modal({ title, open, onClose, children }: ModalProps) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
      <div className="w-full max-w-2xl rounded-[32px] bg-[#0D0D0D] p-8 shadow-2xl">
        <div className="mb-6 flex items-center justify-between">
          <div>
            {title && <h2 className="text-2xl font-bold">{title}</h2>}
          </div>
          <button onClick={onClose} className="rounded-full bg-white/10 px-4 py-2 text-white">
            Fermer
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
