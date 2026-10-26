(() => {
    const allowed = [90, 100, 110, 120, 130];
    let current = 100;
    function apply(value) {
        current = allowed.includes(Number(value)) ? Number(value) : 100;
        document.documentElement.style.fontSize = `${current}%`;
        document.documentElement.dataset.fontSize = String(current);
    }
    window.hrmFontSize = {
        initialize(value) { apply(value); },
        set(value) { apply(value); },
        get() { return current; }
    };
})();
