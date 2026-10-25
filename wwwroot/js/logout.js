export async function submitLogout() {
    const tokenUrl = new URL("Online/Auth/LogoutToken", document.baseURI);
    const logoutUrl = new URL("Online/Auth/Logout", document.baseURI);
    if (tokenUrl.origin !== location.origin || logoutUrl.origin !== location.origin) {
        throw new Error("Invalid sign-out destination.");
    }

    const response = await fetch(tokenUrl, {
        credentials: "same-origin",
        mode: "same-origin",
        cache: "no-store",
        headers: { Accept: "application/json" }
    });
    if (!response.ok) throw new Error("Could not prepare sign-out.");
    const token = await response.json();
    if (typeof token.formFieldName !== "string" || !token.formFieldName ||
        typeof token.requestToken !== "string" || !token.requestToken) {
        throw new Error("Sign-out token is unavailable.");
    }

    // Use a browser POST, not a circuit or enhanced-navigation request.
    // The preceding HTTP response has already stored the matching cookie.
    const form = document.createElement("form");
    form.method = "post";
    form.action = logoutUrl.href;
    form.hidden = true;
    const field = document.createElement("input");
    field.type = "hidden";
    field.name = token.formFieldName;
    field.value = token.requestToken;
    form.append(field);
    document.body.append(form);
    HTMLFormElement.prototype.submit.call(form);
}
