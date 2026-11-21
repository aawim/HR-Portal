window.hrmDeveloperTestClock = (() => {
    let nextId = 1;
    const listeners = new Map();

    return {
        subscribe(dotNetReference, storageKey) {
            const id = nextId++;
            const handler = event => {
                if (event.key === storageKey) {
                    dotNetReference.invokeMethodAsync("OnStorageChanged", event.newValue);
                }
            };

            window.addEventListener("storage", handler);
            listeners.set(id, handler);
            return id;
        },

        unsubscribe(id) {
            const handler = listeners.get(id);
            if (!handler) return;
            window.removeEventListener("storage", handler);
            listeners.delete(id);
        }
    };
})();
