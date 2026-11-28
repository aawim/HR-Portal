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
        if (element._hrmChatPaste) {
            element.removeEventListener("paste", element._hrmChatPaste);
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
                const hasText = element.value && element.value.trim().length > 0;
                const hasFiles = element.dataset && element.dataset.hasAttachments === "true";
                if (hasText || hasFiles) {
                    dotNetRef.invokeMethodAsync("SubmitFromKeyboardAsync");
                }
            }
        };
        const onPaste = (e) => {
            const items = e.clipboardData && e.clipboardData.items;
            if (!items) return;
            for (let i = 0; i < items.length; i++) {
                const item = items[i];
                if (item.kind === "file" && item.type && item.type.startsWith("image/")) {
                    const file = item.getAsFile();
                    if (!file) continue;
                    e.preventDefault();
                    if (file.size > 5 * 1024 * 1024) {
                        dotNetRef.invokeMethodAsync("OnClipboardPasteErrorAsync", "Pasted screenshot exceeds the 5 MB file size limit.").catch(() => { });
                        return;
                    }
                    const reader = new FileReader();
                    reader.onload = () => {
                        const result = typeof reader.result === "string" ? reader.result : "";
                        const ext = (file.type.split("/")[1] || "png").replace("jpeg", "jpg");
                        const stamp = new Date().toISOString().replace(/[:.]/g, "-").slice(0, 19);
                        const fileName = file.name && file.name !== "image.png" ? file.name : `screenshot-${stamp}.${ext}`;
                        dotNetRef.invokeMethodAsync("OnClipboardImagePastedAsync", fileName, file.type, result).catch(() => { });
                    };
                    reader.readAsDataURL(file);
                    return;
                }
            }
        };
        element._hrmChatKeydown = onKeydown;
        element._hrmChatInput = autoResize;
        element._hrmChatPaste = onPaste;
        element.addEventListener("keydown", onKeydown);
        element.addEventListener("input", autoResize);
        element.addEventListener("paste", onPaste);
        autoResize();
    },
    resetComposerHeight(element) {
        if (!element) return;
        element.style.height = "auto";
        element.style.overflowY = "hidden";
    },
    downloadFile(fileName, contentType, base64) {
        if (!base64) return;
        const bin = atob(base64);
        const len = bin.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) bytes[i] = bin.charCodeAt(i);
        const blob = new Blob([bytes], { type: contentType || "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "attachment";
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 5000);
    }
};
