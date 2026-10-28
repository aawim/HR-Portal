// Copy in the browser's click handler to retain clipboard user activation.
// Delegation also covers notifications rendered after navigation or polling.
document.addEventListener("click", async event => {
    const button = event.target instanceof Element ? event.target.closest("[data-notification-copy]") : null;
    if (!button) return;
    event.preventDefault();
    event.stopPropagation();
    const text = button.getAttribute("data-notification-copy") || "";
    const status = button.parentElement.querySelector("[data-notification-copy-status]");
    const copyFallback = () => {
        const field = document.createElement("textarea");
        field.value = text;
        field.setAttribute("readonly", "");
        field.style.cssText = "position:fixed;top:0;left:-9999px;opacity:0";
        document.body.appendChild(field);
        try {
            field.select();
            if (!document.execCommand("copy")) throw new Error("Clipboard unavailable");
        } finally {
            field.remove();
            button.focus({ preventScroll: true });
        }
    };
    try {
        if (navigator.clipboard && window.isSecureContext) {
            try { await navigator.clipboard.writeText(text); }
            catch { copyFallback(); }
        } else copyFallback();
        if (status) status.textContent = "Message copied.";
    } catch {
        if (status) status.textContent = "Copy unavailable. Select the message text and copy it manually.";
    }
});
