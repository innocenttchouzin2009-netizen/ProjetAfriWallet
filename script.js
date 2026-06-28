const products = [
  {
    id: 1,
    name: "Casquette " + "Urban",
    price: 29,
    description: "Casquette streetwear légère avec logo minimal.",
    image: "https://images.unsplash.com/photo-1512436991641-6745cdb1723f?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: 2,
    name: "Chapeau " + "Fedora",
    price: 59,
    description: "Fedora élégant en feutre pour un style raffiné.",
    image: "https://images.unsplash.com/photo-1514996937319-344454492b37?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: 3,
    name: "Béret " + "Chic",
    price: 35,
    description: "Béret doux et vintage, parfait pour une touche artistique.",
    image: "https://images.unsplash.com/photo-1531733875087-95f97fe2347e?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: 4,
    name: "Bucket Hat",
    price: 42,
    description: "Bucket hat moderne pour une allure détente et urbaine.",
    image: "https://images.unsplash.com/photo-1520975915454-950996c18bef?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: 5,
    name: "Panama",
    price: 65,
    description: "Panama léger idéal pour les escapades ensoleillées.",
    image: "https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: 6,
    name: "Trucker",
    price: 27,
    description: "Casquette trucker classique avec filets respirants.",
    image: "https://images.unsplash.com/photo-1541099649105-f69ad21f3246?auto=format&fit=crop&w=800&q=80"
  }
];

const productGrid = document.getElementById("productGrid");
const cartButton = document.getElementById("cartButton");
const contactForm = document.getElementById("contactForm");
let cartCount = 0;

function renderProducts() {
  products.forEach((product) => {
    const card = document.createElement("article");
    card.className = "product-card";

    card.innerHTML = `
      <img src="${product.image}" alt="${product.name}" />
      <h3>${product.name}</h3>
      <p>${product.description}</p>
      <div class="product-meta">
        <span>${product.price}€</span>
        <button data-id="${product.id}">Ajouter</button>
      </div>
    `;

    card.querySelector("button").addEventListener("click", () => addToCart(product));
    productGrid.appendChild(card);
  });
}

function updateCartLabel() {
  cartButton.textContent = `Panier (${cartCount})`;
}

function addToCart(product) {
  cartCount += 1;
  updateCartLabel();
  alert(`Ajouté au panier : ${product.name}`);
}

contactForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const name = document.getElementById("name").value.trim();
  const email = document.getElementById("email").value.trim();
  const message = document.getElementById("message").value.trim();

  if (!name || !email || !message) {
    alert("Veuillez remplir tous les champs.");
    return;
  }

  alert(`Merci ${name}! Votre message a bien été envoyé.`);
  contactForm.reset();
});

renderProducts();
updateCartLabel();
