type DrawerProps = {
  open: boolean;
  onClose: () => void;
  children: React.ReactNode;
};

export default function Drawer({ open, onClose, children }: DrawerProps) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex bg-black/50">
      <div className="ml-auto h-full w-full max-w-md rounded-l-[32px] bg-[#0D0D0D] p-6 shadow-2xl">
        <button onClick={onClose} className="mb-6 rounded-full bg-white/10 px-4 py-2 text-white">
          Fermer
        </button>
        {children}
      </div>
    </div>
  );
}
