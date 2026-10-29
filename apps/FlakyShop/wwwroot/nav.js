// Shared header and a tiny fetch helper for every page.
document.body.insertAdjacentHTML('afterbegin', `
  <nav aria-label="Main">
    <a href="/">Flaky Shop</a>
    <a href="/orders">Orders</a>
    <a href="/catalog">Catalog</a>
    <a href="/tasks">Team tasks</a>
    <a href="/checkout">Checkout</a>
    <a href="/search">Search</a>
  </nav>`);

async function api(path, options) {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!response.ok) {
    throw new Error(`${path} returned ${response.status}`);
  }
  return response.json();
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}
