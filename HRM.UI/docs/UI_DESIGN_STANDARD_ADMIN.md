# HRM.UI Design Standard — Admin Portal (Organisation HR & Management)

> **Parent Standard**: Inherits all base spacing (`p-2`), header, Full Width / Shrink View, form, card, table, and icon rules from [`UI_DESIGN_STANDARD.md`](./UI_DESIGN_STANDARD.md).
> **Route Scope**: `/admin` and `/admin/*`. Cross-portal navigation to `/online`, `/developer`, or `/superadmin` is restricted; use the Portal Switcher.
> **Shell Class**: `.portal-admin`
> **Primary Visual References**: `/admin/my/workspace`, `/admin/settings`

---

## 1. Portal Color Theme — Deep Teal (Existing Theme Preserved)

| Token / Role | Light Mode | Dark Mode | Usage |
| :--- | :--- | :--- | :--- |
| **Sidebar Background** | `#008080` | `#0b3536` | Fixed left navigation rail (`.portal-admin .app-sidebar`) |
| **Topbar Accent Border** | `3px solid #008080` | `3px solid #008080` | Bottom border of the slim top header |
| **Primary Accent / Active Tab** | `#0d9488` / `#0f766e` (`bg-teal-600` / `bg-teal-700`) | `dark:bg-teal-600` | Active folder tab fill, active Portal Switcher pill (`#0d9488`), workspace accents |
| **Inactive Tab Text / Hover** | `text-teal-800` / `hover:bg-teal-50` | `dark:text-slate-300` / `dark:hover:bg-slate-700` | Inactive folder tab text and hover tint |
| **Tab Baseline Rule** | `border-b-2 border-teal-600` | `dark:border-teal-500` | Full-width baseline under `.my-workspace-tabs` / `.org-workspace-tabs` |
| **Context Pill Badge** | `bg-teal-50 border-teal-200 text-teal-700` (dot: `bg-teal-500`) | `dark:bg-teal-950/60 dark:border-teal-800 dark:text-teal-300` | Inline status/context badge beside the page title |
| **Scrollbar Track / Thumb** | `#ecfdf5` / `#16a34a` (hover `#15803d`) | `#123524` / `#22c55e` (hover `#4ade80`) | `.portal-admin` custom scrollbar tokens |
| **Primary Action Button** | `#2563eb` (`app-button-primary`) | `#2563eb` | Standardised cobalt-blue primary action across all portals |

---

## 2. Canonical Header Snippet (Admin Portal)

```html
<header class="flex flex-wrap items-center justify-between gap-2 px-1 py-0.5">
    <div class="min-w-0">
        <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-slate-100 truncate">Page Title</h1>
            <span class="inline-flex items-center gap-1 rounded-full bg-teal-50 dark:bg-teal-950/60 border border-teal-200/80 dark:border-teal-800 px-2 py-0.5 text-[10px] font-semibold text-teal-700 dark:text-teal-300">
                <span class="h-1.5 w-1.5 rounded-full bg-teal-500"></span>
                Admin Portal
            </span>
        </div>
        <p class="text-xs text-slate-500 dark:text-slate-400">Concise one-line description.</p>
    </div>
    <div class="flex flex-wrap items-center gap-1.5">
        <button type="button" class="app-button app-button-neutral app-button-sm">Full Width</button>
        <button type="button" class="app-button app-button-secondary app-button-sm">Refresh</button>
        <button type="button" class="app-button app-button-primary app-button-sm">Create</button>
    </div>
</header>
```

---

## 3. Contextual Tab Strip Snippet (Admin Portal)

```html
<div class="my-workspace-tabs w-full border-b-2 border-teal-600 dark:border-teal-500 overflow-x-auto">
    <nav class="flex items-end gap-1 min-w-max pb-0" aria-label="Admin Workspace Tabs">
        <button type="button"
                class="rounded-t-xl px-3.5 py-2 font-semibold text-xs transition-all border-t border-l border-r @(isActive ? "bg-teal-700 text-white border-teal-700 shadow-xs" : "bg-white text-teal-800 hover:bg-teal-50 dark:bg-slate-800/90 dark:text-slate-300 dark:hover:text-white dark:hover:bg-slate-700 border-slate-200 dark:border-slate-700")">
            Tab Label
        </button>
    </nav>
</div>
```

---

## 4. Layout & Interaction Rules
- **Container**: Outer wrapper `w-full min-w-0 p-2 space-y-4`.
- **No Redundant Back Button**: Do not place a `← Back` button on `/admin/settings` or standardised Admin module headers.
- **Enterprise Tables**: Compact zebra-striped rows, crisp equal-height filters, right-aligned outline row actions.
- **Route Isolation**: Keep all Admin links within `/admin/*` (or neutral shared routes like `/profile`). Platform-wide SuperAdmin tasks belong in `/superadmin/*`, and developer telemetry belongs in `/developer/*`.
