/* HotelBooking UI Polish v1 – progressive enhancement only.
   Leaves MVC actions / API payloads / validation unchanged. */
(() => {
  "use strict";
  const noMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

  function enhanceStatuses() {
    const names = new Set(["PENDING", "CONFIRMED", "CHECKED_IN", "CHECKED_OUT", "CANCELLED"]);
    document.querySelectorAll(".status").forEach((element) => {
      const value = (element.textContent || "").trim().toUpperCase();
      if (names.has(value)) element.classList.add(`hotel-status--${value.toLowerCase()}`);
    });
  }

  // Soft ripple follows a mouse / touch press on clickable buttons.
  document.addEventListener("pointerdown", (event) => {
    if (noMotion.matches || !(event.target instanceof Element)) return;
    const button = event.target.closest(".btn, .operation-button, .btn-nav");
    if (!button || button.matches(":disabled, .disabled, .is-processing")) return;
    const rect = button.getBoundingClientRect();
    const ripple = document.createElement("span");
    ripple.className = "hotel-ripple";
    ripple.setAttribute("aria-hidden", "true");
    ripple.style.left = `${event.clientX - rect.left}px`;
    ripple.style.top = `${event.clientY - rect.top}px`;
    button.appendChild(ripple);
    ripple.addEventListener("animationend", () => ripple.remove(), { once: true });
  }, { passive: true });

  // POST forms: show a loading indicator and guard against accidental double submit.
  // Native and jQuery validation must pass before the visual lock is applied.
  document.addEventListener("submit", (event) => {
    if (event.defaultPrevented) return;
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) return;
    if ((form.getAttribute("method") || "GET").toUpperCase() !== "POST") return;
    if (!form.checkValidity()) return;

    if (window.jQuery && typeof window.jQuery.fn.valid === "function" &&
        window.jQuery(form).data("validator") && !window.jQuery(form).valid()) return;

    if (form.dataset.hotelSubmitting === "true") {
      event.preventDefault();
      return;
    }
    const submitter = event.submitter || form.querySelector('button[type="submit"], input[type="submit"]');
    if (!(submitter instanceof HTMLElement)) return;
    form.dataset.hotelSubmitting = "true";
    submitter.classList.add("is-processing");
    submitter.setAttribute("aria-busy", "true");
    // Do not set disabled: MVC must be allowed to submit the form normally.
  });

  // Browser back/forward cache can restore a previously submitted document.
  window.addEventListener("pageshow", () => {
    document.querySelectorAll('form[data-hotel-submitting="true"]').forEach((form) => {
      delete form.dataset.hotelSubmitting;
      form.querySelectorAll(".is-processing").forEach((button) => {
        button.classList.remove("is-processing");
        button.removeAttribute("aria-busy");
      });
    });
  });

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", enhanceStatuses, { once: true });
  } else {
    enhanceStatuses();
  }
})();
