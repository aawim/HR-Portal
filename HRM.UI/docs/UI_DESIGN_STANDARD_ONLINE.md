# HRM.UI Design Standard — Online Portal (Employee Self-Service)

> **Parent Standard**: Inherits all base spacing (`p-2`), header, Full Width / Shrink View, form, card, table, and icon rules from [`UI_DESIGN_STANDARD.md`](./UI_DESIGN_STANDARD.md).
> **Route Scope**: `/online` and `/online/*` (plus `/` when authenticated). Cross-portal navigation to `/admin`, `/developer`, or `/superadmin` is restricted; use the Portal Switcher.
> **Shell Class**: `.portal-online`
> **Primary Visual Reference**: `/online/item-requests`

---

## 1. Portal Color Theme — Deep Sky Blue (Existing Theme Preserved)

| Token / Role | Light Mode | Dark Mode | Usage |
| :--- | :--- | :--- | :--- |
| **Sidebar Background** | `#075985` | `#0c4a6e` | Fixed left navigation rail (`.portal-online .app-sidebar`) |
| **Topbar Accent Border** | `3px solid #075985` | `3px solid #075985` | Bottom border of the slim top header |
| **Primary Accent / Active Tab** | `#2563eb` (`bg-blue-600`) | `#3b82f6` (`dark:bg-blue-600`) | Active folder tab fill, primary actions (`app-button-primary`), active Portal Switcher pill (`#0284c7`) |
| **Inactive Tab Text / Hover** | `text-blue-800` / `hover:bg-blue-50` | `dark:text-slate-300` / `dark:hover:bg-slate-700` | Inactive folder tab text and hover tint |
| **Tab Baseline Rule** | `border-b-2 border-blue-600` | `dark:border-blue-500` | Full-width baseline under `.online-workspace-tabs` |
| **Context Pill Badge** | `bg-blue-50 border-blue-200 text-blue-700` (dot: `bg-blue-500`) | `dark:bg-blue-950/60 dark:border-blue-800 dark:text-blue-300` | Inline status/context badge beside the page title |
| **Scrollbar Track / Thumb** | `#eff6ff` / `#2563eb` (hover `#1d4ed8`) | `#172554` / `#3b82f6` (hover `#60a5fa`) | `.portal-online` custom scrollbar tokens |
| **Focus Ring & Selection** | `focus:border-blue-500 focus:ring-blue-500` | `dark:focus:border-blue-400` | Inputs, selects, selectable history cards (`border-blue-500 bg-blue-50/70`) |

---

## 2. Canonical Header Snippet (Online Portal)

```html
<header class="flex flex-wrap items-center justify-between gap-2 px-1 py-0.5">
    <div class="min-w-0">
        <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-slate-100 truncate">Page Title</h1>
            <span class="inline-flex items-center gap-1 rounded-full bg-blue-50 dark:bg-blue-950/60 border border-blue-200/80 dark:border-blue-800 px-2 py-0.5 text-[10px] font-semibold text-blue-700 dark:text-blue-300">
                <span class="h-1.5 w-1.5 rounded-full bg-blue-500"></span>
                Self-Service
            </span>
        </div>
        <p class="text-xs text-slate-500 dark:text-slate-400">Concise one-line description.</p>
    </div>
    <div class="flex flex-wrap items-center gap-1.5">
        <button type="button" class="app-button app-button-neutral app-button-sm">Full Width</button>
        <button type="button" class="app-button app-button-secondary app-button-sm">Refresh</button>
        <button type="button" class="app-button app-button-primary app-button-sm">New Request</button>
    </div>
</header>
```

---

## 3. Contextual Tab Strip Snippet (Online Portal)

```html
<div class="online-workspace-tabs w-full border-b-2 border-blue-600 dark:border-blue-500 overflow-x-auto">
    <nav class="flex items-end gap-1 min-w-max pb-0" aria-label="Online Section Tabs">
        <button type="button"
                class="rounded-t-xl px-3.5 py-2 font-semibold text-xs transition-all border-t border-l border-r @(isActive ? "bg-blue-600 text-white border-blue-600 shadow-xs" : "bg-white text-blue-800 hover:bg-blue-50 dark:bg-slate-800/90 dark:text-slate-300 dark:hover:text-white dark:hover:bg-slate-700 border-slate-200 dark:border-slate-700")">
            Tab Label
        </button>
    </nav>
</div>
```

---

## 4. Layout & Interaction Rules
- **Container**: Outer wrapper `w-full min-w-0 p-2 space-y-4`.
- **Header Never Shrinks**: Header and tab strip always remain full width; only content below shrinks to `max-w-7xl mx-auto` in Shrink View.
- **2-Panel Split**: `grid-template-columns: minmax(0, 7fr) minmax(0, 5fr)` at `>= 1024px` (left: request form / detail, right: history / summary list).
- **Route Isolation**: Keep all navigation links within `/online/*` (or neutral shared routes like `/profile` and `/careers`). Never link directly to `/admin/*`, `/developer/*`, or `/superadmin/*`.
