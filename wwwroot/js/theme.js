// Apply before styles load to avoid a light flash on navigation or reload.
(() => {
    const key = "hrm.colour-theme";
    const valid = value => value === "light" || value === "dark";
    let theme = "dark";
    let userKey = null;
    try {
        const saved = localStorage.getItem(key);
        if (valid(saved)) theme = saved;
    } catch { /* Private browsing may disable storage; toggling still works. */ }

    function updateButtons() {
        document.querySelectorAll("[data-theme-toggle]").forEach(button => {
            button.setAttribute("aria-pressed", String(theme === "dark"));
            const label = `Switch to ${theme === "dark" ? "light" : "dark"} mode`;
            button.setAttribute("aria-label", label);
            button.setAttribute("title", label);
        });
    }

    function apply(value) {
        theme = valid(value) ? value : "dark";
        document.documentElement.dataset.theme = theme;
        document.documentElement.setAttribute("data-bs-theme", theme);
        updateButtons();
    }

    apply(theme);
    // Authenticated settings come from UserPreferences. Browser storage is only
    // a first-paint hint and a guest preference; it never overrides the database.
    window.hrmTheme = {
        initialize(serverTheme, userId) {
            userKey = userId == null ? null : `${key}.user.${userId}`;
            if (userKey) {
                apply(serverTheme);
                try { localStorage.setItem(key, theme); } catch { }
            }
            updateButtons();
        },
        get() { return theme; },
        set(value) {
            apply(value);
            try {
                localStorage.setItem(key, theme);
                if (userKey) localStorage.setItem(userKey, theme);
            } catch { /* Keep the current tab usable. */ }
        }
    };
    window.addEventListener("storage", event => {
        if (event.key === (userKey || key)) apply(event.newValue);
    });
    // Blazor can replace the topbar during interactive navigation.
    const observer = new MutationObserver(records => {
        if (records.some(record => Array.from(record.addedNodes).some(node =>
            node instanceof Element && (node.matches("[data-theme-toggle]") || node.querySelector("[data-theme-toggle]"))))) updateButtons();
    });
    observer.observe(document.documentElement, { childList: true, subtree: true });
})();
