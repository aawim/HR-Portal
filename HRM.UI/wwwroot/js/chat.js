window.hrmChat = {
    isVisible() { return document.visibilityState === "visible"; },
    scrollToLatest(element, force) {
        if (element && (force || element.scrollHeight - element.scrollTop - element.clientHeight < 180)) {
            element.scrollTop = element.scrollHeight;
        }
    },
    bindComposer(element, dotNetRef) {
        if (!element || !dotNetRef) return;
        if (element._hrmChatKeydown) {
            element.removeEventListener("keydown", element._hrmChatKeydown);
        }
        if (element._hrmChatInput) {
            element.removeEventListener("input", element._hrmChatInput);
        }
        const autoResize = () => {
            element.style.height = "auto";
            const max = 128;
            element.style.height = Math.min(element.scrollHeight, max) + "px";
            element.style.overflowY = element.scrollHeight > max ? "auto" : "hidden";
        };
        const onKeydown = (e) => {
            if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
                if (element.value && element.value.trim().length > 0) {
                    dotNetRef.invokeMethodAsync("SubmitFromKeyboardAsync");
                }
            }
        };
        element._hrmChatKeydown = onKeydown;
        element._hrmChatInput = autoResize;
        element.addEventListener("keydown", onKeydown);
        element.addEventListener("input", autoResize);
        autoResize();
    },
    resetComposerHeight(element) {
        if (!element) return;
        element.style.height = "auto";
        element.style.overflowY = "hidden";
    }
};
