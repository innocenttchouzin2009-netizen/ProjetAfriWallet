const reviews = [
  { id: 1, author: 'Lina', rating: 5, comment: 'Très bonne qualité et rendu parfait !' },
  { id: 2, author: 'Tom', rating: 5, comment: 'Le service client était au top.' },
  { id: 3, author: 'Sofia', rating: 4, comment: 'Produit stylé, livraison rapide.' },
];

export default function ProductReviews() {
  return (
    <section className="space-y-6 rounded-[32px] border border-white/10 bg-white/5 p-8">
      <h2 className="text-3xl font-black">Avis clients</h2>
      <div className="space-y-4">
        {reviews.map((review) => (
          <div key={review.id} className="rounded-3xl border border-white/10 bg-white/5 p-6">
            <div className="flex items-center justify-between">
              <span className="font-semibold">{review.author}</span>
              <span className="text-sm text-[#C8A45C]">{review.rating} ★</span>
            </div>
            <p className="mt-3 text-white/70">{review.comment}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
