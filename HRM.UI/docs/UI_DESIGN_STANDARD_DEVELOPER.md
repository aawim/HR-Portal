# HRM.UI Design Standard — Developer Portal (Workflows, Telemetry & Diagnostics)

> **Parent Standard**: Inherits all base spacing (`p-2`), header, Full Width / Shrink View, form, card, table, and icon rules from [`UI_DESIGN_STANDARD.md`](./UI_DESIGN_STANDARD.md).
> **Route Scope**: `/developer` and `/developer/*` (including `/developer/audit-logs` and `/developer/activity-logs`). Cross-portal navigation to `/online`, `/admin`, or `/superadmin` is restricted; use the Portal Switcher.
> **Shell Class**: `.portal-developer`
> **Primary Visual Reference**: `/developer/overview` (`AdminDeveloperPage.razor`)

---

## 1. Portal Color Theme — Warm Amber / Burnt Orange (Existing Theme Preserved)

| Token / Role | Light Mode | Dark Mode | Usage |
| :--- | :--- | :--- | :--- |
| **Sidebar Background** | `#92400e` | `#451a03` | Fixed left navigation rail (`.portal-developer .app-sidebar`) |
| **Sidebar Active Left Accent** | `3px solid #fbbf24` | `3px solid #fbbf24` | Active sidebar item indicator (`.portal-developer .app-nav-active`) |
| **Topbar Accent Border** | `3px solid #d97706` | `3px solid #d97706` | Bottom border of the slim top header |
| **Primary Accent / Active Tab** | `#d97706` (`bg-amber-600`) | `dark:bg-amber-600` | Active folder tab fill, loading spinners (`border-amber-500` / `border-amber-600`), Portal Switcher pill (`#ea580c`) |
| **Inactive Tab Text / Hover** | `text-amber-800` / `hover:bg-amber-50` | `dark:text-slate-300` / `dark:hover:bg-slate-700` | Inactive folder tab text and hover tint |
| **Tab Baseline Rule** | `border-b-2 border-amber-600` | `dark:border-amber-500` | Full-width baseline under `.developer-workspace-tabs` |
| **Context Pill Badge** | `bg-amber-50 border-amber-200 text-amber-700` (dot: `bg-amber-500`) | `dark:bg-amber-950/60 dark:border-amber-800 dark:text-amber-300` | Inline status/context badge beside the page title |
| **Scrollbar Track / Thumb** | `#fff7ed` / `#ea580c` (hover `#c2410c`) | `#431407` / `#f97316` (hover `#fb923c`) | `.portal-developer` custom scrollbar tokens |

---

## 2. Canonical Header Snippet (Developer Portal)

```html
<header class="developer-workspace-header flex flex-wrap items-center justify-between gap-2 px-1 py-0.5">
    <div class="min-w-0">
        <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-slate-100 truncate">Developer Workspace</h1>
            <span class="inline-flex items-center gap-1 rounded-full bg-amber-50 dark:bg-amber-950/60 border border-amber-200/80 dark:border-amber-800 px-2 py-0.5 text-[10px] font-semibold text-amber-700 dark:text-amber-300">
                <span class="h-1.5 w-1.5 rounded-full bg-amber-500"></span>
                Developer Portal
            </span>
        </div>
        <p class="text-xs text-slate-500 dark:text-slate-400">Centralized configuration for request types, state transition rules, approval workflows, and system diagnostics.</p>
    </div>
    <div class="flex flex-wrap items-center gap-1.5">
        <button type="button" class="app-button app-button-neutral app-button-sm">Full Width</button>
        <button type="button" class="app-button app-button-secondary app-button-sm">Refresh</button>
    </div>
</header>
```

---

## 3. Contextual Tab Strip Snippet (Developer Portal)

```html
<div class="developer-workspace-tabs w-full border-b-2 border-amber-600 dark:border-amber-500 overflow-x-auto">
    <nav class="flex items-end gap-1 min-w-max pb-0" aria-label="Developer Workspace Tabs">
        <button type="button"
                class="rounded-t-xl px-3.5 py-2 font-semibold text-xs transition-all border-t border-l border-r @(isActive ? "bg-amber-600 text-white border-amber-600 shadow-xs" : "bg-white text-amber-800 hover:bg-amber-50 dark:bg-slate-800/90 dark:text-slate-300 dark:hover:text-white dark:hover:bg-slate-700 border-slate-200 dark:border-slate-700")">
            Tab Label
        </button>
    </nav>
</div>
```

---

## 4. Layout & Interaction Rules
- **Container**: Outer wrapper `w-full min-w-0 p-2 space-y-3`.
- **In-Context Logs**: Audit Logs (`/developer/audit-logs`) and Activity Logs (`/developer/activity-logs`) must stay under `/developer/*` so developers without Admin Portal access can inspect logs seamlessly.
- **Route Isolation**: Never link from Developer screens into `/admin/*`, `/online/*`, or `/superadmin/*`.
