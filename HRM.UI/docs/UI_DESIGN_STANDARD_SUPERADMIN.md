# HRM.UI Design Standard — SuperAdmin Portal (Platform Configuration)

> **Parent Standard**: Inherits all base spacing (`p-2`), header, Full Width / Shrink View, form, card, table, and icon rules from [`UI_DESIGN_STANDARD.md`](./UI_DESIGN_STANDARD.md).
> **Route Scope**: `/superadmin` and `/superadmin/*`. Cross-portal navigation to `/online`, `/admin`, or `/developer` is restricted; use the Portal Switcher.
> **Shell Class**: `.portal-superadmin`
> **Primary Visual Reference**: `/superadmin/overview` (`SuperAdminPortalPage.razor`)

---

## 1. Portal Color Theme — Light Blue (`#ADD8E6`)

The SuperAdmin Portal uses **`#ADD8E6`** (Light Blue) as its signature portal identity colour. Because `#ADD8E6` is a light pastel surface, all text and icons placed directly on `#ADD8E6` MUST use high-contrast dark navy/slate (`#0f172a` / `#0c4a6e`) to maintain WCAG AA/AAA readability.

| Token / Role | Light Mode | Dark Mode | Usage |
| :--- | :--- | :--- | :--- |
| **Sidebar Background** | `#ADD8E6` | `#163a4a` | Fixed left navigation rail (`.portal-superadmin .app-sidebar`) |
| **Sidebar Text & Icons** | `#0f172a` (headings/groups: `#0c4a6e`) | `#e0f2fe` (groups: `#ADD8E6`) | High-contrast text/icons on the `#ADD8E6` rail |
| **Sidebar Active Item** | `rgba(255,255,255,.62)` + `3px solid #0369a1` | `rgba(173,216,230,.22)` + `3px solid #ADD8E6` | Active nav link highlight |
| **Topbar Accent Border** | `3px solid #ADD8E6` | `3px solid #ADD8E6` | Bottom border of the slim top header |
| **Primary Accent / Active Tab** | `background: #ADD8E6; border: #7cc0d8; color: #0f172a` | `background: #ADD8E6; border: #7cc0d8; color: #0f172a` | Active folder tab, active sub-tab, active Portal Switcher pill |
| **Inactive Tab Text / Hover** | `text-slate-800` / `hover:bg-sky-50` | `dark:text-slate-300` / `dark:hover:bg-slate-700` | Inactive folder tab text and hover tint |
| **Tab Baseline Rule** | `border-b-2` with `#ADD8E6` | `border-b-2` with `#ADD8E6` | Full-width baseline under `.superadmin-workspace-tabs` |
| **Context Pill Badge** | `bg: #ADD8E6; border: #7cc0d8; text: #0f172a` (dot: `#0369a1`) | `bg: #ADD8E6; border: #7cc0d8; text: #0f172a` | Inline status/context badge beside the page title |
| **Scrollbar Track / Thumb** | `#f0f9ff` / `#ADD8E6` (hover `#7cc0d8`) | `#0c2a36` / `#ADD8E6` (hover `#cfeaf3`) | `.portal-superadmin` custom scrollbar tokens |

> **Tailwind Note**: `#ADD8E6` and `#7cc0d8` are not precompiled arbitrary Tailwind utilities in `tailwind.css`. Apply them via CSS classes in `layout.css` / `theme.css` or inline `style="background-color: #ADD8E6; border-color: #7cc0d8; color: #0f172a;"`.

---

## 2. Canonical Header Snippet (SuperAdmin Portal)

```html
<header class="superadmin-workspace-header flex flex-wrap items-center justify-between gap-2 px-1 py-0.5">
    <div class="min-w-0">
        <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-base sm:text-lg font-bold text-slate-900 dark:text-slate-100 truncate">SuperAdmin Portal</h1>
            <span class="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-semibold text-slate-900"
                  style="background-color: #ADD8E6; border-color: #7cc0d8;">
                <span class="h-1.5 w-1.5 rounded-full" style="background-color: #0369a1;"></span>
                Platform Configuration
            </span>
        </div>
        <p class="text-xs text-slate-500 dark:text-slate-400">Platform-wide organisation onboarding, permission entitlements, cross-organisation staff administration, and shared catalogues.</p>
    </div>
    <div class="flex flex-wrap items-center gap-1.5">
        <button type="button" class="app-button app-button-neutral app-button-sm">Full Width</button>
    </div>
</header>
```

---

## 3. Contextual Tab Strip Snippet (SuperAdmin Portal)

```html
<div class="superadmin-workspace-tabs w-full border-b-2 overflow-x-auto" style="border-bottom-color: #ADD8E6;">
    <nav class="flex items-end gap-1 min-w-max pb-0" aria-label="SuperAdmin Portal Tabs">
        <button type="button"
                class="rounded-t-xl px-3.5 py-2 font-semibold text-xs transition-all border-t border-l border-r @(isActive ? "text-slate-900 shadow-xs" : "bg-white text-slate-800 hover:bg-sky-50 dark:bg-slate-800/90 dark:text-slate-300 dark:hover:text-white dark:hover:bg-slate-700 border-slate-200 dark:border-slate-700")"
                style="@(isActive ? "background-color: #ADD8E6; border-color: #7cc0d8; color: #0f172a;" : null)">
            Tab Label
        </button>
    </nav>
</div>
```

---

## 4. Scope & Interaction Rules
- **Authorisation**: Requires `Super.Admin.Manage` (`PlatformAdministratorAccess.HasAsync`).
- **Core Capabilities**: Organisation Onboarding, Organisation Permissions, Admin Portal Access Grants, Cross-Organisation Staff & User Editing, Roles & Access Control, Shared Reference Resources, Job/Work Types, and Pay Frameworks.
- **Container & Persistence**: Outer wrapper `w-full min-w-0 p-2 space-y-3`; persist Full Width / Shrink View under `superadmin.workspace.fullWidth`.
- **Route Isolation**: Keep all links and embedded editors strictly within `/superadmin/*` (pass `HideHeader="true"` to embedded editors so they do not navigate to `/admin/*`).
