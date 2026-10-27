window.hrmChat = {
    isVisible() { return document.visibilityState === "visible"; },
    scrollToLatest(element, force) {
        if (element && (force || element.scrollHeight - element.scrollTop - element.clientHeight < 180)) element.scrollTop = element.scrollHeight;
    }
};
