"use strict";

document.querySelectorAll("[data-copy-page-url]").forEach((button) => {
  const status = button.parentElement.querySelector("[data-copy-status]");

  function report(message) {
    if (status) status.textContent = message;
  }

  function copyWithSelection(value) {
    const field = document.createElement("textarea");
    field.value = value;
    field.setAttribute("readonly", "");
    field.style.position = "fixed";
    field.style.opacity = "0";
    document.body.appendChild(field);
    field.select();
    const copied = document.execCommand("copy");
    document.body.removeChild(field);
    return copied;
  }

  button.addEventListener("click", async () => {
    const pageUrl = window.location.href;

    try {
      if (navigator.clipboard && window.isSecureContext) {
        try {
          await navigator.clipboard.writeText(pageUrl);
          report("Page link copied.");
          return;
        } catch (_) {
          // Try the selection-based fallback when clipboard access is denied.
        }
      }

      if (!copyWithSelection(pageUrl)) throw new Error("Copy command failed");
      report("Page link copied.");
    } catch (_) {
      report("Copy failed. Use the address shown in your browser.");
    }
  });
});
