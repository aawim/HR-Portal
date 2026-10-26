# Personal font size

Open the employee/login menu → Settings → Font size. Available sizes: 90%, 100% (default), 110%, 120%, 130%. Selection applies immediately across the application and is loaded from the signed-in account on subsequent sessions.

`UserPreferences.FontSizePercent` is an integer with default 100. No new table is created. Apply the additive `UserFontSizePreference` migration before restarting the application.

The server validates the authenticated identity and supported values. Legacy preferences shared by accounts are copied before changing either theme or font size, preserving language, context and recent searches. Changing theme preserves font size and vice versa. Browser zoom remains available.
