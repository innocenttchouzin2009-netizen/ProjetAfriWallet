type PaginationProps = {
  currentPage: number;
  totalPages: number;
  onChange: (page: number) => void;
};

export default function Pagination({ currentPage, totalPages, onChange }: PaginationProps) {
  return (
    <div className="flex items-center gap-2">
      <button
        onClick={() => onChange(Math.max(1, currentPage - 1))}
        className="rounded-full bg-white/10 px-4 py-2 text-sm text-white transition hover:bg-white/20"
      >
        Précédent
      </button>
      <span className="text-sm text-white/70">
        {currentPage} / {totalPages}
      </span>
      <button
        onClick={() => onChange(Math.min(totalPages, currentPage + 1))}
        className="rounded-full bg-white/10 px-4 py-2 text-sm text-white transition hover:bg-white/20"
      >
        Suivant
      </button>
    </div>
  );
}
