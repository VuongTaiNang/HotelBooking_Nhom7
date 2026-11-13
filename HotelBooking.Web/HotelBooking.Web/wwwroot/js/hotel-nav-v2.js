/* Navbar v2: cosmetic scroll depth, no network requests or form interception. */
(() => {
  'use strict';
  const header = document.getElementById('hotelHeader');
  if (!header) return;
  const update = () => header.classList.toggle('is-scrolled', window.scrollY > 10);
  update();
  window.addEventListener('scroll', update, { passive: true });
})();
